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

    public ToolConfig ToolConfig { get; set; }

    public List<int> AttachmentHashes { get; set; }

    [Export]
    public int PrimaryInputState { get; set; } = 0; // 0 is no, 1 is yes, 2 is just released
    [Export]
    public int SecondaryInputState { get; set; } = 0; // 0 is no, 1 is yes, 2 is just released
    [Export]
    public int ReloadInputState { get; set; } = 0; // 0 is no, 1 is yes, 2 is just released

    public AnimationPlayer AnimationPlayer { get; private set; }

    // public Godot.Collections.Dictionary<string, string> AttachmentConfig { get; set; }
    // public Godot.Collections.Dictionary<string, string> ModifierConfig { get; set; }

    private bool equipped = false;

    // toolfirearm
    public Vector2 CurrentSpread { get; private set; }
    public Vector2 MinSpread { get; private set; }
    public Vector2 MaxSpread { get; private set; }

    [Export]
    public int CurrentMag { get; private set; } = 0;

    public int CurrentReserve { get; private set; } = 0;
    public bool Reloading { get; private set; } = false;
    private ulong rpmAsMs = 0;
    private ulong msSinceFire = 0;
    private bool bolting = false;
    private bool shotSemi = false;
    private bool shotBolt = false;

    public bool Aiming { get; private set; } = false;
    public float OpticZoom => builtTool.OpticZoom == 0f ? 1.1f : builtTool.OpticZoom;

    private float currentAimingPositionLerp;

    private Node3D worldScene;

    private Node3D viewmodelScene;
    private Vector3 viewmodelSceneStartPos;
    private Vector3 viewmodelSceneSightPosition;
    private MFResource.BuiltToolData builtTool;

    private GpuParticles3D muzzleFlash;

    public override void _Ready()
    {
        Player = Player.FindPlayer(PlayerId);
        ToolResource = ResourceManager.ToolRegistry.GetResourceRef(ToolFullId);

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

        //AnimationPlayer?.Play(ToolResource.HoldTypeAnimation);

        currentAimingPositionLerp += Aiming ? (float)delta * 4f : -(float)delta * 4f;
        currentAimingPositionLerp = Mathf.Clamp(currentAimingPositionLerp, 0f, 1f);
        viewmodelScene.Position = viewmodelSceneStartPos.Lerp(viewmodelSceneSightPosition + builtTool.SightPositionOffset, currentAimingPositionLerp);

        if (ToolResource is ToolFirearm firearm)
        {
            var plrVel = Player.Velocity.LengthSquared();
            var movementPenalty = Vector2.One;
            if (plrVel > 8f)
                movementPenalty = firearm.FastWalkSpreadMult;
            else if (plrVel > 1f)
                movementPenalty = firearm.SlowWalkSpreadMult;

            var aimBuff = Aiming ? firearm.AimSpreadMult : Vector2.One;

            MinSpread = firearm.InitialDegreeSpread * aimBuff * movementPenalty;
            MaxSpread = firearm.MaxDegreeSpread * movementPenalty;

            var recoveryRate = Vector2.One * firearm.SpreadRecoveryRate * (float)delta;

            if (CurrentSpread < MinSpread)

                CurrentSpread += (Vector2.One * (float)delta * 50f).Min(MinSpread);
            else
                CurrentSpread = (CurrentSpread - recoveryRate).Max(MinSpread);
        }

        if (PrimaryInputState == 1) FirePrimary();
        if (PrimaryInputState == 2) UnFirePrimary();

        if (SecondaryInputState == 1) FireSecondary();
        if (SecondaryInputState == 2) UnFireSecondary();

        if (ReloadInputState == 1)
        {
            FireReload();
        }
    }

    public async Task Equip()
    {
        var posNode = Player.Viewing == Player ? Player.ViewAimViewmodel : Player.WorldToolPosition;
        foreach (var child in posNode.GetChildren())
        {
            child.Free();
        }

        if (Player.Viewing == Player)
        {
            viewmodelScene = ToolResource.ViewmodelScene.Instantiate<Node3D>();
            viewmodelScene.RotationDegrees += new Vector3(0, 90, 0);

            if (ToolResource.FullId != "base:fists")
            {
                var oldGun = (Node3D)viewmodelScene.FindChild("Armature*");
                var oldGunPos = oldGun.Position;
                var oldGunRot = oldGun.Rotation;
                oldGun.Free();

                builtTool = ToolResource.BuildToolScene(ToolConfig);
                var newGun = builtTool.Tool.GetChild<Node3D>(0);
                newGun.Owner = null;
                newGun.Position = oldGunPos;
                newGun.Rotation = oldGunRot;
                newGun.Reparent(viewmodelScene, false);
                viewmodelSceneSightPosition = new Vector3(-oldGunPos.Z, -oldGunPos.Y, 0);

                var flash = GD.Load<PackedScene>("res://scenes/particle/GunFlash.tscn");
                var inst = flash.Instantiate<Node3D>();
                inst.Position = builtTool.MuzzlePosition;
                newGun.AddChild(inst);
                muzzleFlash = inst.GetChild<GpuParticles3D>(0);
            }

            posNode.AddChild(viewmodelScene);
        }
        else
        {
            // modelSceneArms = ToolResource.MeshScene.Instantiate<Node3D>();
            // posNode.AddChild(modelScene);
            // modelScene.RotationDegrees = new Vector3(0, ToolResource.MeshSceneImportYaw, 0);
        }

        viewmodelSceneStartPos = viewmodelScene.Position;
        AnimationPlayer = (AnimationPlayer)viewmodelScene.FindChild("AnimationPlayer");
        if (AnimationPlayer.HasAnimation("equip"))
        {
            await TaskAnimation("equip", 400);
        }
        else
        {
            Player.AddViewmodelRotationKick(new Vector3(-1.2f, 0, 0));
            await Task.Delay(400);
        }

        AnimationPlayer.Play("idle");
        GD.Print(string.Join(',', AnimationPlayer.GetAnimationList()));
        equipped = true;
    }

    public async Task EquipViewing()
    {
        var posNode = Player.ViewAimViewmodel;
        foreach (var child in posNode.GetChildren())
        {
            child.Free();
        }

        viewmodelScene = ToolResource.ViewmodelScene.Instantiate<Node3D>();
        viewmodelScene.RotationDegrees += new Vector3(0, 90, 0);

        if (ToolResource.FullId != "base:fists")
        {
            var oldGun = (Node3D)viewmodelScene.FindChild("Armature*");
            var oldGunPos = oldGun.Position;
            var oldGunRot = oldGun.Rotation;
            oldGun.Free();

            builtTool = ToolResource.BuildToolScene(ToolConfig);
            var newGun = builtTool.Tool.GetChild<Node3D>(0);
            newGun.Owner = null;
            newGun.Position = oldGunPos;
            newGun.Rotation = oldGunRot;
            newGun.Reparent(viewmodelScene, false);
            viewmodelSceneSightPosition = new Vector3(-oldGunPos.Z, -oldGunPos.Y, 0);

            var flash = GD.Load<PackedScene>("res://scenes/particle/GunFlash.tscn");
            var inst = flash.Instantiate<Node3D>();
            inst.Position = builtTool.MuzzlePosition;
            newGun.AddChild(inst);
            muzzleFlash = inst.GetChild<GpuParticles3D>(0);
        }
        viewmodelSceneStartPos = viewmodelScene.Position;

        AnimationPlayer = (AnimationPlayer)viewmodelScene.FindChild("AnimationPlayer");
        posNode.AddChild(viewmodelScene);
        AnimationPlayer.Play("idle");
        equipped = true;
    }

    public async Task Unequip()
    {
        if (AnimationPlayer.HasAnimation("unequip"))
        {
            await TaskAnimation("unequip", 400);
        }
        else
        {
            Player.AddViewmodelRotationKick(new Vector3(-1.2f, 0, 0));
            await Task.Delay(400);
        }

        viewmodelScene?.Free();
        viewmodelScene = null;
        equipped = false;
    }

    public async Task UnequipViewing()
    {
        viewmodelScene?.Free();
        viewmodelScene = null;
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

        if (ToolResource is ToolFirearm)
        {
            Aiming = true;
        }
    }

    public void UnFireSecondary()
    {
        if (!equipped) return;

        Aiming = false;
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

            AnimationPlayer.Stop();
            AnimationPlayer.Play("fire");
            var poly = (AudioStreamPlaybackPolyphonic)fi.Player.AudioStreamPlayer3D.GetStreamPlayback();
            poly.PlayStream(firearm.FireSound, bus: "Effects");

            muzzleFlash.Restart();
            // muzzle effect

            shotSemi = true;
            shotBolt = true;
            CurrentSpread = (CurrentSpread + firearm.SpreadIncreasePerShot).Min(MaxSpread);
            CurrentMag--;
        }
    }

    private async void BoltFirearm(ToolFirearm firearm, Tool.FireInfo fi)
    {
        if (bolting) return;
        if (Reloading) return;
        if (CurrentMag <= 0) return;
        bolting = true;

        if (AnimationPlayer.HasAnimation("bolt"))
        {
            await TaskAnimation("bolt", firearm.ManualFireDelayMs);
        }
        else
        {
            fi.Player.AddViewmodelPositionKick(new Vector3(0, 0, 0.1f), 2);
            await Task.Delay(firearm.ManualFireDelayMs - 200);
            fi.Player.AddViewmodelPositionKick(new Vector3(0, 0, -0.05f), 2);
            await Task.Delay(200);
        }

        var poly = (AudioStreamPlaybackPolyphonic)fi.Player.AudioStreamPlayer3D.GetStreamPlayback();
        poly.PlayStream(firearm.ManualFireSound, bus: "Effects");

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

        if (AnimationPlayer.HasAnimation("reload"))
        {
            await TaskAnimation("reload", firearm.ReloadDelayMs);
        }
        else
        {
            fi.Player.AddViewmodelRotationKick(new Vector3(-1f, 0.5f, 0));
            await Task.Delay(firearm.ReloadDelayMs - 200);
            fi.Player.AddViewmodelPositionKick(new Vector3(0, 0, 0.1f));
            fi.Player.AddViewmodelRotationKick(new Vector3(0.2f, 0, 0));
            await Task.Delay(200);
        }

        var poly = (AudioStreamPlaybackPolyphonic)fi.Player.AudioStreamPlayer3D.GetStreamPlayback();
        poly.PlayStream(firearm.ReloadSound, bus: "Effects");

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
            ViewTransform = Player.ViewGlobalTransform,
        };
    }

    private async Task TaskAnimation(string animation, int timeToTakeMs)
    {
        var length = (float)(AnimationPlayer.GetAnimation(animation).Length * 1000d);
        var timeScale = timeToTakeMs / length;
        AnimationPlayer.Play(animation, customSpeed: timeScale);
        await ToSignal(AnimationPlayer, AnimationPlayer.SignalName.AnimationFinished);
    }
}