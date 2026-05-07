namespace MurderFloor;

[GlobalClass]
public partial class ToolFirearmHitscan : ToolFirearm
{
    [Export]
    public Vector2 Damages { get; set; } = new Vector2(10f, 4f);
    [Export]
    public Vector2 FalloffRanges { get; set; } = new Vector2(40f, 50f);
    [Export]
    public float MaxRange = 80f;
    [Export]
    public Vector2 DegreeSpread { get; set; } = new Vector2(3f, 3f);
}