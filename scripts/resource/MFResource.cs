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

    [Export, ExportSubgroup("Loot")]
    public bool IsRandomLoot { get; private set; } = false;
    [Export]
    public int LootMinimumSpawnLevel { get; private set; } = 0;

    [Export, ExportSubgroup("")]
    public string InclusionVersion { get; private set; } = "0.1.0"; // see also: Version

    [Export]
    public string NameLocalizationGroup { get; private set; } = "";

    public string NameLocalizationKey => $"{NameLocalizationGroup}.{FullId}";

    public void BuildIds()
    {
        FullId = $"{PackageId}:{ResourceId}";
        HashId = Hashing.StableHash(FullId);
    }

    public virtual async Task<ImageTexture> GenerateThumbnailImage(int resX, int resY) => Global.MissingTextureImage;

    /// <summary> Builds the tool itself with additional data. DOES NOT BUILD VIEWMODEL </summary>
    public virtual BuiltToolData BuildToolScene(ToolConfig toolConfig) => new();

    public static Aabb GetBounds(Node3D weaponScene)
    {
        var bounds = new Aabb();
        if (weaponScene.IsQueuedForDeletion()) return bounds;

        if (weaponScene is VisualInstance3D inst)
        {
            bounds = inst.GetAabb();
        }

        foreach (var child in weaponScene.GetChildren())
        {
            if (child is not VisualInstance3D childInst) continue;
            if (childInst.GetAabb() == default) continue;

            var childBounds = childInst.GetAabb();
            bounds = bounds.Merge(childBounds);
        }

        bounds = weaponScene.Transform * bounds;

        return bounds;
    }

    private protected static void ApplyThumbnailMaterialToParts(Node3D weaponScene)
    {
        foreach (var child in weaponScene.FindChildren("*", nameof(MeshInstance3D)))
        {
            if (child is MeshInstance3D mesh)
            {
                mesh.MaterialOverride = GD.Load<Material>("res://materials/thumbnail.tres");
            }
        }
    }

    public struct BuiltToolData
    {
        public int ToolHashId { get; set; }
        public Node3D Tool { get; set; }
        public Vector3 SightPositionOffset { get; set; }
        public Vector3 MuzzlePosition { get; set; }
    }
}