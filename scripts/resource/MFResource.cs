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
    public string LootInclusionVer { get; private set; } = "0.1.0"; // formatted like "0.1.0" or "3.12.8"

    [Export]
    public string NameLocalizationGroup { get; private set; } = "";

    public string NameLocalizationKey => $"{NameLocalizationGroup}.{FullId}";

    public void BuildIds()
    {
        FullId = $"{PackageId}:{ResourceId}";
        HashId = Hashing.StableHash(FullId);
    }

    public virtual async Task<ImageTexture> GenerateThumbnailImage(int resX, int resY) => Global.MissingTextureImage;

    public virtual Node3D BuildToolScene() => new();

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

    public struct BuiltToolData
    {
        public int ToolHashId { get; set; }

        public Node3D Node3D { get; private set; }
        public Vector3 SightPosition { get; private set; }
        public Vector3 MuzzleFlarePosition { get; private set; }

        public BuiltToolData(int toolHashId)
        {
            ToolHashId = toolHashId;
            Build();
        }

        // ! change to get attachments attached to it instead of save for networking
        private void Build()
        {
            var toolResource = ResourceManager.ToolRegistry.GetResourceRef(ToolHashId);
            var meshScene = toolResource.MeshScene.Instantiate<Node3D>();

            Node3D FindNode(string name)
            {
                var thing = (Node3D)meshScene.FindChildren(name).FirstOrDefault(new Node3D());
                if (!thing.IsInsideTree())
                    GD.PrintErr($"Warning: {toolResource.FullId} has no Node3D named \"{name}\"");

                return thing;
            }

            var muzzleNode = FindNode("Point-Muzzle");
            var sightNode = FindNode("Point-Sight");
            var sightAttachmentNode = FindNode("Point-SightAttachment");
            var ejectionNode = FindNode("Point-Ejection");
            var foregripNode = FindNode("Point-Foregrip");
            var gadgetNode = FindNode("Point-Gadget");

            SightPosition = sightNode.Position.Rotated(Vector3.Up, -meshScene.Rotation.Y);

            foreach (var item in SaveManager.CurrentSave.Loot)
            {
                var lootState = Loot.LootState.Deserialize(item);

                if (lootState.GetCustomData("g", out string gunLootStateHashId))
                {

                }
            }



            if (sightAttachmentNode.IsInsideTree())
            {
                var etec = ResourceManager.AttachmentRegistry.GetResourceRef("base:etec");
                var etecModelScene = etec.MeshScene.Instantiate<Node3D>();
                sightAttachmentNode.AddChild(etecModelScene);
                etecModelScene.RotationDegrees = new Vector3(0, etec.MeshSceneImportYaw, 0);
                var attachSightNode = (Node3D)etecModelScene.FindChildren("Sight").FirstOrDefault(new Node3D());
                SightPosition = sightAttachmentNode.Position.Rotated(Vector3.Up, -meshScene.Rotation.Y);
                SightPosition += attachSightNode.Position.Rotated(Vector3.Up, -etecModelScene.Rotation.Y);
            }

        }
    }
}