namespace Shooter.Resource;

[GlobalClass]
public partial class Map : GameResource
{
    [Export]
    public Vector2 MapLocation { get; private set; } = Vector2.Zero;
    [Export]
    public Texture2D PreviewImage { get; private set; } = Utils.Defaults.MissingTexture;
    [Export]
    public float MapDifficultyScale { get; private set; } = 1f;
    [Export]
    public int MapLevelRequirement { get; private set; } = 0;
    [Export]
    public Godot.Collections.Dictionary<Game.Game.DifficultyEnum, GameResource[]> MapCompletionLoot { get; private set; }
}