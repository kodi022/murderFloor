namespace Shooter.Ui;

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
	[Export]
	private LineEdit nameLine;
	[Export]
	private LineEdit coolLine;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		joinButton.ButtonDown += () =>
		{
			ParseText();
			var error = Global.NetworkManager.Singleton.JoinServer();
			if (error == Error.Ok)
			{
				GetTree().ChangeSceneToFile("res://scenes/map/lobby/Lobby.tscn");
			}
		};

		hostButton.ButtonDown += () =>
		{
			ParseText();
			var error = Global.NetworkManager.Singleton.CreateServer();
			if (error == Error.Ok)
			{
				GetTree().ChangeSceneToFile("res://scenes/map/lobby/Lobby.tscn");
			}
		};
	}

	private void ParseText()
	{
		static string PickText(LineEdit line)
		{
			if (string.IsNullOrEmpty(line.Text)) return line.PlaceholderText.Trim();
			else return line.Text.Trim();
		}

		Global.NetworkManager.Singleton.ServerIP = PickText(IPline);
		Global.NetworkManager.Singleton.Port = PickText(portLine).ToInt();
		Global.NetworkManager.Singleton._playerInfo["name"] = PickText(nameLine);
		Global.NetworkManager.Singleton._playerInfo["coolness"] = PickText(coolLine);
	}
}
