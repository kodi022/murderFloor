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

    // 16 labels, 0 - 15
    public override void _Process(double delta)
    {
        if (Game.Current is null)
        {
            labels[0].Text = $"game state:null";
            return;
        }
        labels[0].Text = $"game state:{Game.Current.GameState}";
        labels[1].Text = $"game maxround:{Game.Current.MaxRound} round:{Game.Current.Round}";
        labels[2].Text = $"game roundmobleft:{Game.Current.RoundMobsLeft}";
        labels[3].Text = $"game mobmax:{Game.Current.MaxActiveMobs} mobactive:{Game.Current.ActiveMobs}";
    }
}