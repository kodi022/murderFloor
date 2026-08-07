namespace MurderFloor;

[GlobalClass]
public partial class Map : MFResource
{
    [Export]
    public Vector2 MapLocation { get; private set; } = Vector2.Zero;
    [Export]
    public float MapDifficultyScale { get; private set; } = 1f;
}