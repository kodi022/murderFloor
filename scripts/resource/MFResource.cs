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
    [Export] // this is for splitting the LootRegistry for older loot
    public string LootInclusionVer { get; private set; } = "0.1.0"; // formatted like "0.1.0" or "3.12.26"

    [Export]
    public string NameLocalizationGroup { get; private set; } = "";

    public string NameLocalizationKey => $"{NameLocalizationGroup}.{FullId}";

    public void BuildIds()
    {
        FullId = $"{PackageId}:{ResourceId}";
        HashId = Hashing.StableHash(FullId);
    }

    public virtual async Task<ImageTexture> GenerateThumbnailImage(int resX, int resY) => Global.MissingTextureImage;

    public virtual BuiltToolData BuildToolScene(BuildToolData buildToolData) => new();

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
        foreach (var child in weaponScene.GetChildren())
        {
            if (child is MeshInstance3D mesh)
            {
                mesh.MaterialOverride = GD.Load<Material>("res://materials/thumbnail.tres");
            }
        }
    }

    public struct BuildToolData
    {
        public int[] AttachmentHashIds { get; set; }
    }

    public struct BuiltToolData
    {
        public int ToolHashId { get; set; }
        public Node3D Node3D { get; set; }
        public Vector3 SightPosition { get; set; }
        public Vector3 MuzzleFlarePosition { get; set; }
    }
}