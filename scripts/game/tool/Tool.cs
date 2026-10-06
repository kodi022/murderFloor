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
    public Resource.Tool ToolResource { get; private set; }
    // contains all the completed data for the tool with everything on it
    public ToolConfig ToolConfig { get; set; }
    // the logic for the specific tool, such as melee, or firearm
    public ToolBehavior ToolBehavior { get; private set; }

    // Networked properties cannot be inside ToolBehavior
    // 0 is no, 1 is yes, 2 is just released
    [Export]
    public int PrimaryInputState { get; set; } = 0;
    [Export]
    public int SecondaryInputState { get; set; } = 0;
    [Export]
    public int ReloadInputState { get; set; } = 0;

    [Export]
    public int CurrentMag { get; set; } = 0;

    public AnimationPlayer AnimationPlayer { get; private set; }

    public bool Aiming { get; set; } = false;
    public float OpticZoom => BuiltTool.OpticZoom == 0f ? 1.1f : BuiltTool.OpticZoom;

    public GameResource.BuiltToolData BuiltTool { get; private set; }

    // public so ToolFirearm can set rotations
    public List<GpuParticles3D> BulletParticles { get; private set; }
    // public to get global position
    public GpuParticles3D MuzzleFlashParticle { get; private set; }

    private bool equipped = false;

    private float currentAimingPositionLerp;

    private Node3D viewmodelScene;
    private Vector3 viewmodelSceneStartPos;
    private Vector3 viewmodelSceneSightPosition;

    public override void _Ready()
    {
        Player = Player.FindPlayer(PlayerId);
        ToolResource = ResourceManager.ToolRegistry.GetResourceRef(ToolFullId);

        if (ToolResource is ToolFirearm)
        {
            CurrentMag = (int)ToolConfig.GetStat(ToolFirearm.PropertyName.MagSize);
            ToolBehavior = new ToolBehaviorFirearm() { ParentTool = this };
            ToolBehavior.Ready();
        }

        if (ToolResource is ToolMelee)
        {
            ToolBehavior = new ToolBehaviorMelee() { ParentTool = this };
            ToolBehavior.Ready();
        }
    }

    public override void _Process(double delta)
    {
        if (!equipped) return;

        //AnimationPlayer?.Play(ToolResource.HoldTypeAnimation);

        currentAimingPositionLerp += Aiming ? (float)delta * 4f : -(float)delta * 4f;
        currentAimingPositionLerp = Mathf.Clamp(currentAimingPositionLerp, 0f, 1f);
        viewmodelScene.Position = viewmodelSceneStartPos.Lerp(viewmodelSceneSightPosition + BuiltTool.SightPositionOffset, currentAimingPositionLerp);

        ToolBehavior.Process(delta);

        if (PrimaryInputState == 1) FirePrimary();
        if (PrimaryInputState == 2) UnfirePrimary();

        if (SecondaryInputState == 1) FireSecondary();
        if (SecondaryInputState == 2) UnfireSecondary();

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
        equipped = false;
        Aiming = false;

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

        Player.WorldAnimationTree.RemoveAnimationLibrary(ToolAnimLibraryKey);

        if (ToolResource is ToolFirearm)
        {
            foreach (var bullet in BulletParticles) if (IsInstanceValid(bullet)) bullet.Free();
        }

        BuiltTool.Tool?.Free();
        viewmodelScene?.Free();
        viewmodelScene = null;
    }

    public void FirePrimary()
    {
        if (!equipped) return;
        ToolBehavior.FirePrimary(CreateFireInfo());
    }

    public void UnfirePrimary()
    {
        if (!equipped) return;
        ToolBehavior.UnfirePrimary();
    }

    public void FireSecondary()
    {
        if (!equipped) return;
        ToolBehavior.FireSecondary();
    }

    public void UnfireSecondary()
    {
        if (!equipped) return;
        ToolBehavior.UnfireSecondary();
    }

    public void FireReload()
    {
        if (!equipped) return;
        ToolBehavior.Reload(CreateFireInfo());
    }

    public async Task TaskAnimation(string animation, int timeToTakeMs)
    {
        var length = (float)(AnimationPlayer.GetAnimation(animation).Length * 1000d);
        var timeScale = timeToTakeMs / length;
        AnimationPlayer.Play(animation, customSpeed: timeScale);
        await ToSignal(AnimationPlayer, AnimationPlayer.SignalName.AnimationFinished);
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
}