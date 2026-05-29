namespace MurderFloor;

public partial class Menu : Control
{
    [Export]
    private Button button0;
    [Export]
    private Button button1;
    [Export]
    private Button button2;
    [Export]
    private Button buttonExit;
    [Export]
    private Button buttonExitDesktop;

    public override void _Ready()
    {
        base._Ready();
        buttonExit.Pressed += ExitButton;
        buttonExitDesktop.Pressed += ExitDesktopButton;
    }

    private void ExitButton()
    {
        NetworkManager.Current.CloseServer();
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
        foreach (var player in Player.AllPlayers) player.QueueFree();
    }

    private void ExitDesktopButton()
    {
        NetworkManager.Current.CloseServer();
        GetTree().Quit();
    }
}
