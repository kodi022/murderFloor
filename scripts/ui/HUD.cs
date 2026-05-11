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

        var move = Player.Self.MaxHealth - Player.Self.Health;
        healthBarPanel.SetPosition(new Vector2(-move * healthBarPanel.Size.X, 0));
    }
}