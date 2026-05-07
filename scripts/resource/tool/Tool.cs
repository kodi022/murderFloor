namespace MurderFloor;

[GlobalClass]
public partial class Tool : Resource
{
    [Export]
    public string PackageId { get; set; } = "base";
    [Export]
    public string ResourceId { get; set; } = "";
    [Export]
    public bool UseInGame { get; set; } = true;
    [Export]
    public string NameLocalizationKey { get; set; } = "#empty";

    [Export]
    public PackedScene MeshScene { get; set; }
    [Export]
    public string HoldTypeAnimation { get; set; } = "holdtype_pistol";

    public virtual void FirePrimary() { }
    public virtual void FireSecondary() { }
    public virtual void FireReload() { }
}