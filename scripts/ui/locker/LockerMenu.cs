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
    private Node3D weaponScene;

    private Vector2 mousePosition;
    private Vector2 mouseScreenRelative;

    public override void _Ready()
    {
        BuildList();
        equipToolButton.Pressed += ToolAddOrRemove;
        modifyToolButton.Pressed += ModifyButton;
    }

    public override void _Process(double delta)
    {
        if (weaponScene is null) return;

        playerEquippedTool = Player.Self.HasTool(selectedLootState);
        equipToolButton.Text = playerEquippedTool ? "Unequip" : "Equip";

        if (rectDragControl.GetGlobalRect().HasPoint(mousePosition))
        {
            weaponScene.Rotate(Vector3.Up, mouseScreenRelative.X * 0.006f);
            weaponScene.Rotate(Vector3.Right, mouseScreenRelative.Y * 0.006f);
            mouseScreenRelative = Vector2.Zero;
        }
        else
        {
            var target = new Vector3(0, selectedTool.MeshSceneImportYaw, 0);
            weaponScene.RotationDegrees = weaponScene.RotationDegrees.Lerp(target, 3f * (float)delta);
        }

        if (selectedTool is not null)
        {
            if (Player.Self.HasTool(selectedLootState))
                totalWeightLabel.Text = $"[img]res://images/ui/icon-weight.png[/img]{Player.Self.MaxWeight} / {Player.Self.ToolWeight} (-{selectedTool.CarryWeight})";
            else
                totalWeightLabel.Text = $"[img]res://images/ui/icon-weight.png[/img]{Player.Self.MaxWeight} / {Player.Self.ToolWeight} (+{selectedTool.CarryWeight})";
        }

        base._Process(delta);
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (@event is InputEventMouseMotion eventMouse)
        {
            mousePosition = eventMouse.Position;
            mouseScreenRelative = eventMouse.ScreenRelative;
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

    private void BuildToolViewport()
    {
        if (!previewSceneCreated)
        {
            previewSceneCreated = true;

            sceneViewport = new SubViewport
            {
                Size = (Vector2I)GetViewportRect().Size,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
                OwnWorld3D = true,
                TransparentBg = true,
            };
            AddChild(sceneViewport);

            weaponScene = selectedTool.MeshScene.Instantiate<Node3D>();
            weaponScene.RotationDegrees = new Vector3(0, selectedTool.MeshSceneImportYaw, 0);
            var dirLight = new DirectionalLight3D();
            dirLight.RotationDegrees = new Vector3(-55, 35, 0);
            var camera = new Camera3D();
            camera.Fov = 30f;
            camera.LookAtFromPosition(new Vector3(-0.2f, 0, 2.2f), new Vector3(-0.2f, 0, 0));

            sceneViewport.AddChild(weaponScene);
            sceneViewport.AddChild(dirLight);
            sceneViewport.AddChild(camera);
            rect.Texture = sceneViewport.GetTexture();
        }
        else
        {
            weaponScene?.Free();
            weaponScene = null;
            weaponScene = selectedTool.MeshScene.Instantiate<Node3D>();
            weaponScene.RotationDegrees = new Vector3(0, selectedTool.MeshSceneImportYaw, 0);
            sceneViewport.AddChild(weaponScene);
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