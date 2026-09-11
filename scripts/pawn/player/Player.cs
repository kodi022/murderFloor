using MurderFloor.Loot;

namespace MurderFloor;

public partial class Player : Pawn
{
    public static List<Player> AllPlayers { get; private set; } = [];
    public static Player Self { get; private set; }
    public static Player Viewing { get; set; }

    public int Id { get; private set; }

    public Transform3D ViewGlobalTransform => viewModels.GlobalTransform;

    public Node3D WorldToolPosition { get; private set; }

    [Export]
    public Node ToolsNode { get; private set; } // this is the synced node tool inventory
    [Export]
    public AudioStreamPlayer3D AudioStreamPlayer3D { get; private set; }
    [Export]
    public Hud Hud { get; private set; }

    [Export]
    public Vector2 ViewAngle { get; set; } = Vector2.Zero;
    private Vector2 lastViewAngle = Vector2.Zero;

    // * world
    [Export]
    private Node3D worldModels; // root of worldmodels
    [Export]
    private AnimationTree worldAnimationTree;
    private BoneAttachment3D worldHandBone;

    // * view
    [Export]
    private Node3D viewModels; // root of viewmodels (+ aiming). camera is offset from this
    [Export]
    public Node3D ViewAimViewmodel { get; private set; } // ViewModel attachment point
    [Export]
    public Camera3D Camera { get; private set; } // offsets from viewAim
    [Export]
    private RayCast3D cameraRaycast;

    // * other
    public string UseInfoText { get; private set; } = "";

    private Input.MouseModeEnum mouseMode = Input.MouseModeEnum.Captured;

    private float cameraFovTarget;
    private float cameraFovCurrent;

    private float cameraShake;
    private Vector3 cameraRotationKick;

    private Vector3 viewmodelPositionKickTarget;
    private Vector3 viewmodelPositionKickCurrent;
    private float viewmodelPositionKicklerpScale;
    private Vector3 viewmodelRotationKick;
    private Vector3 viewmodelAimSway;

    private Vector2 mouseDelta;

