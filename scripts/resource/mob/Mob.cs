namespace MurderFloor;

[GlobalClass]
public partial class Mob : MFResource
{
    [Export]
    public PackedScene MeshScene { get; private set; }
    [Export]
    public float MaxHealth { get; private set; }
    [Export]
    public float Health { get; private set; }
    [Export]
    public float Armor { get; private set; }

    [Export, ExportSubgroup("Enragement")]
    public bool Enragement { get; private set; }
    [Export]
    public float EnragementDamageThreshold { get; private set; }
    [Export]
    public float EnragementLength { get; private set; }
}