namespace MurderFloor;

public partial class LiveTool : Node
{
    [Export]
    public int PlayerId { get; set; }
    [Export]
    public string ToolId { get; set; }

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
    private ulong rpmAsMs = 0;
    private ulong msSinceFire = 0;
    private int currentMag = 0;
    private bool reloading = false;
    private bool bolting = false;
    private bool shotSemi = false;

    public override void _Ready()
    {
        Player = Player.FindPlayer(PlayerId);
        ToolResource = ResourceManager.ToolRegistry.GetResourceReference(ToolId);

        if (ToolResource is ToolFirearm firearm)
        {
            rpmAsMs = (ulong)(60f / firearm.RPM * 1000f);
            currentMag = firearm.MagSize;
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
        foreach (var child in Player.ViewToolPosition.GetChildren())
        {
            child.Free();
        }

        Player.ViewToolPosition.AddChild(ToolResource.MeshScene.Instantiate());

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
            if (reloading) return;
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

        if (ToolResource is ToolFirearm firearm)
        {
            if (reloading) return;
            if (bolting) return;
            if (currentMag >= firearm.MagSize) return;
            ReloadFirearm(firearm);
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
            shotSemi = true;
            CurrentSpread = (CurrentSpread + firearm.SpreadIncreasePerShot).Min(firearm.MaxDegreeSpread);
            currentMag--;

            if (currentMag <= 0)
            {
                ReloadFirearm(firearm);
                return;
            }

            if (firearm.FireMode == ToolFirearm.FireModeEnum.Manual)
            {
                BoltFirearm(firearm);
                return;
            }
        }
    }

    private async void BoltFirearm(ToolFirearm firearm)
    {
        if (bolting) return;
        bolting = true;
        GD.Print("BOLT");

        await Task.Delay(firearm.ManualFireDelayMs);
        //await boltanimation

        GD.Print("BOLT-DONE");
        bolting = false;
    }

    private async void ReloadFirearm(ToolFirearm firearm)
    {
        if (reloading) return;
        reloading = true;
        GD.Print("RELOAD");

        await Task.Delay(firearm.ReloadDelayMs);
        //await reloadanimation

        GD.Print("RELOAD-DONE");
        currentMag = firearm.MagSize;
        reloading = false;
    }
}