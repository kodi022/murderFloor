namespace MurderFloor;

using Loot;

// ! rework to support other types
public partial class LockerListMenu : Control
{
    public bool LMShowModifyButton { get; set; }
    public bool LMShowAdvancedButton { get; set; }
    public bool LMShowWeightLabel { get; set; }

    [Export]
    private GridContainer grid;
    [Export]
    private TextureRect rect;
    [Export]
    private Control rectDragControl;
    [Export]
    private Button returnButton;
    [Export]
    private Button equipToolButton;
    [Export]
    private Button modifyToolButton;
    [Export]
    private Button advancedToolButton;
    [Export]
    private RichTextLabel totalWeightLabel;
    [Export]
    private Panel toolStatsPanel;

    private bool previewSceneCreated;
    private Camera3D cam;

    private bool listShowAttachments = false;

    private Tool selectedTool;
    private LockerToolButton selectedToolLockerToolButton;
    private LootState selectedToolLootState;

    private bool isSelectedToolEquipped;

    private Attachment selectedAttachment;
    private LockerToolButton selectedAttachmentLockerToolButton;
    private LootState selectedAttachmentLootState;

    private SubViewport sceneViewport;
    private Node3D weaponSceneParent;
    private Node3D weaponScene;

    private bool draggingRect;
    private Vector2 mouseScreenRelative;

    public override void _Ready()
    {
        BuildList();

        returnButton.Pressed += QueueFree;
        equipToolButton.Pressed += PressedEquipButton;
        modifyToolButton.Pressed += PressedModifyButton;
    }

