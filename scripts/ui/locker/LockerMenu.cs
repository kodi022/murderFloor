namespace MurderFloor;

using Loot;

public partial class LockerMenu : ScreenScaleLimiter
{
    [Export]
    private GridContainer grid;
    [Export]
    private TextureRect rect;
    [Export]
    private Control rectDragControl;
    [Export]
    private Button equipToolButton;
    [Export]
    private Button modifyToolButton;
    [Export]
    private Button advancedToolButton;
    [Export]
    private RichTextLabel totalWeightLabel;

    private bool previewSceneCreated;
    private Camera3D cam;

    private bool listShowAttachments = false;

    private Tool selectedTool;
    private LockerToolButton selectedToolLockerToolButton;
    private LootState selectedToolLootState;

    private bool playerEquippedSelectedTool;

    private Attachment selectedAttachment;
    private LockerToolButton selectedAttachmentLockerToolButton;
    private LootState selectedAttachmentLootState;
    private int selectedAttachmentSaveIndex;

    private SubViewport sceneViewport;
    private Node3D weaponSceneParent;
    private Node3D weaponScene;

    private bool draggingRect;
    private Vector2 mouseScreenRelative;


    public override void _Ready()
    {
        BuildList();
        equipToolButton.Pressed += EquipButton;
        equipToolButton.Pressed += SelectTool;
        modifyToolButton.Pressed += ModifyButton;
    }

    public override void _Process(double delta)
    {
        if (weaponSceneParent is null) return;

        equipToolButton.Text = playerEquippedSelectedTool ? "Unequip" : "Equip";
        modifyToolButton.Text = listShowAttachments ? "Stop Modify" : "Modify";

        if (draggingRect)
        {
            weaponSceneParent.Rotate(Vector3.Up, mouseScreenRelative.X * 0.006f);
            weaponSceneParent.Rotate(Vector3.Right, mouseScreenRelative.Y * 0.006f);
            mouseScreenRelative = Vector2.Zero;
        }
        else
        {
            weaponSceneParent.Rotation = weaponSceneParent.Rotation.Lerp(Vector3.Zero, 3f * (float)delta);
        }

        base._Process(delta);
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (@event is InputEventMouseButton eventMouseButton)
        {
            if (eventMouseButton.ButtonIndex == MouseButton.Left)
            {
                if (eventMouseButton.Pressed && rectDragControl.GetGlobalRect().HasPoint(eventMouseButton.Position))
                    draggingRect = true;
                else
                    draggingRect = false;
            }
        }

        if (@event is InputEventMouseMotion eventMouseMotion)
        {
            mouseScreenRelative = eventMouseMotion.ScreenRelative;
        }
    }

    private async void BuildList()
    {
        foreach (var child in grid.GetChildren()) child.Free();

        var lockerToolButton = GD.Load<PackedScene>("res://scenes/ui/locker/LockerToolButton.tscn");
        foreach (var loot in SaveManager.CurrentSave.Loot)
        {
            if (string.IsNullOrEmpty(loot)) continue;

            var lootState = LootState.Deserialize(loot);
            var lootRef = LootState.GetLootRef(lootState);
            if (lootRef.FullId == "base:fists") continue;

            var lootResource = ResourceManager.LootRegistry.GetResourceRef(lootState.ResourceHashId);

            if (listShowAttachments && lootResource is Tool) continue;
            if (!listShowAttachments && lootResource is Attachment) continue;

            var newButton = lockerToolButton.Instantiate<LockerToolButton>();
            newButton.LootState = lootState;
            newButton.Button.Pressed += () =>
            {
                if (lootResource is Tool tool)
                {
                    selectedTool = tool;
                    selectedToolLootState = lootState;
                    if (IsInstanceValid(selectedToolLockerToolButton))
                        selectedToolLockerToolButton.CheckState(selectedToolLootState);
                    selectedToolLockerToolButton = newButton;
                    SelectTool();
                }

                if (lootResource is Attachment att)
                {
                    selectedAttachment = att;
                    selectedAttachmentLootState = lootState;
                    selectedAttachmentLockerToolButton = newButton;
                    selectedAttachmentSaveIndex = SaveManager.CurrentSave.Loot.IndexOf(loot);
                    SelectAttachment();
                }
            };
            grid.AddChild(newButton);

            if (lootResource is Tool tool)
            {
                if (selectedTool is null)
                {
                    selectedTool = tool;
                    selectedToolLootState = lootState;
                    if (IsInstanceValid(selectedToolLockerToolButton))
                        selectedToolLockerToolButton.CheckState(selectedToolLootState);
                    selectedToolLockerToolButton = newButton;
                    SelectTool();
                }
                else if (!IsInstanceValid(selectedToolLockerToolButton) && selectedToolLootState == lootState)
                {
                    selectedToolLockerToolButton = newButton;
                }

                newButton.CheckState(selectedToolLootState);
            }

            if (lootResource is Attachment)
            {
                newButton.CheckState(new LootState());
            }
        }
    }

