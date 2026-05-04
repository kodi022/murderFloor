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
            NetworkManager.Current.ServerIP = default;
            NetworkManager.Current.Port = default;
            NetworkManager.Current.CreateServer(true);
            NetworkManager.Current.LoadGame("res://scenes/Dev.tscn");
        };
    }
}