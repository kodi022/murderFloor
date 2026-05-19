namespace MurderFloor;

[GlobalClass]
public partial class Tool : MFResource
{
    [Export]
    public bool UseInGame { get; private set; } = true;
    [Export]
    public string NameLocalizationKey { get; private set; } = "#empty";

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
        public Vector3 StartPosition { get; set; }
        public Vector3 CameraForward { get; set; }
    }

    public virtual void FirePrimary(FireInfo fi) { }
    public virtual void FireSecondary(FireInfo fi) { }
    public virtual void FireReload(FireInfo fi) { }

    public virtual SlotEnum GetSlot() => SlotEnum.Special;
}