    private void SelectTool()
    {
        playerEquippedSelectedTool = Player.Self.HasTool(selectedToolLootState);
        selectedToolLockerToolButton.CheckState(selectedToolLootState);

        var strIcon = "[img]res://images/ui/icon-weight.png[/img]";
        if (Player.Self.HasTool(selectedToolLootState))
        {
            totalWeightLabel.Text = strIcon + $"{Player.Self.MaxWeight} / {Player.Self.ToolWeight} (-{selectedTool.CarryWeight})";
            equipToolButton.Disabled = false;
        }
        else
        {
            totalWeightLabel.Text = strIcon + $"{Player.Self.MaxWeight} / {Player.Self.ToolWeight} (+{selectedTool.CarryWeight})";
            equipToolButton.Disabled = Player.Self.ToolWeight + selectedTool.CarryWeight > Player.Self.MaxWeight;
        }

        BuildToolViewport();
    }

    private void SelectAttachment()
    {
        if (selectedAttachmentLootState.HasCustomData("g")) return;
        selectedAttachmentLootState.AddCustomData('g', Compression.IntToAB64(selectedToolLootState.GetHashCode()));
        SaveManager.CurrentSave.Loot[selectedAttachmentSaveIndex] = LootState.Serialize(selectedAttachmentLootState);
        selectedAttachmentLockerToolButton.CheckState(selectedAttachmentLootState);
        SaveManager.Save(SaveManager.CurrentSave);
    }

    private void BuildToolViewport()
    {
        if (!previewSceneCreated)
        {
            previewSceneCreated = true;

            var windowSize = GetWindow().Size;
            sceneViewport = new SubViewport
            {
                Size = new Vector2I((int)(windowSize.Y * 1.7777778f), windowSize.Y),
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
                OwnWorld3D = true,
                TransparentBg = true,
            };
            AddChild(sceneViewport);

            weaponSceneParent = new Node3D();
            weaponScene = selectedTool.MeshScene.Instantiate<Node3D>();
            weaponSceneParent.AddChild(weaponScene);

            var dirLight = new DirectionalLight3D() { RotationDegrees = new Vector3(-55, 35, 0) };
            var camera = new Camera3D() { Fov = 35f };
            camera.LookAtFromPosition(new Vector3(-0.2f, 0, 2f), new Vector3(-0.2f, 0, 0));

            sceneViewport.AddChild(weaponSceneParent);
            sceneViewport.AddChild(dirLight);
            sceneViewport.AddChild(camera);
            rect.Texture = sceneViewport.GetTexture();

            var bounds = MFResource.GetBounds(weaponScene);
            var modelCenter = (bounds.End + bounds.Position) / 2;
            weaponScene.GlobalPosition = -modelCenter;
            weaponScene.RotationDegrees = new Vector3(0, selectedTool.MeshSceneImportYaw, 0);
        }
        else
        {
            weaponScene?.Free();
            weaponScene = selectedTool.MeshScene.Instantiate<Node3D>();
            weaponSceneParent.AddChild(weaponScene);

            var bounds = MFResource.GetBounds(weaponScene);
            var modelCenter = (bounds.End + bounds.Position) / 2;
            weaponScene.GlobalPosition = -modelCenter;
            weaponScene.RotationDegrees = new Vector3(0, selectedTool.MeshSceneImportYaw, 0);
        }
    }

    private void EquipButton()
    {
        var lootStateHash = selectedToolLootState.GetHashCode();
        if (Player.Self.HasTool(selectedToolLootState))
        {
            SaveManager.CurrentSave.Equipped.Remove(lootStateHash);
            Player.Self.Rpc("ToolRemoveRpc", LootState.Serialize(selectedToolLootState));
        }
        else
        {
            if (!SaveManager.CurrentSave.Equipped.Contains(lootStateHash))
                SaveManager.CurrentSave.Equipped.Add(lootStateHash);
            Player.Self.Rpc("ToolAddRpc", LootState.Serialize(selectedToolLootState));
        }

        playerEquippedSelectedTool = Player.Self.HasTool(selectedToolLootState);
        selectedToolLockerToolButton.CheckState(selectedToolLootState);
        SaveManager.Save(SaveManager.CurrentSave);
    }

    private void ModifyButton()
    {
        listShowAttachments = !listShowAttachments;
        BuildList();
    }
}