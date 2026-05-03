namespace MurderFloor;

public partial class MainMenuExitButton : Button
{
    public override void _Ready()
    {
        ButtonUp += () =>
        {
            NetworkManager.Current.CloseServer();
            GetTree().Quit();
        };
    }
}
