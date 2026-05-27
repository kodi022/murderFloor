namespace MurderFloor;

public partial class Player : Pawn
{
    public static Player Self { get; private set; }
    public static List<Player> AllPlayers { get; private set; } = [];

    public Vector3 ViewPosition => viewAim.GlobalPosition;
    public Transform3D ViewTransform => viewAim.GlobalTransform;

    public Vector3 CameraRotationKick { get; set; }
    public Vector3 ViewModelPositionKick { get; set; }
    public Vector3 ViewModelRotationKick { get; set; }

    public Node3D ViewToolPosition; // tool models are attached to this

    [Export]
    public Node ToolsNode; // this is the synced node tool inventory
    [Export]
    public AudioStreamPlayer3D AudioStreamPlayer3D { get; private set; }

    [Export]
    public float ViewYaw { get; set; } = 0f;
    [Export]
    public float ViewPitch { get; set; } = 0f;

    public string UseInfoText { get; set; } = "";

    // world
    [Export]
    private Node3D worldModels;

    private BoneAttachment3D worldHandBone;

    private Node3D worldBody;
    private Skeleton3D worldBodySkeleton;
    private AnimationPlayer worldBodyAnimationPlayer;
    private Node3D worldToolPosition;

    // view
    [Export]
    public Camera3D Camera { get; private set; }
    [Export]
    private Node3D viewAim; // real aim of view / weapons. camera is offset from this
    [Export]
    private RayCast3D cameraRaycast;

    private BoneAttachment3D viewHandBone;

    private Node3D viewModels;
    private Node3D viewBody;
    private Skeleton3D viewBodySkeleton;
    private AnimationPlayer viewBodyAnimationPlayer;

    private Input.MouseModeEnum mouseMode = Input.MouseModeEnum.Captured;
    [Export(PropertyHint.Range, "1,500,0.01")]
    private float sensitivity = 50f;

    private Control openUI;
    private Control debugUI;

    private Vector3 lastVel;

    public static Player FindPlayer(int playerId) => AllPlayers.First(p => p.GetMultiplayerAuthority() == playerId);

    public override void _Ready()
    {
        BuildWorldNodes();
        AudioStreamPlayer3D.Play();

        if (!IsMultiplayerAuthority()) return;

        Rpc("ToolAdd", "base:testassaultrifle");
        Rpc("ToolAdd", "base:testpistol");
        worldModels.Free();
        BuildViewNodes();
    }

