namespace MurderFloor;

public partial class Player : Pawn
{
    public static Player Self { get; private set; }
    public static long SelfId { get; private set; }
    public static List<Player> AllPlayers { get; private set; } = [];

    public Vector3 ViewPosition => viewAim.GlobalPosition;
    public Transform3D ViewTransform => viewAim.GlobalTransform;

    public float CameraShakeScale { get; set; }
    public Vector3 CameraRotationKick { get; set; }
    public Vector3 ViewModelPositionKick { get; set; }
    public Vector3 ViewModelRotationKick { get; set; }
    private Vector3 viewModelAimSway;

    public Node3D WorldToolPosition { get; private set; }

    [Export]
    public Node ToolsNode; // this is the synced node tool inventory
    [Export]
    public AudioStreamPlayer3D AudioStreamPlayer3D { get; private set; }

    [Export]
    public Vector2 ViewAngle { get; set; } = Vector2.Zero;
    private Vector2 lastViewAngle = Vector2.Zero;

    public string UseInfoText { get; set; } = "";

    // world
    [Export]
    private Node3D worldModels;

    private BoneAttachment3D worldHandBone;

    private Node3D worldBody;
    private Skeleton3D worldBodySkeleton;
    private AnimationPlayer worldBodyAnimationPlayer;

    // view
    [Export]
    public Node3D ViewAimViewmodel { get; private set; } // ViewModel attachment point
    [Export]
    public Camera3D Camera { get; private set; } // offsets from viewAim
    [Export]
    private Node3D viewAim; // real aim of view / weapons. camera is offset from this
    [Export]
    private RayCast3D cameraRaycast;

    private BoneAttachment3D viewHandBone;

    private Node3D viewBody;
    private Skeleton3D viewBodySkeleton;

    private Input.MouseModeEnum mouseMode = Input.MouseModeEnum.Captured;
    [Export(PropertyHint.Range, "1,500,0.01")]
    private float sensitivity = 50f;

    private Control openUI;
    private Control debugUI;

    private Vector3 lastVel;
    private Vector2 mouseDelta;

    public static Player FindPlayer(int playerId) => AllPlayers.First(p => p.GetMultiplayerAuthority() == playerId);

    public override void _EnterTree()
    {
        AllPlayers.Add(this);

        if (!IsMultiplayerAuthority()) return;

        Self = this;
        Input.UseAccumulatedInput = false;
    }

    public override void _ExitTree()
    {
        AllPlayers.Remove(this);
    }

    public override void _Ready()
    {
        BuildWorldNodes();
        AudioStreamPlayer3D.Play();

        if (!IsMultiplayerAuthority())
        {
            foreach (var child in GetChildren()) if (child is Control) child.Free();
            cameraRaycast.Free();
            Camera.Free();
            return;
        }

        cameraRaycast.AddException(this);
        SelfId = GetMultiplayerAuthority();
        Camera.Current = true;
        Rpc("ToolAddRpc", "base:testpistol");
        Rpc("ToolAddRpc", "base:testgodpistol");
        Rpc("ToolAddRpc", "base:testshotgun");
        Rpc("ToolAddRpc", "base:testassaultrifle");
        worldModels.Free();
    }

    public override void _Input(InputEvent @event)
    {
        if (!IsMultiplayerAuthority()) return;
        if (mouseMode != Input.MouseModeEnum.Captured) return;

        if (@event is InputEventMouseMotion eventMouseMotion)
        {
            var mouse = eventMouseMotion.ScreenRelative * 0.0001f * sensitivity;
            mouseDelta += mouse;
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
                NetworkManager.Current.Rpc("LoadGame", "res://scenes/map/dev/Dev.tscn");
            }
        }
    }

    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority())
        {
            worldBodyAnimationPlayer.Play("pose_idle");

            viewAim.Rotation = new Vector3(ViewAngle.Y, 0, 0);
            Rotation = new Vector3(0, ViewAngle.X, 0);
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

        // X = horizontal, Y = vertical
        ViewAngle = new Vector2(ViewAngle.X - mouseDelta.X, float.Clamp(ViewAngle.Y - mouseDelta.Y, -1.4f, 1.4f));
        mouseDelta = Vector2.Zero;

        Rotation = new Vector3(0, ViewAngle.X, 0);
        viewAim.Rotation = new Vector3(ViewAngle.Y, 0, 0);

        var reduction = 1f - ((float)delta * 6);
        CameraRotationKick *= reduction;
        ViewModelPositionKick *= reduction;
        ViewModelRotationKick *= reduction;

        viewModelAimSway += new Vector3(ViewAngle.X - lastViewAngle.X, lastViewAngle.Y - ViewAngle.Y, 0) * 0.04f;
        viewModelAimSway *= reduction;
        viewModelAimSway = viewModelAimSway.Normalized() * Mathf.Min(viewModelAimSway.Length(), 0.06f);
        lastViewAngle = ViewAngle;

        var shakeReduction = 1f - ((float)delta * 16);
        CameraShakeScale *= shakeReduction;

        Camera.Rotation = CameraRotationKick;
        ViewAimViewmodel.Position = ViewModelPositionKick + viewModelAimSway;
        ViewAimViewmodel.Rotation = ViewModelRotationKick;
        if (CameraShakeScale > 0.001f) Camera.Position = new Vector3(0, Random.Shared.NextSingle(), Random.Shared.NextSingle()) * CameraShakeScale;
        else Camera.Position = Vector3.Zero;

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
            if (Input.IsActionPressed("fire1")) SelectedTool.PrimaryInputState = 1;
            else if (Input.IsActionJustReleased("fire1")) SelectedTool.PrimaryInputState = 2;
            else SelectedTool.PrimaryInputState = 0;

            if (Input.IsActionPressed("fire2")) SelectedTool.SecondaryInputState = 1;
            else if (Input.IsActionJustReleased("fire2")) SelectedTool.SecondaryInputState = 2;
            else SelectedTool.SecondaryInputState = 0;

            if (Input.IsActionPressed("reload")) SelectedTool.ReloadInputState = 1;
            else if (Input.IsActionJustReleased("reload")) SelectedTool.ReloadInputState = 2;
            else SelectedTool.ReloadInputState = 0;
        }

        if (Input.IsActionJustPressed("jump"))
        {
            lastVel.Y = 11f;
        }

        var forward = Input.GetAxis("backward", "forward");
        var strafe = Input.GetAxis("left", "right");
        var wishMove = new Vector3(forward, 0f, strafe).Normalized() * 0.90f;
        wishMove.Y = -0.4f;
        wishMove = wishMove.Rotated(Vector3.Up, ViewAngle.X + 1.570796326794896f);
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

        WorldToolPosition = (Node3D)worldModels.GetChild(1);
        // reparent/reowner worldtool to handbone
        WorldToolPosition.GetParent().RemoveChild(WorldToolPosition);
        WorldToolPosition.Owner = null;
        worldHandBone.AddChild(WorldToolPosition);
        WorldToolPosition.Owner = worldHandBone;
    }
}
