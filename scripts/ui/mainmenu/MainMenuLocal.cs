namespace MurderFloor;

public partial class MainMenuLocal : Panel
{
    [Export]
    private Button startButton;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        startButton.ButtonDown += () =>
        {
            NetworkManager.Singleton.ServerIP = default;
            NetworkManager.Singleton.Port = default;
            NetworkManager.Singleton.CreateServer(true);
            NetworkManager.Singleton.LoadGame("res://scenes/map/lobby/Lobby.tscn");
        };
    }
}