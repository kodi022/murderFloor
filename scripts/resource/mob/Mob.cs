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

    [Export, ExportSubgroup("Attacking")]
    public ulong AttackRateMs { get; private set; } = 1000ul;
    [Export]
    public float AttackRange { get; private set; } = 1.2f;

    [Export, ExportSubgroup("Enragement")]
    public bool Enragement { get; private set; }
    [Export]
    public float EnragementDamageThreshold { get; private set; }
    [Export]
    public float EnragementLength { get; private set; }
}