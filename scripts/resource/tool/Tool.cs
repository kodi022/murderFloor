namespace MurderFloor;

[GlobalClass]
public partial class Tool : MFResource
{
    [Export]
    public PackedScene MeshSceneViewmodel { get; private set; }
    [Export]
    public string HoldTypeAnimation { get; private set; } = "holdtype_pistol";

    public enum SlotEnum
    {
        Primary,
        Secondary,
        Special,
        Melee
    }

    public struct FireInfo
    {
        public Player Player { get; set; }
        public LiveTool LiveTool { get; set; }
        public Vector3 StartPosition { get; set; }
        public Transform3D ViewTransform { get; set; }
        public readonly Vector3 ViewForward => -ViewTransform.Basis.Z;
    }

    public virtual SlotEnum GetSlot() => SlotEnum.Special;

    public override async Task<ImageTexture> GenerateThumbnailImage(int resX, int resY)
    {
        if (MeshScene is null) return Global.MissingTextureImage;

        string GetDictKey(int resX, int resY) { return $"{HashId}-{resX}-{resY}"; }
        if (generatedThumbnails.TryGetValue(GetDictKey(resX, resY), out ImageTexture val))
            return val;

        var sceneViewport = new SubViewport
        {
            Size = new Vector2I(resX, resY),
            OwnWorld3D = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Once,
            Msaa3D = Viewport.Msaa.Msaa8X,
            TransparentBg = true,
        };
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(sceneViewport);

        var weaponScene = MeshScene.Instantiate<Node3D>();
        var camera = new Camera3D();
        var bounds = GetBounds(weaponScene);
        var modelCenter = (bounds.End + bounds.Position) / 2;
        var modelWidth = bounds.End.Abs().X + bounds.Position.Abs().X;
        weaponScene.Position = -modelCenter;
        weaponScene.RotationDegrees = new Vector3(0, MeshSceneImportYaw, 0);
        camera.SetOrthogonal(MathF.Max(modelWidth, 0.5f), 0.1f, 20f);
        camera.LookAtFromPosition(new Vector3(0, 0, 3f), Vector3.Zero);

        sceneViewport.AddChild(weaponScene);
        sceneViewport.AddChild(camera);
        ApplyThumbnailMaterialToParts(weaponScene);

        // var outline = new Outlines.OutlinerComponent { Target = weaponScene };
        // var outlineDisplay = new Outlines.OutlinesDisplayComponent { Camera = camera };
        // sceneViewport.AddChild(outline);
        // sceneViewport.AddChild(outlineDisplay);

        await sceneViewport.ToSignal(RenderingServer.Singleton, RenderingServerInstance.SignalName.FramePostDraw);
        var image = sceneViewport.GetViewport().GetTexture().GetImage();
        sceneViewport.QueueFree();

        var imgTex = ImageTexture.CreateFromImage(image);
        if (!generatedThumbnails.ContainsKey(GetDictKey(resX, resY)))
            generatedThumbnails.Add(GetDictKey(resX, resY), imgTex);
        return imgTex;
    }

    private static void ApplyThumbnailMaterialToParts(Node3D weaponScene)
    {
        foreach (var child in weaponScene.GetChildren())
        {
            if (child is MeshInstance3D mesh)
            {
                mesh.MaterialOverride = GD.Load<Material>("res://materials/thumbnail.tres");
            }
        }
    }

    private static Aabb GetBounds(Node3D node)
    {
        var bounds = new Aabb();
        if (node.IsQueuedForDeletion()) return bounds;

        if (node is VisualInstance3D inst)
        {
            bounds = inst.GetAabb();
        }

        foreach (var child in node.GetChildren())
        {
            if (child is not VisualInstance3D childInst) continue;
            if (childInst.GetAabb() == default) continue;

            var childBounds = childInst.GetAabb();
            bounds = bounds.Merge(childBounds);
        }

        //bounds = node.Transform * bounds;

        return bounds;
    }
}