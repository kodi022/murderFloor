namespace MurderFloor;

[GlobalClass]
public partial class ToolFirearmProjectile : ToolFirearm
{
    [Export]
    public PackedScene ProjectileScene { get; private set; }
    [Export]
    public float ProjectileVelocity { get; private set; }
    [Export]
    public float Damage { get; private set; } = 10f;
    [Export]
    public Vector2 DegreeSpread { get; private set; } = new Vector2(3f, 3f);
}