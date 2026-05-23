namespace MurderFloor;

public partial class LockerMenu : Control
{
    [Export]
    private GridContainer grid;
    [Export]
    private TextureRect rect;

    private bool previewSceneCreated = false;
    private Camera3D cam;

    private Tool selectedTool;
    private SubViewport sceneViewport;
    private Node3D weaponScene;

    public override void _Ready()
    {
        BuildList();
    }

    private async void BuildList()
    {
        foreach (var tool in ResourceManager.ToolRegistry.GetAllResource())
        {
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
            rect.Texture = (Texture2D)await tool.Value.GenerateThumbnailTexture();

            selectedTool ??= tool.Value;
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
            };
            AddChild(sceneViewport);

            weaponScene = (Node3D)selectedTool.MeshScene.Instantiate();
            var dirLight = new DirectionalLight3D();
            var camera = new Camera3D();
            camera.SetOrthogonal(1f, 0.1f, 100f);
            camera.LookAtFromPosition(new Vector3(0, 0, 5f), Vector3.Zero);

            sceneViewport.AddChild(weaponScene);
            sceneViewport.AddChild(dirLight);
            sceneViewport.AddChild(camera);
            rect.Texture = sceneViewport.GetTexture();
        }
        else
        {
            weaponScene?.Free();
            weaponScene = null;
            weaponScene = (Node3D)selectedTool.MeshScene.Instantiate();
            sceneViewport.AddChild(weaponScene);
        }
    }
}