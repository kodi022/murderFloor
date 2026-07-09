namespace MurderFloor;

[GlobalClass]
public partial class Attachment : MFResource
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
}