namespace MurderFloor;

[GlobalClass]
public partial class Mob : MFResource
{
    [Export]
    public bool UseInGame { get; private set; } = true;
    [Export]
    public string NameLocalizationKey { get; private set; } = "#empty";

    [Export]
    public PackedScene MeshScene { get; private set; }
}