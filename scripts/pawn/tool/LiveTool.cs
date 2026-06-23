namespace MurderFloor;

public partial class LiveTool : Node
{
    [Export]
    public int PlayerId { get; set; }
    // reference to player
    public Player Player { get; private set; }

    [Export]
    public string ToolFullId { get; set; }
    // reference to tool
    public Tool ToolResource { get; private set; }

    [Export]
    public int PrimaryInputState { get; set; } = 0; // 0 is no, 1 is yes, 2 is justReleased
    [Export]
    public int SecondaryInputState { get; set; } = 0; // 0 is no, 1 is yes, 2 is justReleased
    [Export]
    public int ReloadInputState { get; set; } = 0; // 0 is no, 1 is yes, 2 is justReleased

    public AnimationPlayer AnimationPlayer { get; private set; }

    // public Godot.Collections.Dictionary<string, string> AttachmentConfig { get; set; }
    // public Godot.Collections.Dictionary<string, string> ModifierConfig { get; set; }

    private bool equipped = false;

    // toolfirearm
    public Vector2 CurrentSpread { get; private set; }

    [Export]
    public int CurrentMag { get; private set; } = 0;

    public int CurrentReserve { get; private set; } = 0;
    public bool Reloading { get; private set; } = false;
    private ulong rpmAsMs = 0;
    private ulong msSinceFire = 0;
    private bool bolting = false;
    private bool shotSemi = false;
    private bool shotBolt = false;

    private Node3D modelScene;
    private Node3D muzzleNode;
    private Node3D sightNode;
    private Node3D barrelNode;
    private Node3D addon1Node;
    private Node3D addon2Node;

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

        AnimationPlayer?.Play(ToolResource.HoldTypeAnimation);

        if (ToolResource is ToolFirearm firearm)
        {
            CurrentSpread = (CurrentSpread - (Vector2.One * firearm.SpreadRecoveryRate * (float)delta)).Max(firearm.InitialDegreeSpread);
            if (PrimaryInputState == 1)
            {
                FirePrimary();
            }

            if (PrimaryInputState == 2)
            {
                UnFirePrimary();
            }

            if (ReloadInputState == 1)
            {
                FireReload();
            }
        }
    }

    public async Task Equip()
    {
        var posNode = IsMultiplayerAuthority() ? Player.ViewAimViewmodel : Player.WorldToolPosition;
        foreach (var child in posNode.GetChildren())
        {
            child.Free();
        }

        if (IsMultiplayerAuthority())
        {
            modelScene = (Node3D)ToolResource.MeshSceneViewmodel.Instantiate();
            posNode.AddChild(modelScene);
            AnimationPlayer = (AnimationPlayer)modelScene.GetChild(0).GetChild(1);
        }
        else
        {
            modelScene = (Node3D)ToolResource.MeshScene.Instantiate();
            posNode.AddChild(modelScene);
            modelScene.RotationDegrees = new Vector3(0, ToolResource.MeshSceneImportYaw, 0);
        }

        muzzleNode = (Node3D)modelScene.FindChildren("Muzzle").FirstOrDefault(new Node3D());
        if (ToolResource is ToolFirearm && !muzzleNode.IsInsideTree())
            GD.PrintErr($"Warning: {ToolResource.FullId} has no Node3D named \"Muzzle\"");

        sightNode = (Node3D)modelScene.FindChildren("Sight").FirstOrDefault(new Node3D());
        if (ToolResource is ToolFirearm && !sightNode.IsInsideTree())
            GD.PrintErr($"Warning: {ToolResource.FullId} has no Node3D named \"Sight\"");

        // ! find other attachments

        // await equip animation
        await Task.Delay(200);
        equipped = true;
    }

    public async Task Unequip()
    {
        // await unequip animation
        await Task.Delay(200);

        modelScene = null;
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

        if (CurrentMag <= 0)
        {
            ReloadFirearm(firearm, fi);
            return;
        }

        if (shotBolt && firearm.FireMode == ToolFirearm.FireModeEnum.Manual)
        {
            if (!shotSemi) BoltFirearm(firearm, fi);
            return;
        }

        var ticksMs = Time.GetTicksMsec();
        if (rpmAsMs < ticksMs - msSinceFire)
        {
            msSinceFire = ticksMs;
            firearm.FireBullet(fi);

            var poly = (AudioStreamPlaybackPolyphonic)fi.Player.AudioStreamPlayer3D.GetStreamPlayback();
            poly.PlayStream(firearm.FireSound, bus: "Effects");

            // muzzle effect

            shotSemi = true;
            shotBolt = true;
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

        var poly = (AudioStreamPlaybackPolyphonic)fi.Player.AudioStreamPlayer3D.GetStreamPlayback();
        poly.PlayStream(firearm.ManualFireSound, bus: "Effects");

        fi.Player.ViewModelPositionKick += new Vector3(0, 0, 0.1f);
        await Task.Delay(firearm.ManualFireDelayMs - 200);
        //await boltanimation
        fi.Player.ViewModelPositionKick += new Vector3(0, 0, -0.1f);

        await Task.Delay(200);

        bolting = false;
        shotBolt = false;
    }

    private async void ReloadFirearm(ToolFirearm firearm, Tool.FireInfo fi)
    {
        if (bolting) return;
        if (Reloading) return;
        if (CurrentMag >= firearm.MagSize) return;
        if (CurrentReserve <= 0) return;
        Reloading = true;

        fi.Player.ViewModelRotationKick += new Vector3(-1f, 0.5f, 0);
        var poly = (AudioStreamPlaybackPolyphonic)fi.Player.AudioStreamPlayer3D.GetStreamPlayback();
        poly.PlayStream(firearm.ReloadSound, bus: "Effects");

        await Task.Delay(firearm.ReloadDelayMs);

        fi.Player.ViewModelPositionKick += new Vector3(0, 0, 0.1f);
        fi.Player.ViewModelRotationKick += new Vector3(0.2f, 0, 0);
        //await reloadanimation

        if (firearm.EndlessReserve)
        {
            CurrentMag = firearm.MagSize;
            Reloading = false;
            return;
        }

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
        shotBolt = false;
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