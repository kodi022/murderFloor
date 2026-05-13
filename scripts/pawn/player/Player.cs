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

    private BoneAttachment3D viewHandBone;

    private Node3D viewModels;
    private Node3D viewBodyScene;
    private Node3D viewTool;
    private Skeleton3D viewSkeleton;
    private AnimationPlayer viewAnimationPlayer;

    [Export(PropertyHint.Range, "1,500,0.01")]
    private float sensitivity = 50f;

    private Vector3 lastVel;

    [Export]
    private float viewYaw = 0f;
    [Export]
    private float viewPitch = 0f;

    private Input.MouseModeEnum mouseMode = Input.MouseModeEnum.Captured;

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
