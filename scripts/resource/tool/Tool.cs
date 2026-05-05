namespace MurderFloor;

[GlobalClass]
public partial class Tool : Resource
{
    [Export]
    public string PackageId { get; set; } = "base";
    [Export]
    public string Id { get; set; } = "";

    public virtual void FirePrimary() { }
    public virtual void FireSecondary() { }
    public virtual void FireReload() { }
}