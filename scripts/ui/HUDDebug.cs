namespace MurderFloor;

public partial class HUDDebug : Control
{
    private readonly List<Label> labels = [];

    public override void _EnterTree()
    {
        foreach (var child in GetChild(0).GetChildren())
        {
            labels.Add((Label)child);
        }
    }

    public override void _Process(double delta)
    {
        labels[0].Text = $"game state:{Game.GameState}";
        labels[1].Text = $"game maxwave:{Game.MaxWave} wave:{Game.Wave}";
        labels[2].Text = $"game mobwaveleft:{Game.WaveMobsLeft}";
        labels[3].Text = $"game mobmax:{Game.MaxActiveMobs} mobactive:{Game.ActiveMobs}";
    }
}