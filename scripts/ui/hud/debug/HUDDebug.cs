namespace Shooter.Ui.Debug;

public partial class HudDebug : Control
{
    private readonly List<Label> labels = [];

    public override void _EnterTree()
    {
        foreach (var child in GetChild(0).GetChildren())
        {
            labels.Add((Label)child);
        }
    }

    // 16 labels, 0 - 15
    public override void _Process(double delta)
    {
        if (Game.Game.Current is null)
        {
            labels[0].Text = $"game state:null";
            return;
        }
        labels[0].Text = $"game state:{Game.Game.Current.GameState}";
        labels[1].Text = $"game maxwave:{Game.Game.Current.MaxWave} wave:{Game.Game.Current.Wave}";
        labels[2].Text = $"game wavemobleft:{Game.Game.Current.WaveMobsLeft}";
        labels[3].Text = $"game mobmax:{Game.Game.Current.MaxActiveMobs} mobactive:{Game.Game.Current.ActiveMobs}";
    }
}