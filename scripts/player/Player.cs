namespace MurderFloor;

public partial class Player : CharacterBody3D
{
    [Export]
    private Node3D worldModels;

    private BoneAttachment3D worldHandBone;

    private Node3D worldBodyScene;
    private Node3D worldTool;
    private Skeleton3D worldSkeleton;
    private AnimationPlayer worldAnimationPlayer;

    [Export]
    private Camera3D camera;
    [Export]
    private Node3D viewHandPoint;

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
        // worldHandBone.BoneName = "forearm_right";

        if (!IsMultiplayerAuthority()) return;

        worldModels.Free();

        camera.Current = true;
        Input.UseAccumulatedInput = false;

        var tool = ResourceManager.ToolRegistry.GetResourceReference("base:testassaultrifle");

        viewModels = (Node3D)GD.Load<PackedScene>("res://scenes/PlayerViewmodel.tscn").Instantiate();
        viewBodyScene = (Node3D)viewModels.GetChild(0);
        viewTool = (Node3D)viewModels.GetChild(1);
        viewSkeleton = (Skeleton3D)viewBodyScene.GetChild(0).GetChild(0);
        //viewAnimationPlayer = (AnimationPlayer)viewBodyScene.GetChild(1);

        viewHandPoint.AddChild(viewModels);
        //viewSkeleton.SetBonePoseScale(viewSkeleton.FindBone("spine_3"), new Vector3(2, 2, 2));
        viewHandBone = new BoneAttachment3D();
        viewSkeleton.AddChild(viewHandBone);
        viewHandBone.BoneName = "Hand.R";
        viewTool.GetParent().RemoveChild(viewTool);
        viewTool.Owner = null;
        viewHandBone.AddChild(viewTool);
        viewTool.Owner = viewHandBone;

        viewTool.AddChild(tool.MeshScene.Instantiate());
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!IsMultiplayerAuthority()) return;

        if (mouseMode != Input.MouseModeEnum.Captured) return;

        if (@event is InputEventMouseMotion eventMouseMotion)
        {
            var viewportTransform = GetTree().Root.GetFinalTransform();
            var relative = ((InputEventMouseMotion)eventMouseMotion.XformedBy(viewportTransform)).Relative;
            viewPitch -= relative.Y * 0.0001f * sensitivity;
            viewPitch = float.Clamp(viewPitch, -1.3f, 1.3f);

            viewYaw -= relative.X * 0.0001f * sensitivity;
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

        camera.Rotation = new Vector3(viewPitch, 0, 0);
        Rotation = new Vector3(0, viewYaw, 0);
    }

    public override void _PhysicsProcess(double delta)
    {
        //GD.Print(Name + IsMultiplayerAuthority());
        if (!IsMultiplayerAuthority()) return;

        var forward = Input.GetAxis("backward", "forward");
        var strafe = Input.GetAxis("left", "right");
        var wishMove = new Vector3(forward, -1f, strafe);
        wishMove = wishMove.Normalized();
        wishMove = wishMove.Rotated(Vector3.Up, viewYaw + 1.570796326794896f);
        lastVel *= new Vector3(0.86f, 0.95f, 0.86f);
        lastVel += wishMove;

        Velocity = lastVel;
        MoveAndSlide();
        lastVel = Velocity;
    }
}
