namespace MurderFloor;

[GlobalClass]
public partial class Tool : MFResource
{
    [Export]
    public PackedScene MeshScene { get; private set; }
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
        public Vector2 CurrentSpread { get; set; }
        public Vector3 StartPosition { get; set; }
        public Transform3D ViewTransform { get; set; }
        public readonly Vector3 ViewForward => -ViewTransform.Basis.Z;
    }

    public virtual SlotEnum GetSlot() => SlotEnum.Special;

    public override async Task<Texture> GenerateThumbnailTexture()
    {
        if (MeshScene is null) return GD.Load<Texture>("res://images/missing.png");

        var sceneViewport = new SubViewport
        {
            Size = new Vector2I(128, 128),
            OwnWorld3D = true,
        };
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(sceneViewport);

        var weaponScene = (Node3D)MeshScene.Instantiate();
        var dirLight = new DirectionalLight3D();
        var camera = new Camera3D();
        camera.SetOrthogonal(1f, 0.1f, 100f);
        camera.LookAtFromPosition(new Vector3(0, 0, 5f), Vector3.Zero);

        sceneViewport.AddChild(weaponScene);
        sceneViewport.AddChild(dirLight);
        sceneViewport.AddChild(camera);
        await sceneViewport.ToSignal(RenderingServer.Singleton, RenderingServerInstance.SignalName.FramePostDraw);
        var texture = sceneViewport.GetTexture();
        //sceneViewport.Free();
        return texture;
    }
}