namespace Shooter.Ui;

public partial class MainMenuExitButton : Button
{
    public override void _Ready()
    {
        ButtonUp += () =>
        {
            Global.NetworkManager.Singleton.CloseServer();
            GetTree().Quit();
        };
    }
}
