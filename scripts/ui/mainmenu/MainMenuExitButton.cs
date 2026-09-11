namespace MurderFloor;

public partial class MainMenuExitButton : Button
{
    public override void _Ready()
    {
        ButtonUp += () =>
        {
            NetworkManager.Singleton.CloseServer();
            GetTree().Quit();
        };
    }
}
