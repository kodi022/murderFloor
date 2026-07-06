namespace MurderFloor;

[GlobalClass]
public partial class Attachment : Loot
{
    public enum AttachmentTypeEnum
    {
        Optic,
        Muzzle,
        Barrel,
        Trinket,
    }

    [Export]
    public AttachmentTypeEnum AttachmentType { get; private set; }
    [Export]
    public float MeshSceneImportYaw { get; private set; }
}