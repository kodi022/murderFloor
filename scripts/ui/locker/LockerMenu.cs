namespace MurderFloor;

public partial class LockerMenu : ScreenScaleLimiter
{
    [Export]
    private GridContainer grid;
    [Export]
    private TextureRect rect;
    [Export]
    private Button addToolButton;
    [Export]
    private Button removeToolButton;

    private bool previewSceneCreated = false;
    private Camera3D cam;

    private Tool selectedTool;
    private SubViewport sceneViewport;
    private Node3D weaponScene;

    public override void _Ready()
    {
        BuildList();
        addToolButton.Pressed += ToolAddToInventory;
        removeToolButton.Pressed += ToolRemoveFromInventory;
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (@event is InputEventMouseMotion eventMouse)
        {
            if (weaponScene is null) return;
            weaponScene.Rotate(Vector3.Up, eventMouse.ScreenRelative.X * 0.005f);
            weaponScene.Rotate(Vector3.Right, eventMouse.ScreenRelative.Y * 0.005f);
        }
    }

    private async void BuildList()
    {
        var lockerToolButton = GD.Load<PackedScene>("res://scenes/ui/locker/LockerToolButton.tscn");
        foreach (var loot in SaveManager.CurrentSave.Loot)
        {
            var lootState = Loot.LootStateInfo.Deserialize(loot);
            var lootResource = ResourceManager.LootRegistry.First(c => lootState.LootHashId == c.HashId);
            if (lootResource is Tool tool)
            {
                var newButton = lockerToolButton.Instantiate<Control>();

                // var panelContainer = new PanelContainer();
                // var button = new Button();
                // panelContainer.AddChild(button);
                // var rect = new TextureRect();
                // panelContainer.AddChild(rect);

                var lootRarity = new Loot.LootRarityInfo(lootState);

                //var label = new Label() { Text = lootState.Level.ToString() };
                //panelContainer.AddChild(label);
                // ! continue on this
                // button.ButtonDown += () =>
                // {
                //     selectedTool = tool;
                //     SelectTool();
                // };

                // rect.Texture = await lootResource.GenerateThumbnailImage(128, 80);
                // grid.AddChild(panelContainer);

                if (selectedTool is null)
                {
                    selectedTool = tool;
                    SelectTool();
                }
            }
        }

        foreach (var tool in ResourceManager.ToolRegistry.GetAllResource())
        {
            if (tool.Value.FullId == "base:fists") continue;

            var panelContainer = new PanelContainer();
            var button = new Button();
            var rect = new TextureRect();

            button.ButtonDown += () =>
            {
                selectedTool = tool.Value;
                SelectTool();
            };

            grid.AddChild(panelContainer);
            panelContainer.AddChild(rect);
            panelContainer.AddChild(button);
            rect.Texture = await tool.Value.GenerateThumbnailImage(128, 96);

            if (selectedTool is null)
            {
                selectedTool = tool.Value;
                SelectTool();
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
                Size = new Vector2I((int)rect.Size.X, (int)rect.Size.Y),
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
                OwnWorld3D = true,
                TransparentBg = true,
            };
            AddChild(sceneViewport);

            weaponScene = selectedTool.MeshScene.Instantiate<Node3D>();
            weaponScene.RotationDegrees = new Vector3(0, selectedTool.MeshSceneImportYaw, 0);
            var dirLight = new DirectionalLight3D();
            var camera = new Camera3D();
            camera.Fov = 25f;
            camera.LookAtFromPosition(new Vector3(-0.2f, 0, 2.6f), new Vector3(-0.2f, 0, 0));

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

    private void ToolAddToInventory()
    {
        Player.Self.Rpc("ToolAddRpc", selectedTool.FullId);
    }

    private void ToolRemoveFromInventory()
    {
        Player.Self.Rpc("ToolRemoveRpc", selectedTool.FullId);
    }
}