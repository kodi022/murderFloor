namespace MurderFloor;

public partial class Menu : Control
{
    [Export]
    private Button button0;
    [Export]
    private Button button1;
    [Export]
    private Button buttonOption;
    [Export]
    private Button buttonExit;
    [Export]
    private Button buttonExitDesktop;

    private Control openMenu;

    public override void _Ready()
    {
        base._Ready();
        buttonOption.Pressed += OptionButton;
        buttonExit.Pressed += ExitButton;
        buttonExitDesktop.Pressed += ExitDesktopButton;
    }

    private void OptionButton()
    {
        openMenu = GD.Load<PackedScene>("res://scenes/ui/options/OptionsMenu.tscn").Instantiate<Control>();
        AddChild(openMenu);
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