    public override void _Process(double delta)
    {
        if (weaponSceneParent is null) return;

        equipToolButton.Text = isSelectedToolEquipped ? "Unequip" : "Equip";
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

    private void PressedEquipButton()
    {
        var lootStateHash = selectedToolLootState.GetHashCode();
        if (Player.Self.HasTool(selectedToolLootState))
        {
            SaveManager.CurrentSave.Equipped.Remove(lootStateHash);
            Player.Self.Rpc("ToolRemoveRpc", selectedToolLootState.Serialize());
        }
        else
        {
            if (!SaveManager.CurrentSave.Equipped.Contains(lootStateHash))
                SaveManager.CurrentSave.Equipped.Add(lootStateHash);

            var attachments = SaveManager.CurrentSave.GetAttachmentsOnTool(selectedToolLootState);
            var toolConfig = new ToolConfig(selectedToolLootState, attachments);
            Player.Self.Rpc("ToolAddRpc", toolConfig.Serialize());
        }

        isSelectedToolEquipped = Player.Self.HasTool(selectedToolLootState);
        selectedToolLockerToolButton.CheckState(selectedToolLootState);
        SaveManager.Save(SaveManager.CurrentSave);
        SelectTool();
    }

    private void PressedModifyButton()
    {
        listShowAttachments = !listShowAttachments;
        BuildList();
    }

    private async void BuildList()
    {
        foreach (var child in grid.GetChildren()) child.Free();

        var lockerToolButton = GD.Load<PackedScene>("res://scenes/ui/locker/LockerToolButton.tscn");
        foreach (var lootState in SaveManager.CurrentSave.GetAllLoot())
        {
            var lootResource = lootState.GetLootRef();
            if (lootResource.FullId == "base:fists") continue;

            if (listShowAttachments && lootResource is Tool) continue;
            if (!listShowAttachments && lootResource is Attachment) continue;

            var newButton = lockerToolButton.Instantiate<LockerToolButton>();
            newButton.LootState = lootState;
            newButton.Button.Pressed += () =>
            {
                if (lootResource is Tool tool)
                    PressedToolButton(tool, lootState, newButton);

                if (lootResource is Attachment att)
                    PressedAttachmentButton(att, lootState, newButton);
            };
            grid.AddChild(newButton);

            if (lootResource is Tool tool)
            {
                if (selectedTool is null)
                {
                    PressedToolButton(tool, lootState, newButton);
                }
                // else if (!IsInstanceValid(selectedToolLockerToolButton) && selectedToolLootState == lootState)
                // {
                //     selectedToolLockerToolButton = newButton;
                // }

                newButton.CheckState(selectedToolLootState);
            }

            if (lootResource is Attachment)
            {
                newButton.CheckState(selectedToolLootState);
            }
        }
    }

    private void PressedToolButton(Tool tool, LootState lootState, LockerToolButton button)
    {
        selectedTool = tool;
        selectedToolLootState = lootState;

        if (IsInstanceValid(selectedToolLockerToolButton))
            selectedToolLockerToolButton.CheckState(selectedToolLootState);

        selectedToolLockerToolButton = button;
        SelectTool();
    }

    private void PressedAttachmentButton(Attachment att, LootState lootState, LockerToolButton button)
    {
        selectedAttachment = att;
        selectedAttachmentLootState = lootState;
        selectedAttachmentLockerToolButton = button;
        SelectAttachment();
    }

    private void SelectTool()
    {
        isSelectedToolEquipped = Player.Self.HasTool(selectedToolLootState);
        selectedToolLockerToolButton.CheckState(selectedToolLootState);

        var strIcon = "[img=32]res://images/ui/TablerWeight.png[/img]";
        if (Player.Self.HasTool(selectedToolLootState))
        {
            totalWeightLabel.Text = strIcon + $"{Player.Self.ToolWeight} / {Player.Self.MaxWeight} (-{selectedTool.CarryWeight})";
            equipToolButton.Disabled = false;
        }
        else
        {
            totalWeightLabel.Text = strIcon + $"{Player.Self.ToolWeight} / {Player.Self.MaxWeight} (+{selectedTool.CarryWeight})";
            equipToolButton.Disabled = Player.Self.ToolWeight + selectedTool.CarryWeight > Player.Self.MaxWeight;
        }

        var lootRef = selectedToolLootState.GetLootRef();
        var lootRarity = new LootRarity(selectedToolLootState);
        toolStatsPanel.GetChild<Label>(1).Text = lootRef.FullId;
        toolStatsPanel.GetChild<RichTextLabel>(2).Text = $"[color={Wears.WearList[lootRarity.Wear]}]{lootRarity.Wear}";
        toolStatsPanel.GetChild<RichTextLabel>(3).Text = $"[color={Tiers.TierList[lootRarity.Tier].Color.ToHtml()}]{lootRarity.Tier}";

        BuildToolViewport();
    }

    private void SelectAttachment()
    {
        if (selectedAttachmentLootState.HasCustomData("g"))
        {
            selectedAttachmentLootState.RemoveCustomData("g");
            SaveManager.CurrentSave.ReplaceLoot(selectedAttachmentLootState);
            selectedAttachmentLockerToolButton.CheckState(selectedToolLootState);
        }
        else
        {
            selectedAttachmentLootState.AddCustomData('g', Compression.IntToAB64(selectedToolLootState.GetHashCode()));
            SaveManager.CurrentSave.ReplaceLoot(selectedAttachmentLootState);
            selectedAttachmentLockerToolButton.CheckState(selectedToolLootState);
        }

        SaveManager.Save(SaveManager.CurrentSave);
        BuildToolViewport();
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
            var atts = SaveManager.CurrentSave.GetAttachmentsOnTool(selectedToolLootState);
            var t = new ToolConfig(selectedToolLootState, atts);
            weaponScene = selectedTool.BuildToolScene(t).Tool;
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
        }
        else
        {
            weaponScene?.Free();
            var atts = SaveManager.CurrentSave.GetAttachmentsOnTool(selectedToolLootState);
            var t = new ToolConfig(selectedToolLootState, atts);
            weaponScene = selectedTool.BuildToolScene(t).Tool;
            weaponSceneParent.AddChild(weaponScene);

            var bounds = MFResource.GetBounds(weaponScene);
            var modelCenter = (bounds.End + bounds.Position) / 2;
            weaponScene.GlobalPosition = -modelCenter;
        }
    }
}