namespace MurderFloor;

[GlobalClass]
public partial class MFResource : Resource
{
    public int HashId { get; private set; } = 0;
    public string FullId { get; private set; } = "";

    [Export]
    public string PackageId { get; private set; } = "base";
    [Export]
    public string ResourceId { get; private set; } = "";

    [Export]
    public bool UseInGame { get; private set; } = true;
    [Export]
    public string NameLocalizationKey { get; private set; } = "#empty";

    public void BuildIds()
    {
        FullId = $"{PackageId}:{ResourceId}";
        HashId = Global.StableHash(FullId);
    }

    public virtual async Task<Texture> GenerateThumbnailTexture() => GD.Load<Texture2D>("res://images/missing.png");
}