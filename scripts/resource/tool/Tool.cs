namespace MurderFloor;

[GlobalClass]
public partial class Tool : MFResource
{
    [Export]
    public int CarryWeight { get; private set; } = 2;
    [Export]
    public PackedScene ViewmodelScene { get; private set; }
    [Export]
    public float ViewmodelImportYaw { get; private set; }

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
        public Transform3D ViewTransform { get; set; }
        public readonly Vector3 ViewForward => -ViewTransform.Basis.Z.Normalized();
    }

    public virtual SlotEnum GetSlot() => SlotEnum.Special;
}