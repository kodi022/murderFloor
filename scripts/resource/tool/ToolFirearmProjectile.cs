namespace MurderFloor;

[GlobalClass]
public partial class ToolFirearmProjectile : ToolFirearm
{
    [Export]
    public PackedScene ProjectileScene { get; private set; }
    [Export]
    public float ProjectileVelocity { get; private set; }
}