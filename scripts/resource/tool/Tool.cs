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

    public virtual void FirePrimary() { }
    public virtual void FireSecondary() { }
    public virtual void FireReload() { }
}