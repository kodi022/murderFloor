namespace MurderFloor;

[GlobalClass]
public partial class Attachment : MFResource
{
    public enum AttachmentTypeEnum
    {
        Optic,
        Muzzle,
        Barrel,
        Gadget,
    }

    [Export]
    public AttachmentTypeEnum AttachmentType { get; private set; }

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
        var modelWidth = bounds.End.Abs().X + bounds.Position.Abs().X;
        weaponScene.RotationDegrees = new Vector3(0, MeshSceneImportYaw, 0);
        camera.SetOrthogonal(modelWidth * 0.65f, 0.1f, 20f);
        camera.LookAtFromPosition(new Vector3(3f, 0, 3f), Vector3.Zero);

        sceneViewport.AddChild(weaponScene);
        var modelCenter = (bounds.End + bounds.Position) / 2;
        weaponScene.GlobalPosition = -modelCenter;

        sceneViewport.AddChild(camera);
        ApplyThumbnailMaterialToParts(weaponScene);

        await sceneViewport.ToSignal(RenderingServer.Singleton, RenderingServerInstance.SignalName.FramePostDraw);
        var image = sceneViewport.GetViewport().GetTexture().GetImage();
        sceneViewport.QueueFree();

        var imgTex = ImageTexture.CreateFromImage(image);
        if (!generatedThumbnails.ContainsKey(GetDictKey(resX, resY)))
            generatedThumbnails.Add(GetDictKey(resX, resY), imgTex);
        return imgTex;
    }
}