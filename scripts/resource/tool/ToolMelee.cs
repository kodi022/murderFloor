namespace MurderFloor;

[GlobalClass]
public partial class ToolMelee : Tool
{
    [Export]
    public float RPM { get; private set; }
    [Export]
    public float Damage { get; private set; } = 20f;
    [Export]
    public float MaxRange { get; private set; } = 1f;
}