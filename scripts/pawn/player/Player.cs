namespace MurderFloor;

public partial class Player : Pawn
{
    public static List<Player> AllPlayers { get; private set; } = [];
    public static Player Self { get; private set; }

    public int Id { get; private set; }

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
    public Vector3 BodyVelocity { get; set; } = Vector3.Zero;

    [Export]
    public Vector2 ViewAngle { get; set; } = Vector2.Zero;
    private Vector2 lastViewAngle = Vector2.Zero;

    public string UseInfoText { get; set; } = "";

    // * world
    [Export]
    private Node3D worldModels;
    [Export]
    private AnimationTree worldAnimationTree;
    private BoneAttachment3D worldHandBone;
    private Node3D worldBody;

    // * view
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

    // * other
    private Input.MouseModeEnum mouseMode = Input.MouseModeEnum.Captured;
    [Export(PropertyHint.Range, "1,500,0.01")]
    private float sensitivity = 50f;

    private Control openUI;
    private Control debugUI;

    private Vector3 lastVel;
    private Vector2 mouseDelta;

    public string Hold = "";

    public static Player FindPlayer(int playerId) => AllPlayers.First(p => p.Id == playerId);

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
        Id = GetMultiplayerAuthority();

        if (!IsMultiplayerAuthority())
        {
            foreach (var child in GetChildren()) if (child is Control) child.Free();
            cameraRaycast.Free();
            Camera.Free();

            //Player.Self.RpcId(peerId, "ToolsResyncRpc", Player.Self.GetAllTools());
            NetworkManager.Current.RpcId(Id, "ClientPlayerReady");

            return;
        }

        cameraRaycast.AddException(this);
        Camera.Current = true;
        Rpc("ToolAddRpc", "base:testpistol");
        Rpc("ToolAddRpc", "base:testgodpistol");
        Rpc("ToolAddRpc", "base:testshotgun");
        Rpc("ToolAddRpc", "base:shotgun1");
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

            if (eventKey.Keycode == Key.F3 && eventKey.Pressed)
            {
                var tierCount = new Dictionary<Loot.LootTierEnum, int>();
                var wearCount = new Dictionary<Loot.LootWearEnum, int>();
                var lootCount = 1_000_000;
                var level = 100;
                for (int i = 0; i < lootCount; i++)
                {
                    var e = new Loot.LootRarityInfo(Random.Shared.Next(), level, Game.GameDifficultyEnum.Easy);
                    if (!tierCount.TryAdd(e.LootTier, 1))
                        tierCount[e.LootTier] += 1;

                    if (!wearCount.TryAdd(e.LootWear, 1))
                        wearCount[e.LootWear] += 1;
                }

                GD.Print($"-- Generated {lootCount} loot at level {level}");
                GD.Print(string.Join(", ", tierCount.OrderByDescending(a => (int)a.Key)));
                GD.Print(string.Join(", ", wearCount.OrderBy(a => (int)a.Key)));
            }
        }
    }

    public override void _Process(double delta)
    {
        var reduction = 1f - ((float)delta * 6);
        CameraRotationKick *= reduction;
        ViewModelPositionKick *= reduction;
        ViewModelRotationKick *= reduction;

        var shakeReduction = 1f - ((float)delta * 16);
        CameraShakeScale *= shakeReduction;

        if (!IsMultiplayerAuthority())
        {
            var vel = BodyVelocity.Length() * 0.4f;
            worldAnimationTree.Set("parameters/timescale_walk/scale", vel < 0.1f ? 0f : vel);
            Hold = SelectedTool?.ToolResource.HoldTypeAnimation ?? "";
            //worldAnimationTree.Set("parameters/StateMachine/conditions/holdtype", SelectedTool?.ToolResource.HoldTypeAnimation ?? "");
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

        viewModelAimSway += new Vector3(ViewAngle.X - lastViewAngle.X, lastViewAngle.Y - ViewAngle.Y, 0) * 0.04f;
        viewModelAimSway *= reduction;
        viewModelAimSway = viewModelAimSway.Normalized() * Mathf.Min(viewModelAimSway.Length(), 0.06f);
        lastViewAngle = ViewAngle;

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
                UseInfoText = usable.UseInfoText;

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
        BodyVelocity = Velocity;
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
        var skeleton = (Skeleton3D)worldBody.GetChild(0).GetChild(0);

        worldHandBone = new BoneAttachment3D();
        skeleton.AddChild(worldHandBone);
        worldHandBone.BoneName = "Hand.R";

        WorldToolPosition = (Node3D)worldModels.GetChild(1);
        // reparent/reowner worldtool to handbone
        WorldToolPosition.GetParent().RemoveChild(WorldToolPosition);
        WorldToolPosition.Owner = null;
        worldHandBone.AddChild(WorldToolPosition);
        WorldToolPosition.Owner = worldHandBone;
    }
}