    public override void _EnterTree()
    {
        AllPlayers.Add(this);

        if (!IsMultiplayerAuthority()) return;
        Self = this;
        Camera.Current = true;
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
            ViewPitch -= eventMouseMotion.ScreenRelative.Y * 0.0001f * sensitivity;
            ViewPitch = float.Clamp(ViewPitch, -1.3f, 1.3f);

            ViewYaw -= eventMouseMotion.ScreenRelative.X * 0.0001f * sensitivity;
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

            if (eventKey.Keycode == Key.F2 && eventKey.Pressed)
            {
                NetworkManager.Current.Rpc("LoadGame", "res://scenes/mapdev/Dev.tscn");
            }
        }
    }

    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority())
        {
            worldBodyAnimationPlayer.Play("pose_idle");
            return;
        }

        Input.MouseMode = mouseMode;

        if (Input.IsActionJustPressed("exit"))
        {
            if (openUI is null)
            {
                OpenUI("res://scenes/ui/Menu.tscn");
            }
            else
            {
                CloseUI();
            }
        }

        var reduction = 1f - ((float)delta * 6);
        CameraRotationKick *= reduction;
        ViewModelPositionKick *= reduction;
        ViewModelRotationKick *= reduction;

        Camera.Rotation = CameraRotationKick;
        viewModels.Position = ViewModelPositionKick;
        viewModels.Rotation = ViewModelRotationKick;

        viewAim.Rotation = new Vector3(ViewPitch, 0, 0);
        Rotation = new Vector3(0, ViewYaw, 0);

        if (openUI is not null) return;

        if (Input.IsActionJustPressed("selectprimary")) SelectToolBySlot(Tool.SlotEnum.Primary);
        if (Input.IsActionJustPressed("selectsecondary")) SelectToolBySlot(Tool.SlotEnum.Secondary);
        if (Input.IsActionJustPressed("selectspecial")) SelectToolBySlot(Tool.SlotEnum.Special);
        if (Input.IsActionJustPressed("selectmelee")) SelectToolBySlot(Tool.SlotEnum.Melee);

        if (cameraRaycast.GetCollider() is Node3D node)
        {
            if (node.GetParent() is Usable usable)
            {
                usable.UsableHit();

                if (Input.IsActionJustPressed("interact"))
                {
                    usable.UsableInvoke();
                }
            }
            else
            {
                UseInfoText = "";
            }
        }
        else
        {
            UseInfoText = "";
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsMultiplayerAuthority()) return;

        if (openUI is not null) return;

        if (SelectedTool is not null)
        {
            viewBodyAnimationPlayer.Play(SelectedTool.ToolResource.HoldTypeAnimation);

            if (Input.IsActionPressed("fire1"))
            {
                SelectedTool.FirePrimary();
            }

            if (Input.IsActionJustReleased("fire1"))
            {
                SelectedTool.UnFirePrimary();
            }

            if (Input.IsActionPressed("reload"))
            {
                SelectedTool.FireReload();
            }

        }

        // ! block movement input if menu open
        if (Input.IsActionJustPressed("jump"))
        {
            lastVel.Y = 16f;
        }

        var forward = Input.GetAxis("backward", "forward");
        var strafe = Input.GetAxis("left", "right");
        var wishMove = new Vector3(forward, 0f, strafe).Normalized() * 0.80f;
        wishMove.Y = -0.8f;
        wishMove = wishMove.Rotated(Vector3.Up, ViewYaw + 1.570796326794896f);
        lastVel *= new Vector3(0.80f, 0.95f, 0.80f);
        lastVel += wishMove;

        Velocity = lastVel;
        MoveAndSlide();
        lastVel = Velocity;
    }

    public void OpenUI(string uiScene)
    {
        if (openUI is not null) return;

        var ui = (Control)GD.Load<PackedScene>(uiScene).Instantiate();
        openUI = ui;
        AddChild(ui);
        mouseMode = Input.MouseModeEnum.Visible;
    }

    public void CloseUI()
    {
        if (openUI is null) return;

        openUI.Free();
        openUI = null;
        mouseMode = Input.MouseModeEnum.Captured;
    }

    private void BuildWorldNodes()
    {
        worldBody = (Node3D)worldModels.GetChild(0);
        worldBodySkeleton = (Skeleton3D)worldBody.GetChild(0).GetChild(0);
        worldBodyAnimationPlayer = (AnimationPlayer)worldBody.GetChild(1);

        worldHandBone = new BoneAttachment3D();
        worldBodySkeleton.AddChild(worldHandBone);
        worldHandBone.BoneName = "Hand.R";

        worldToolPosition = (Node3D)worldModels.GetChild(1);
        // reparent/reowner worldtool to handbone
        worldToolPosition.GetParent().RemoveChild(worldToolPosition);
        worldToolPosition.Owner = null;
        worldHandBone.AddChild(worldToolPosition);
        worldToolPosition.Owner = worldHandBone;
    }

    private void BuildViewNodes()
    {
        viewModels = (Node3D)GD.Load<PackedScene>("res://scenes/PlayerViewmodel.tscn").Instantiate();
        viewModels.Position = Vector3.Zero;
        viewModels.Rotation = Vector3.Zero;
        viewBody = (Node3D)viewModels.GetChild(0);
        viewBodySkeleton = (Skeleton3D)viewBody.GetChild(0).GetChild(0);
        viewBodyAnimationPlayer = (AnimationPlayer)viewBody.GetChild(1);
        viewAim.AddChild(viewModels);

        ViewToolPosition = (Node3D)viewModels.GetChild(1);
        viewHandBone = new BoneAttachment3D();
        viewBodySkeleton.AddChild(viewHandBone);
        viewHandBone.BoneName = "Hand.R";
        // reparent/reowner viewtool to handbone
        ViewToolPosition.GetParent().RemoveChild(ViewToolPosition);
        ViewToolPosition.Owner = null;
        viewHandBone.AddChild(ViewToolPosition);
        ViewToolPosition.Owner = viewHandBone;
    }
}
