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
        Secondary,
        Primary,
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
}