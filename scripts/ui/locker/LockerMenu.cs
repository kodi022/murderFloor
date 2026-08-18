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

    private LockerToolButton selectedLockerToolButton;
    private LootState selectedLootState;
    private Tool selectedTool;
    private bool playerEquippedTool;
    private SubViewport sceneViewport;
    private Node3D weaponSceneParent;
    private Node3D weaponScene;

    private bool draggingRect;
    private Vector2 mouseScreenRelative;

    public override void _Ready()
    {
        BuildList();
        equipToolButton.Pressed += ToolAddOrRemove;
        equipToolButton.Pressed += SelectTool;
        modifyToolButton.Pressed += ModifyButton;
    }

    public override void _Process(double delta)
    {
        if (weaponSceneParent is null) return;

        playerEquippedTool = Player.Self.HasTool(selectedLootState);
        equipToolButton.Text = playerEquippedTool ? "Unequip" : "Equip";

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
        var lockerToolButton = GD.Load<PackedScene>("res://scenes/ui/locker/LockerToolButton.tscn");
        foreach (var loot in SaveManager.CurrentSave.Loot)
        {
            if (string.IsNullOrEmpty(loot)) continue;

            var lootState = LootState.Deserialize(loot);
            var lootRef = LootState.GetLootRef(lootState);
            if (lootRef.FullId == "base:fists") continue;

            var lootResource = ResourceManager.LootRegistry.GetResourceRef(lootState.HashId);
            if (lootResource is Tool tool)
            {
                var newButton = lockerToolButton.Instantiate<LockerToolButton>();
                newButton.LootStateInfo = lootState;
                newButton.Button.Pressed += () =>
                {
                    selectedLootState = lootState;
                    selectedTool = tool;
                    selectedLockerToolButton.CheckState(selectedLootState);
                    selectedLockerToolButton = newButton;
                    selectedLockerToolButton.CheckState(selectedLootState);
                    SelectTool();
                    BuildToolViewport();
                };
                grid.AddChild(newButton);

                if (selectedTool is null)
                {
                    selectedLockerToolButton = newButton;
                    selectedLootState = lootState;
                    selectedTool = tool;
                    BuildToolViewport();
                }

                newButton.CheckState(selectedLootState);
            }
        }
    }

    private void SelectTool()
    {
        var strIcon = "[img]res://images/ui/icon-weight.png[/img]";
        if (Player.Self.HasTool(selectedLootState))
        {
            totalWeightLabel.Text = strIcon + $"{Player.Self.MaxWeight} / {Player.Self.ToolWeight} (-{selectedTool.CarryWeight})";
            equipToolButton.Disabled = false;
        }
        else
        {
            totalWeightLabel.Text = strIcon + $"{Player.Self.MaxWeight} / {Player.Self.ToolWeight} (+{selectedTool.CarryWeight})";
            equipToolButton.Disabled = Player.Self.ToolWeight + selectedTool.CarryWeight > Player.Self.MaxWeight;
        }
    }

    private void BuildToolViewport()
    {
        if (!previewSceneCreated)
        {
            previewSceneCreated = true;

            var windowSize = GetWindow().Size;
            sceneViewport = new SubViewport
            {
                Size = new Vector2I((int)(windowSize.Y * 1.7777778f), (int)windowSize.Y),
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
                OwnWorld3D = true,
                TransparentBg = true,
            };
            AddChild(sceneViewport);

            weaponSceneParent = new Node3D();
            weaponScene = selectedTool.MeshScene.Instantiate<Node3D>();
            weaponScene.RotationDegrees = new Vector3(0, selectedTool.MeshSceneImportYaw, 0);
            weaponSceneParent.AddChild(weaponScene);

            var dirLight = new DirectionalLight3D() { RotationDegrees = new Vector3(-55, 35, 0) };
            var camera = new Camera3D() { Fov = 35f };
            camera.LookAtFromPosition(new Vector3(-0.2f, 0, 2f), new Vector3(-0.2f, 0, 0));

            sceneViewport.AddChild(weaponSceneParent);
            sceneViewport.AddChild(dirLight);
            sceneViewport.AddChild(camera);
            rect.Texture = sceneViewport.GetTexture();
        }
        else
        {
            weaponScene?.Free();
            weaponScene = selectedTool.MeshScene.Instantiate<Node3D>();
            weaponScene.RotationDegrees = new Vector3(0, selectedTool.MeshSceneImportYaw, 0);
            weaponSceneParent.AddChild(weaponScene);
            sceneViewport.AddChild(weaponSceneParent);
        }
    }

    private void ToolAddOrRemove()
    {
        if (Player.Self.HasTool(selectedLootState))
            Player.Self.Rpc("ToolRemoveRpc", LootState.Serialize(selectedLootState));
        else
            Player.Self.Rpc("ToolAddRpc", LootState.Serialize(selectedLootState));

        selectedLockerToolButton.CheckState(selectedLootState);
    }

    private void ModifyButton()
    {

    }
}