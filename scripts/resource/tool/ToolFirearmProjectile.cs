namespace MurderFloor;

[GlobalClass]
public partial class ToolFirearmProjectile : ToolFirearm
{
    [Export]
    public PackedScene ProjectileScene { get; set; }
    [Export]
    public float ProjectileVelocity { get; set; }
}