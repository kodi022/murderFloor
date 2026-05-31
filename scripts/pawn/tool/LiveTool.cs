namespace MurderFloor;

public partial class LiveTool : Node
{
    [Export]
    public int PlayerId { get; set; }
    [Export]
    public string ToolFullId { get; set; }

    // reference to player
    public Player Player { get; private set; }
    // reference to tool
    public Tool ToolResource { get; private set; }

    // public Godot.Collections.Dictionary<string, string> AttachmentConfig { get; set; }
    // public Godot.Collections.Dictionary<string, string> ModifierConfig { get; set; }

    private MeshInstance3D meshInstance3D;

    private bool equipped = false;

    // toolfirearm
    public Vector2 CurrentSpread { get; private set; }
    public int CurrentMag { get; private set; } = 0;
    public int CurrentReserve { get; private set; } = 0;
    public bool Reloading { get; private set; } = false;
    private ulong rpmAsMs = 0;
    private ulong msSinceFire = 0;
    private bool bolting = false;
    private bool shotSemi = false;

    public override void _Ready()
    {
        Player = Player.FindPlayer(PlayerId);
        ToolResource = ResourceManager.ToolRegistry.GetResourceReference(ToolFullId);

        if (ToolResource is ToolFirearm firearm)
        {
            rpmAsMs = (ulong)(60f / firearm.RPM * 1000f);
            CurrentMag = firearm.MagSize;
            CurrentSpread = firearm.InitialDegreeSpread;
            CurrentReserve = firearm.MagSize * firearm.MagsReserve;
        }
    }

    public override void _Process(double delta)
    {
        if (!equipped) return;

        if (ToolResource is ToolFirearm firearm)
        {
            CurrentSpread = (CurrentSpread - (Vector2.One * firearm.SpreadRecoveryRate * (float)delta)).Max(firearm.InitialDegreeSpread);
        }
    }

    public async Task Equip()
    {
        var posNode = IsMultiplayerAuthority() ? Player.ViewToolPosition : Player.WorldToolPosition;
        foreach (var child in posNode.GetChildren())
        {
            child.Free();
        }

        posNode.AddChild(ToolResource.MeshScene.Instantiate());

        // await equip animation
        await Task.Delay(200);
        equipped = true;
    }

    public async Task Unequip()
    {
        // await unequip animation
        await Task.Delay(200);
        equipped = false;
    }

    public void FirePrimary()
    {
        if (!equipped) return;

        if (ToolResource is ToolFirearm firearm)
        {
            if (Reloading) return;
            if (bolting) return;
            if (firearm.FireMode == ToolFirearm.FireModeEnum.Semi && shotSemi) return;

            FirePrimaryFirearm(firearm, CreateFireInfo());
            return;
        }

        if (ToolResource is ToolMelee melee)
        {

            melee.FireMelee(CreateFireInfo());
            return;
        }
    }

    public void UnFirePrimary()
    {
        shotSemi = false;
    }

    public void FireSecondary()
    {
        if (!equipped) return;

    }

    public void FireReload()
    {
        if (!equipped) return;

        if (ToolResource is ToolFirearm firearm)
        {
            ReloadFirearm(firearm, CreateFireInfo());
            return;
        }
    }

    private void FirePrimaryFirearm(ToolFirearm firearm, Tool.FireInfo fi)
    {
        if (bolting) return;
        if (Reloading) return;

        var ticksMs = Time.GetTicksMsec();
        if (rpmAsMs < ticksMs - msSinceFire)
        {
            if (CurrentMag <= 0)
            {
                ReloadFirearm(firearm, fi);
                return;
            }

            if (shotSemi && firearm.FireMode == ToolFirearm.FireModeEnum.Manual)
            {
                BoltFirearm(firearm, fi);
                return;
            }

            msSinceFire = ticksMs;
            firearm.FireBullet(fi);

            var poly = (AudioStreamPlaybackPolyphonic)fi.Player.AudioStreamPlayer3D.GetStreamPlayback();
            poly.PlayStream(firearm.FireSound);

            shotSemi = true;
            CurrentSpread = (CurrentSpread + firearm.SpreadIncreasePerShot).Min(firearm.MaxDegreeSpread);
            CurrentMag--;
        }
    }

    private async void BoltFirearm(ToolFirearm firearm, Tool.FireInfo fi)
    {
        if (bolting) return;
        if (Reloading) return;
        if (CurrentMag <= 0) return;
        bolting = true;
        GD.Print("BOLT");

        //var poly = (AudioStreamPlaybackPolyphonic)fi.Player.AudioStreamPlayer3D.GetStreamPlayback();
        //poly.PlayStream(firearm.ReloadSound);

        fi.Player.ViewModelRotationKick += new Vector3(1, 0, 0);
        await Task.Delay(firearm.ManualFireDelayMs);
        //await boltanimation

        GD.Print("BOLT-DONE");
        bolting = false;
    }

    private async void ReloadFirearm(ToolFirearm firearm, Tool.FireInfo fi)
    {
        if (bolting) return;
        if (Reloading) return;
        if (CurrentMag >= firearm.MagSize) return;
        if (CurrentReserve <= 0) return;
        Reloading = true;

        fi.Player.ViewModelRotationKick += new Vector3(-1, 0.5f, 0);
        var poly = (AudioStreamPlaybackPolyphonic)fi.Player.AudioStreamPlayer3D.GetStreamPlayback();
        poly.PlayStream(firearm.ReloadSound);

        await Task.Delay(firearm.ReloadDelayMs);

        fi.Player.ViewModelRotationKick += new Vector3(0.5f, 0, 0);
        //await reloadanimation

        var diff = firearm.MagSize - CurrentMag;
        if (diff >= CurrentReserve)
        {
            CurrentMag += CurrentReserve;
            CurrentReserve = 0;
        }
        else
        {
            CurrentMag = firearm.MagSize;
            CurrentReserve -= diff;
        }

        Reloading = false;
    }

    private Tool.FireInfo CreateFireInfo()
    {
        return new Tool.FireInfo()
        {
            Player = Player,
            LiveTool = this,
            StartPosition = Player.ViewPosition,
            ViewTransform = Player.ViewTransform
        };
    }
}