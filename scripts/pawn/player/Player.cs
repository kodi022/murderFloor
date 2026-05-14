namespace MurderFloor;

public partial class Player : Pawn
{
    public static Player Self { get; private set; }
    public static List<Player> AllPlayers { get; private set; } = [];

    public Vector3 CameraPositionBump { get; set; }
    public Vector3 CameraRotationBump { get; set; }

    public Vector3 ViewModelPositionBump { get; set; }
    public Vector3 ViewModelRotationBump { get; set; }

    public List<Tool> PrimaryTools { get; private set; } = [];
    public List<Tool> SecondaryTools { get; private set; } = [];
    public List<Tool> GadgetTools { get; private set; } = [];

    public Tool SelectedTool { get; private set; } = null;

    // world
    [Export]
    private Node3D worldModels;

    private BoneAttachment3D worldHandBone;

    private Node3D worldBodyScene;
    private Node3D worldTool;
    private Skeleton3D worldSkeleton;
    private AnimationPlayer worldAnimationPlayer;

    // view
    [Export]
    private Camera3D camera;
    [Export]
    private RayCast3D cameraRaycast;

    private BoneAttachment3D viewHandBone;

    private Node3D viewModels;
    private Node3D viewBodyScene;
    private Node3D viewTool;
    private Skeleton3D viewSkeleton;
    private AnimationPlayer viewAnimationPlayer;

    private Input.MouseModeEnum mouseMode = Input.MouseModeEnum.Captured;
    [Export(PropertyHint.Range, "1,500,0.01")]
    private float sensitivity = 50f;
    [Export]
    private float viewYaw = 0f;
    [Export]
    private float viewPitch = 0f;

    private Control debugUI;

    private Vector3 lastVel;

    public override void _Ready()
    {
        // worldBodyScene = (Node3D)worldModels.GetChild(0);
        // worldTool = (Node3D)worldModels.GetChild(1);
        // worldSkeleton = (Skeleton3D)worldBodyScene.GetChild(0).GetChild(0);
        // worldAnimationPlayer = (AnimationPlayer)worldBodyScene.GetChild(1);

        // worldHandBone = new BoneAttachment3D();
        // worldSkeleton.AddChild(worldHandBone);
        // worldHandBone.BoneName = "Hand.R";

        if (!IsMultiplayerAuthority()) return;

        worldModels.Free();

        var tool = ResourceManager.ToolRegistry.GetResourceReference("base:testassaultrifle");

        viewModels = (Node3D)GD.Load<PackedScene>("res://scenes/PlayerViewmodel.tscn").Instantiate();
        viewModels.Position = Vector3.Zero;
        viewModels.Rotation = Vector3.Zero;
        viewBodyScene = (Node3D)viewModels.GetChild(0);
        viewTool = (Node3D)viewModels.GetChild(1);
        viewSkeleton = (Skeleton3D)viewBodyScene.GetChild(0).GetChild(0);
        camera.AddChild(viewModels);
        //viewAnimationPlayer = (AnimationPlayer)viewBodyScene.GetChild(1);

        viewHandBone = new BoneAttachment3D();
        viewSkeleton.AddChild(viewHandBone);
        viewHandBone.BoneName = "Hand.R";
        viewTool.GetParent().RemoveChild(viewTool);
        viewTool.Owner = null;
        viewHandBone.AddChild(viewTool);
        viewTool.Owner = viewHandBone;
        viewTool.AddChild(tool.MeshScene.Instantiate());
    }

    public override void _EnterTree()
    {
        AllPlayers.Add(this);

        if (!IsMultiplayerAuthority()) return;
        Self = this;
        camera.Current = true;
        Input.UseAccumulatedInput = false;
        cameraRaycast.AddException(this);
    }

    public override void _ExitTree()
    {
        AllPlayers.Remove(this);
    }

    public override void _Input(InputEvent @event)
    {
        if (!IsMultiplayerAuthority()) return;
        if (mouseMode != Input.MouseModeEnum.Captured) return;

        if (@event is InputEventMouseMotion eventMouseMotion)
        {
            viewPitch -= eventMouseMotion.ScreenRelative.Y * 0.0001f * sensitivity;
            viewPitch = float.Clamp(viewPitch, -1.3f, 1.3f);

            viewYaw -= eventMouseMotion.ScreenRelative.X * 0.0001f * sensitivity;

            camera.Rotation = new Vector3(viewPitch, 0, 0);
            Rotation = new Vector3(0, viewYaw, 0);
        }

        if (@event is InputEventKey eventKey)
        {
            if (eventKey.Keycode == Key.F1 && eventKey.Pressed)
            {
                if (debugUI is null)
                {
                    debugUI = (Control)GD.Load<PackedScene>("res://scenes/ui/HUDDebug.tscn").Instantiate();
                    AddChild(debugUI);
                }
                else
                {
                    debugUI.Free();
                    debugUI = null;
                }
            }
        }
    }

    public override void _Process(double delta)
    {
        //animationPlayer.Play("idle");

        if (!IsMultiplayerAuthority()) return;

        Input.MouseMode = mouseMode;

        if (Input.IsActionJustPressed("exit"))
        {
            if (mouseMode == Input.MouseModeEnum.Captured) mouseMode = Input.MouseModeEnum.Visible;
            else mouseMode = Input.MouseModeEnum.Captured;
        }

        // camera.ProjectPosition aims at pixel, which isn't perfectly center
        // var endPos = camera.ProjectPosition(Vector2.Zero, 100f);
        // var endPos = camera.GlobalPosition - camera.GlobalTransform.Basis.Z * 100f;

        //cameraRaycast.TargetPosition = Vector3.Backward * range;
        if (cameraRaycast.GetCollider() is Pawn pawn)
        {
            var di = new Godot.Collections.Dictionary<string, string>()
            {
                {"attacker", GetMultiplayerAuthority().ToString()},
                {"attackerName", NetworkManager.Current._players[GetMultiplayerAuthority()]["Name"]},
                {"weapon", "mind"},
                {"hitposition", cameraRaycast.GetCollisionPoint().ToString()},
                {"hitbox", "0"}
            };
            pawn.OnDamage(di);
        }

    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsMultiplayerAuthority()) return;

        if (Input.IsActionJustPressed("jump"))
        {
            lastVel.Y = 16f;
        }

        var forward = Input.GetAxis("backward", "forward");
        var strafe = Input.GetAxis("left", "right");
        var wishMove = new Vector3(forward, 0f, strafe).Normalized() * 0.80f;
        wishMove.Y = -0.8f;
        wishMove = wishMove.Rotated(Vector3.Up, viewYaw + 1.570796326794896f);
        lastVel *= new Vector3(0.80f, 0.95f, 0.80f);
        lastVel += wishMove;

        Velocity = lastVel;
        MoveAndSlide();
        lastVel = Velocity;
    }
}