    private OuterController outerController;
    private Control openUI;
    private Control debugUI;

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
        if (Viewing == this && this != Self) ViewPlayer(Self);
    }

    public override void _Ready()
    {
        Id = GetMultiplayerAuthority();
        AudioStreamPlayer3D.Play();

        // build world nodes
        var worldBody = (Node3D)worldModels.GetChild(0);
        var skeleton = (Skeleton3D)worldBody.GetChild(0).GetChild(0);
        worldHandBone = new BoneAttachment3D();
        skeleton.AddChild(worldHandBone);
        worldHandBone.BoneName = "Hand.R";
        WorldToolPosition = (Node3D)worldModels.GetChild(1);
        WorldToolPosition.GetParent().RemoveChild(WorldToolPosition);
        WorldToolPosition.Owner = null;
        worldHandBone.AddChild(WorldToolPosition);
        WorldToolPosition.Owner = worldHandBone;

        if (!IsMultiplayerAuthority())
        {
            cameraRaycast.Free();
            NetworkManager.Singleton.RpcId(Id, "ClientPlayerReady");
            return;
        }

        var fistToolConfig = new ToolConfig(LootState.Deserialize("0,a/Hw/,0.1.0,0,0,0,0,"));
        Rpc("ToolAddRpc", fistToolConfig.Serialize());

        foreach (var equipped in SaveManager.CurrentSave.GetEquippedLoot())
        {
            var toolConfig = new ToolConfig(equipped, SaveManager.CurrentSave.GetAttachmentsOnTool(equipped));
            Rpc("ToolAddRpc", toolConfig.Serialize());
        }

        var opt = OptionsManager.Load();
        OptionsManager.Apply(opt);
        OptionsMenu.ShowReturnButton = true;

        cameraRaycast.AddException(this);
        ViewPlayer(this);
    }

    public override void _Input(InputEvent @event)
    {
        if (!IsMultiplayerAuthority()) return;
        if (mouseMode != Input.MouseModeEnum.Captured) return;

        if (@event is InputEventMouseMotion eventMouseMotion)
        {
            var mouse = eventMouseMotion.ScreenRelative * 0.002f * OptionsManager.CurrentOptions.Sensitivity;
            mouseDelta += mouse;
        }

        if (@event is InputEventKey eventKey)
        {
            if (eventKey.Keycode == Key.F1 && eventKey.Pressed)
            {
                var index = (AllPlayers.IndexOf(Viewing) + 1) % AllPlayers.Count;
                ViewPlayer(AllPlayers[index]);
            }

            if (eventKey.Keycode == Key.F3 && eventKey.Pressed && OS.HasFeature("editor"))
            {
                if (!IsInstanceValid(debugUI))
                {
                    debugUI = GD.Load<PackedScene>("res://scenes/ui/hud/debug/HudDebug.tscn").Instantiate<Control>();
                    AddChild(debugUI);
                }
                else
                {
                    debugUI.Free();
                    debugUI = null;
                }
            }

            if (eventKey.Keycode == Key.F4 && eventKey.Pressed && OS.HasFeature("editor"))
            {
                if (!IsInstanceValid(openUI))
                {
                    OpenUI("res://scenes/ui/hud/debug/HudDebugMenus.tscn");
                }
                else
                {
                    CloseUI();
                }
            }

            if (eventKey.Keycode == Key.F5 && eventKey.Pressed)
            {
                NetworkManager.Singleton.Rpc("LoadGame", "res://scenes/map/barnyard/barnyard.tscn");
            }

            if (eventKey.Keycode == Key.F7 && eventKey.Pressed)
            {
                var di = new DamageInfo()
                {
                    Damage = 25,
                    DamageType = DamageInfo.DamageTypeEnum.Physical,
                    AttackerId = Id,
                    AttackerName = NetworkManager.Singleton._players[Id]["Name"],
                    HitboxName = "Neck"
                };
                Rpc("OnDamageRpc", di.ToVariant());
            }
        }
    }

    public override void _Process(double delta)
    {
        var reduction = 1f - ((float)delta * 6);
        cameraRotationKick *= reduction;
        viewmodelPositionKickTarget *= reduction;
        viewmodelRotationKick *= reduction;

        viewmodelPositionKickCurrent = viewmodelPositionKickCurrent.Lerp(viewmodelPositionKickTarget, (float)delta * 20f * viewmodelPositionKicklerpScale);

        var shakeReduction = 1f - ((float)delta * 15);
        cameraShake *= shakeReduction;

        if (!IsMultiplayerAuthority())
        {
            viewModels.Rotation = new Vector3(ViewAngle.Y, 0, 0);
            Rotation = new Vector3(0, ViewAngle.X, 0);

            var vel = NetworkedVelocity.Length() * 0.4f;
            if (vel < 0.1f)
            {
                worldAnimationTree.Set("parameters/moving/scale", 0f);
                worldAnimationTree.Set("parameters/timescale_walk/scale", 0f);
            }
            else
            {
                worldAnimationTree.Set("parameters/moving/scale", 1f);
                worldAnimationTree.Set("parameters/timescale_walk/scale", vel);
            }

            return;
        }

        Input.MouseMode = IsInputBlocked() ? Input.MouseModeEnum.Visible : Input.MouseModeEnum.Captured;
        Hud.Visible = !IsDead;

        if (Input.IsActionJustPressed("exit"))
        {
            if (!IsInstanceValid(openUI))
            {
                OpenUI("res://scenes/ui/Menu.tscn");
            }
            else
            {
                CloseUI();
            }
        }

        if (SelectedTool?.Aiming ?? false)
        {
            ViewAimViewmodel.Scale = new Vector3(1, 1, OptionsManager.CurrentOptions.AimingViewmodelFieldOfViewScale);
            viewmodelAimSway += new Vector3(ViewAngle.X - lastViewAngle.X, lastViewAngle.Y - ViewAngle.Y, 0) * 0.02f;
            viewmodelAimSway *= reduction;
            viewmodelAimSway = viewmodelAimSway.Normalized() * Mathf.Min(viewmodelAimSway.Length(), 0.014f);
            cameraFovTarget = OptionsManager.CurrentOptions.FieldOfView * (1f / SelectedTool.OpticZoom);
        }
        else
        {
            ViewAimViewmodel.Scale = new Vector3(1, 1, OptionsManager.CurrentOptions.ViewmodelFieldOfViewScale);
            viewmodelAimSway += new Vector3(ViewAngle.X - lastViewAngle.X, lastViewAngle.Y - ViewAngle.Y, 0) * 0.04f;
            viewmodelAimSway *= reduction;
            viewmodelAimSway = viewmodelAimSway.Normalized() * Mathf.Min(viewmodelAimSway.Length(), 0.06f);
            cameraFovTarget = OptionsManager.CurrentOptions.FieldOfView;
        }
        lastViewAngle = ViewAngle;

        cameraFovCurrent = float.Lerp(cameraFovCurrent, cameraFovTarget, (float)delta * 10f);
        Camera.Fov = cameraFovCurrent;

        Camera.Rotation = cameraRotationKick;
        ViewAimViewmodel.Position = viewmodelPositionKickCurrent + viewmodelAimSway;
        ViewAimViewmodel.Rotation = viewmodelRotationKick;
        if (cameraShake > 0.001f) Camera.Position = new Vector3(0, Random.Shared.NextSingle(), Random.Shared.NextSingle()) * cameraShake;
        else Camera.Position = Vector3.Zero;

        if (IsInputBlocked()) return;

        // X = horizontal, Y = vertical
        ViewAngle = new Vector2(ViewAngle.X - mouseDelta.X, float.Clamp(ViewAngle.Y - mouseDelta.Y, -1.4f, 1.4f));
        mouseDelta = Vector2.Zero;

        Rotation = new Vector3(0, ViewAngle.X, 0);
        viewModels.Rotation = new Vector3(ViewAngle.Y, 0, 0);

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

        if (IsInputBlocked()) return;

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

        PhysicsProcessMovement();
    }

    public override void OnDeath(DamageInfo damageInfo)
    {
        base.OnDeath(damageInfo);

        if (Self == this)
        {
            var control = GD.Load<PackedScene>("res://scenes/pawn/outercontroller/OuterControllerDead.tscn");
            var inst = control.Instantiate();
            Global.ClearOnLoad.AddChild(inst);
            outerController = (OuterController)inst;
            ViewPlayer(this);
        }

        var ragdoll = GD.Load<PackedScene>("res://scenes/pawn/mob/LiveMobRagdoll.tscn").Instantiate<Node3D>();
        var liveSk = worldModels.GetNode<Skeleton3D>("KincheePlayer/Player/Skeleton3D");
        var ragSk = ragdoll.GetNode<Skeleton3D>("KincheePlayerMob/Armature/Skeleton3D");
        var copyCount = Math.Min(liveSk.GetBoneCount(), ragSk.GetBoneCount());

        ragdoll.GlobalTransform = GlobalTransform;
        for (int i = 0; i < copyCount; i++)
        {
            var pos = liveSk.GetBonePosePosition(i);
            var rot = liveSk.GetBonePoseRotation(i);
            ragSk.SetBonePosePosition(i, pos);
            ragSk.SetBonePoseRotation(i, rot);
        }

        var hitCollider = damageInfo.HitboxName;
        // ragdoll has different colliders
        if (hitCollider == "Head") hitCollider = "Neck";
        if (hitCollider == "Foot_R") hitCollider = "LowerLeg_R";
        if (hitCollider == "Foot_L") hitCollider = "LowerLeg_L";
        ((Ragdoll)ragdoll).SetHit(hitCollider, damageInfo.HitDirection, damageInfo.Force);

        Global.ClearOnLoad.AddChild(ragdoll);
    }

    public void AddCameraShake(float amount)
    {
        cameraShake += amount;
    }

    public void AddCameraRotationKick(Vector3 rotationAmount)
    {
        cameraRotationKick += rotationAmount;
    }

    public void AddViewmodelPositionKick(Vector3 amount, float lerpScale = 1f)
    {
        viewmodelPositionKickTarget += amount;
        viewmodelPositionKicklerpScale = lerpScale;
    }

    public void AddViewmodelRotationKick(Vector3 rotationAmount)
    {
        viewmodelRotationKick += rotationAmount;
    }

    public void OpenUI(string uiScene)
    {
        if (IsInstanceValid(openUI)) return;

        var ui = GD.Load<PackedScene>(uiScene).Instantiate<Control>();
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

    private bool IsInputBlocked()
    {
        if (IsInstanceValid(openUI)) return true;
        if (IsDead) return true;
        if (IsInstanceValid(outerController)) return true;

        return false;
    }

    /// <summary>Local only. Should not be run on non-owned players</summary>
    public void ViewPlayer(Player player)
    {
        if (this != Self) return;

        if (!IsDead)
        {
            if (IsInstanceValid(Viewing) && Viewing != this)
            {
                Viewing.worldModels.Visible = true;
                Viewing.viewModels.Visible = false;
                _ = Viewing.SelectedTool?.UnequipViewing();
            }

            Viewing = this;
            Viewing.worldModels.Visible = false;
            Viewing.viewModels.Visible = true;
            _ = Viewing.SelectedTool?.EquipViewing();
            Viewing.Camera.MakeCurrent();
            return;
        }

        if (IsInstanceValid(Viewing))
        {
            Viewing.worldModels.Visible = true;
            Viewing.viewModels.Visible = false;
            _ = Viewing.SelectedTool?.UnequipViewing();
        }

        Viewing = player;
        Camera.ClearCurrent(false);
        if (outerController is OuterControllerDead ocd) ocd.ViewPlayer(player);
        if (!Viewing.IsDead)
        {
            Viewing.worldModels.Visible = false;
            Viewing.viewModels.Visible = true;
            _ = Viewing.SelectedTool?.EquipViewing();
        }
        else
        {
            Viewing.worldModels.Visible = false;
            Viewing.viewModels.Visible = false;
        }
    }
}
