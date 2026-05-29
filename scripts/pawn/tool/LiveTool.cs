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

        var fi = new Tool.FireInfo()
        {
            Player = Player.Self,
            CurrentSpread = CurrentSpread,
            StartPosition = Player.Self.ViewPosition,
            ViewTransform = Player.Self.ViewTransform
        };

        if (ToolResource is ToolFirearm firearm)
        {
            if (Reloading) return;
            if (bolting) return;
            if (firearm.FireMode == ToolFirearm.FireModeEnum.Semi && shotSemi) return;
            FirePrimaryFirearm(firearm, fi);
            return;
        }

        if (ToolResource is ToolMelee melee)
        {
            melee.FireMelee(fi);
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

        var fi = new Tool.FireInfo()
        {
            Player = Player.Self,
            CurrentSpread = CurrentSpread,
            StartPosition = Player.Self.ViewPosition,
            ViewTransform = Player.Self.ViewTransform
        };

        if (ToolResource is ToolFirearm firearm)
        {
            if (Reloading) return;
            if (bolting) return;
            if (CurrentMag >= firearm.MagSize) return;
            ReloadFirearm(firearm, fi);
            return;
        }
    }

    private void FirePrimaryFirearm(ToolFirearm firearm, Tool.FireInfo fi)
    {
        var ticksMs = Time.GetTicksMsec();
        if (rpmAsMs < ticksMs - msSinceFire)
        {
            msSinceFire = ticksMs;
            firearm.FireBullet(fi);

            var poly = (AudioStreamPlaybackPolyphonic)fi.Player.AudioStreamPlayer3D.GetStreamPlayback();
            poly.PlayStream(firearm.FireSound);

            shotSemi = true;
            CurrentSpread = (CurrentSpread + firearm.SpreadIncreasePerShot).Min(firearm.MaxDegreeSpread);
            CurrentMag--;

            if (CurrentMag <= 0)
            {
                ReloadFirearm(firearm, fi);
                return;
            }

            if (firearm.FireMode == ToolFirearm.FireModeEnum.Manual)
            {
                BoltFirearm(firearm, fi);
                return;
            }
        }
    }

    private async void BoltFirearm(ToolFirearm firearm, Tool.FireInfo fi)
    {
        if (bolting) return;
        bolting = true;
        GD.Print("BOLT");

        await Task.Delay(firearm.ManualFireDelayMs);
        //await boltanimation

        GD.Print("BOLT-DONE");
        bolting = false;
    }

    private async void ReloadFirearm(ToolFirearm firearm, Tool.FireInfo fi)
    {
        if (Reloading) return;
        Reloading = true;
        GD.Print("RELOAD");

        var poly = (AudioStreamPlaybackPolyphonic)fi.Player.AudioStreamPlayer3D.GetStreamPlayback();
        poly.PlayStream(firearm.ReloadSound);

        await Task.Delay(firearm.ReloadDelayMs);
        //await reloadanimation

        GD.Print("RELOAD-DONE");
        CurrentMag = firearm.MagSize;
        Reloading = false;
    }
}