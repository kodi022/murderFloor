namespace MurderFloor;

public partial class MainMenuOnline : Panel
{
	[Export]
	private Button joinButton;
	[Export]
	private Button hostButton;

	[Export]
	private LineEdit IPline;
	[Export]
	private LineEdit portLine;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		joinButton.ButtonDown += () =>
		{
			NetworkManager.Current.ServerIP = IPline.Text;
			NetworkManager.Current.Port = portLine.Text.ToInt();
			var error = NetworkManager.Current.JoinServer();

			if (error == Error.Ok)
			{
				NetworkManager.Current.LoadGame("res://scenes/Dev.tscn");
			}
		};
		hostButton.ButtonDown += () =>
		{
			NetworkManager.Current.ServerIP = IPline.Text;
			NetworkManager.Current.Port = portLine.Text.ToInt();
			NetworkManager.Current.CreateServer();
			NetworkManager.Current.LoadGame("res://scenes/Dev.tscn");
		};
	}
}
