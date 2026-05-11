namespace MurderFloor;

public partial class HUD : Control
{
    [Export]
    private Label playersLabel;

    [Export]
    private Panel healthBarPanel;

    public override void _Process(double delta)
    {
        string players = "";
        foreach (var player in NetworkManager.Current._players)
        {
            players += $"{player.Key} {player.Value["Name"]}\n";
        }
        playersLabel.Text = players;

        healthBarPanel.SetPosition(new Vector2(-10, 0));
    }
}