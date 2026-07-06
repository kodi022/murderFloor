namespace MurderFloor;

[GlobalClass]
public partial class Map : MFResource
{
    [Export]
    public PackedScene Scene { get; private set; }

    [Export]
    public float DifficultyScale { get; private set; } = 1f;
}