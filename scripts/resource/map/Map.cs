namespace MurderFloor;

[GlobalClass]
public partial class Map : MFResource
{
    [Export]
    public Vector2 MapLocation { get; private set; } = Vector2.Zero;
    [Export]
    public Texture2D PreviewImage { get; private set; } = Global.MissingTexture;
    [Export]
    public float MapDifficultyScale { get; private set; } = 1f;
    [Export]
    public int MapLevelRequirement { get; private set; } = 0;
    [Export]
    public Godot.Collections.Dictionary<Game.DifficultyEnum, MFResource[]> MapCompletionLoot { get; private set; }
}