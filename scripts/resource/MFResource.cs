namespace MurderFloor;

public partial class MFResource : Resource
{
    private protected static Dictionary<string, ImageTexture> generatedThumbnails = [];

    public int HashId { get; private set; } = 0;
    public string FullId { get; private set; } = "";

    [Export]
    public string PackageId { get; private set; } = "base";
    [Export]
    public string ResourceId { get; private set; } = "";

    [Export]
    public PackedScene MeshScene { get; private set; }
    [Export]
    public float MeshSceneImportYaw { get; private set; }

    [Export]
    public bool UseInGame { get; private set; } = true;
    [Export]
    public bool IsLoot { get; private set; } = false;
    [Export]
    public string NameLocalizationGroup { get; private set; } = "";

    public string NameLocalizationKey => $"{NameLocalizationGroup}.{FullId}";

    public void BuildIds()
    {
        FullId = $"{PackageId}:{ResourceId}";
        HashId = Global.StableHash(FullId);
    }

    public virtual async Task<ImageTexture> GenerateThumbnailImage(int resX, int resY) => Global.MissingTextureImage;
}