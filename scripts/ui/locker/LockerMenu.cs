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

    private bool previewSceneCreated;
    private Camera3D cam;

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
                    SelectTool();
                };

                grid.AddChild(newButton);

                if (selectedTool is null)
                {
                    selectedTool = tool;
                    SelectTool();
                }
            }
        }
    }

    private void SelectTool()
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
            Player.Self.Rpc("ToolRemoveRpc", selectedTool.FullId);
        else
            Player.Self.Rpc("ToolAddRpc", selectedTool.FullId);
    }

    private void ModifyButton()
    {

    }
}