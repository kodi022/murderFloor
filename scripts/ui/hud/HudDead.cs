namespace MurderFloor;

public partial class HudDead : Control
{
    [Export]
    private Label viewingLabel;
    [Export]
    private Button lastViewingButton;
    [Export]
    private Button nextViewingButton;

    public Player Target { get; set; }

    public override void _Ready()
    {
        Target = Player.Self;
        lastViewingButton.Pressed += LastViewingPressed;
        nextViewingButton.Pressed += NextViewingPressed;
    }

    public override void _Process(double delta)
    {
        viewingLabel.Text = NetworkManager.Singleton._players[Target.Id]["Name"];
    }

    private void LastViewingPressed()
    {
        var index = Player.AllPlayers.IndexOf(Target);
        var count = Player.AllPlayers.Count;
        Player.Self.ViewPlayer(Player.AllPlayers[(index + count - 1) % count]);
    }

    private void NextViewingPressed()
    {
        var index = Player.AllPlayers.IndexOf(Target);
        Player.Self.ViewPlayer(Player.AllPlayers[(index + 1) % Player.AllPlayers.Count]);
    }
}
