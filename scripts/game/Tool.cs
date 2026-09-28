namespace Shooter.Game;

using Resource;

// Node because this is just data, the viewmodel contains the 3d stuff
public partial class Tool : Node
{
    private const string ToolAnimLibraryKey = "a";

    [Export]
    public int PlayerId { get; set; }
    // reference to player
    public Player Player { get; private set; }

    [Export]
    public string ToolFullId { get; set; }
    // reference to tool
    public Resource.Tool ToolResource { get; private set; }

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

    // toolfirearm
    public Vector2 CurrentSpread { get; private set; }
    public Vector2 MinSpread { get; private set; }
    public Vector2 MaxSpread { get; private set; }

    [Export]
    public int CurrentMag { get; private set; } = 0;

    public bool Aiming { get; private set; } = false;
    public float OpticZoom => BuiltTool.OpticZoom == 0f ? 1.1f : BuiltTool.OpticZoom;

    public GameResource.BuiltToolData BuiltTool { get; private set; }

    // public so ToolFirearm can set rotations
    public List<GpuParticles3D> BulletParticles { get; private set; }
    // public to get global position
    public GpuParticles3D MuzzleFlashParticle { get; private set; }

    public int CurrentReserve { get; private set; } = 0;
    public bool Reloading { get; private set; } = false;
    private ulong rpmAsMs = 0;
    private ulong msSinceFire = 0;
    private bool bolting = false;
    private bool shotSemi = false;
    private bool shotBolt = false;

    private bool equipped = false;

    private float currentAimingPositionLerp;

    private Node3D viewmodelScene;
    private Vector3 viewmodelSceneStartPos;
    private Vector3 viewmodelSceneSightPosition;

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
        viewmodelScene.Position = viewmodelSceneStartPos.Lerp(viewmodelSceneSightPosition + BuiltTool.SightPositionOffset, currentAimingPositionLerp);

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

    public async Task Equip(bool viewing = false)
    {
        if (IsMultiplayerAuthority() || viewing)
        {
            EquipViewing();
        }
        else
        {
            EquipWorld();
        }

        if (!viewing)
        {
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
        }

        AnimationPlayer.Play("idle");
        GD.Print(string.Join(',', AnimationPlayer.GetAnimationList()));
        equipped = true;
    }

    private void EquipViewing()
    {
        foreach (var child in Player.Viewmodel.GetChildren())
        {
            child.Free();
        }
        viewmodelScene = ToolResource.ViewmodelScene.Instantiate<Node3D>();
        viewmodelScene.RotationDegrees += new Vector3(0, 90, 0);

        if (ToolResource.FullId != "base:fists")
        {
            var oldTool = (Node3D)viewmodelScene.FindChild("Armature*");
            var oldToolPos = oldTool.Position;
            var oldToolRot = oldTool.Rotation;
            oldTool.Free();

            BuiltTool = ToolResource.BuildToolScene(ToolConfig);
            var newTool = BuiltTool.Tool.GetChild<Node3D>(0);
            newTool.Owner = null;
            newTool.Position = oldToolPos;
            newTool.Rotation = oldToolRot;
            newTool.Reparent(viewmodelScene, false);

            if (ToolResource is ToolFirearm tf)
            {
                viewmodelSceneSightPosition = new Vector3(-oldToolPos.Z, -oldToolPos.Y, 0);

                var flash = GD.Load<PackedScene>("res://scenes/particle/GunFlash.tscn");
                var flashInst = flash.Instantiate<Node3D>();
                flashInst.Position = BuiltTool.MuzzlePosition;
                newTool.AddChild(flashInst);
                MuzzleFlashParticle = flashInst.GetChild<GpuParticles3D>(0);

                BulletParticles = [];
                for (int i = 0; i < tf.PelletCount; i++)
                {
                    var bullet = GD.Load<PackedScene>("res://scenes/particle/GunBullet.tscn");
                    var bulletInst = bullet.Instantiate<Node3D>();
                    bulletInst.Position = BuiltTool.MuzzlePosition;
                    var farticles = bulletInst.GetChild<GpuParticles3D>(0);
                    farticles.MaterialOverride = GD.Load<StandardMaterial3D>("res://materials/particle/bullet.tres");
                    Global.GameManager.ClearOnLoad.AddChild(bulletInst);
                    BulletParticles.Add(farticles);
                }
            }
        }

        Player.Viewmodel.AddChild(viewmodelScene);
        viewmodelScene.Position = Vector3.Zero;
        viewmodelSceneStartPos = viewmodelScene.Position;
    }

    private void EquipWorld()
    {
        bool armatures = false;
        foreach (var child in Player.WorldModels.GetChild(0).GetChildren())
        {
            if (child.Name == "Armature") armatures = true;
            if (armatures) child.Free();
        }

        viewmodelScene = ToolResource.ViewmodelScene.Instantiate<Node3D>();

        if (ToolResource.FullId != "base:fists")
        {
            BuiltTool = ToolResource.BuildToolScene(ToolConfig);
            var armature = BuiltTool.Tool.GetChild<Node3D>(0);
            var boneIndex = Player.WorldSkeleton.FindBone("Hand.R");
            var pos = Player.WorldSkeleton.GetBonePose(boneIndex);
            armature.Transform = pos;

            if (ToolResource is ToolFirearm tf)
            {
                var flash = GD.Load<PackedScene>("res://scenes/particle/GunFlash.tscn");
                var flashInst = flash.Instantiate<Node3D>();
                flashInst.Position = BuiltTool.MuzzlePosition;
                BuiltTool.Tool.AddChild(flashInst);
                MuzzleFlashParticle = flashInst.GetChild<GpuParticles3D>(0);

                BulletParticles = [];
                for (int i = 0; i < tf.PelletCount; i++)
                {
                    var bullet = GD.Load<PackedScene>("res://scenes/particle/GunBullet.tscn");
                    var bulletInst = bullet.Instantiate<Node3D>();
                    bulletInst.Position = BuiltTool.MuzzlePosition;
                    var farticles = bulletInst.GetChild<GpuParticles3D>(0);
                    farticles.MaterialOverride = GD.Load<StandardMaterial3D>("res://materials/particle/bullet.tres");
                    Global.GameManager.ClearOnLoad.AddChild(bulletInst);
                    BulletParticles.Add(farticles);
                }
            }
        }

        var ap = (AnimationPlayer)viewmodelScene.FindChild("AnimationPlayer");
        Player.WorldAnimationTree.AddAnimationLibrary(ToolAnimLibraryKey, ap.GetAnimationLibrary(""));
    }

    public async Task Unequip(bool viewing = false)
    {
        if (!viewing)
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
        }

        foreach (var bullet in BulletParticles) bullet.Free();
        Player.WorldAnimationTree.RemoveAnimationLibrary(ToolAnimLibraryKey);
        BuiltTool.Tool?.Free();
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

    private void FirePrimaryFirearm(ToolFirearm firearm, Resource.Tool.FireInfo fi)
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

            MuzzleFlashParticle.Restart();

            shotSemi = true;
            shotBolt = true;
            CurrentSpread = (CurrentSpread + firearm.SpreadIncreasePerShot).Min(MaxSpread);
            CurrentMag--;
        }
    }

    private async void BoltFirearm(ToolFirearm firearm, Resource.Tool.FireInfo fi)
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

    private async void ReloadFirearm(ToolFirearm firearm, Resource.Tool.FireInfo fi)
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

    private Resource.Tool.FireInfo CreateFireInfo()
    {
        return new Resource.Tool.FireInfo()
        {
            Player = Player,
            Tool = this,
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