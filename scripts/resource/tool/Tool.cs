namespace MurderFloor;

[GlobalClass]
public partial class Tool : MFResource
{
    [Export]
    public int CarryWeight { get; private set; } = 2;

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
        public Vector3 StartPosition { get; set; }
        public Transform3D ViewTransform { get; set; }
        public readonly Vector3 ViewForward => -ViewTransform.Basis.Z;
    }

    public virtual SlotEnum GetSlot() => SlotEnum.Special;
}