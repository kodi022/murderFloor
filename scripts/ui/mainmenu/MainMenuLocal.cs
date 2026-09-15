namespace Shooter.Ui;

public partial class MainMenuLocal : Panel
{
    [Export]
    private Button startButton;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        startButton.ButtonDown += () =>
        {
            Global.NetworkManager.Singleton.ServerIP = default;
            Global.NetworkManager.Singleton.Port = default;
            Global.NetworkManager.Singleton.CreateServer(true);
            Global.NetworkManager.Singleton.LoadGame("res://scenes/map/lobby/Lobby.tscn");
        };
    }
}