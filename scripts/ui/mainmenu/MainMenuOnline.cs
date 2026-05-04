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
	[Export]
	private LineEdit nameLine;
	[Export]
	private LineEdit coolLine;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		joinButton.ButtonDown += () =>
		{
			HandleInfo();
			var error = NetworkManager.Current.JoinServer();
			if (error == Error.Ok)
			{
				GetTree().ChangeSceneToFile("res://scenes/Dev.tscn");
			}
		};

		hostButton.ButtonDown += () =>
		{
			HandleInfo();
			var error = NetworkManager.Current.CreateServer();
			if (error == Error.Ok)
			{
				GetTree().ChangeSceneToFile("res://scenes/Dev.tscn");
			}
		};

	}

	private void HandleInfo()
	{
		static string PickText(LineEdit line)
		{
			if (string.IsNullOrEmpty(line.Text)) return line.PlaceholderText.Trim();
			else return line.Text.Trim();
		}

		NetworkManager.Current.ServerIP = PickText(IPline);
		NetworkManager.Current.Port = PickText(portLine).ToInt();
		NetworkManager.Current._playerInfo["Name"] = PickText(nameLine);
		NetworkManager.Current._playerInfo["Coolness"] = PickText(coolLine);
	}
}
