namespace MurderFloor;

public partial class MainMenu : ScreenScaleLimiter
{
	[Export]
	private Camera3D camera;
	[Export]
	private Node3D cameraMenuPositionNode;
	[Export]
	private Node3D cameraLocalPositionNode;
	[Export]
	private Node3D cameraOnlinePositionNode;
	[Export]
	private Node3D cameraOptionsPositionNode;

	[Export]
	private Panel buttonsList;

	private Transform3D cameraTargetTransform;

	private Node OpenUI;
	private string OpenUIName = "";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		OptionsMenu.ShowReturnButton = false;
		var opt = OptionsManager.Load();
		OptionsManager.Apply(opt);
		var save = SaveManager.Load();
		SaveManager.Apply(save);
		camera.Fov = 50;

		cameraTargetTransform = cameraMenuPositionNode.Transform;
		((Button)buttonsList.GetChild(0)).ButtonDown += LocalButton;
		((Button)buttonsList.GetChild(1)).ButtonDown += OnlineButton;
		((Button)buttonsList.GetChild(2)).ButtonDown += OptionsButton;
		((Button)buttonsList.GetChild(3)).ButtonDown += ExitButton;

		var args = OS.GetCmdlineArgs();
		if (args.Contains("--delay"))
		{
			async void Delay()
			{
				await Task.Delay(500);
				ProcessArgs();
			}
			Delay();
		}
		else ProcessArgs();

		void ProcessArgs()
		{
			for (int i = 0; i < args.Length; i++)
			{
				switch (args[i].ToLowerInvariant())
				{
					case "--hostserver":
						if (i + 1 < args.Length)
						{
							i++;
							var name = args[i];
							NetworkManager.Current._playerInfo["Name"] = name;
							var error = NetworkManager.Current.CreateServer();
							if (error == Error.Ok)
							{
								GetTree().ChangeSceneToFile("res://scenes/map/Lobby.tscn");
							}
							return;
						}
						break;
					case "--joinserver":
						if (i + 1 < args.Length)
						{
							i++;
							var name = args[i];
							NetworkManager.Current._playerInfo["Name"] = name;
							var error = NetworkManager.Current.JoinServer();
							if (error == Error.Ok)
							{
								GetTree().ChangeSceneToFile("res://scenes/map/Lobby.tscn");
							}
						}
						break;
				}
			}
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		base._Process(delta);
		camera.Transform = camera.Transform.InterpolateWith(cameraTargetTransform, (float)delta * 2f);
	}

	private void LocalButton()
	{
		if (OpenUIName == "local") return;

		OpenUIName = "local";
		cameraTargetTransform = cameraLocalPositionNode.Transform;
		SwitchUI("res://scenes/ui/mainmenu/MainMenuLocal.tscn");
	}

	private void OnlineButton()
	{
		if (OpenUIName == "online") return;

		OpenUIName = "online";
		cameraTargetTransform = cameraOnlinePositionNode.Transform;
		SwitchUI("res://scenes/ui/mainmenu/MainMenuOnline.tscn");
	}

	private void OptionsButton()
	{
		if (OpenUIName == "options") return;

		OpenUIName = "options";
		cameraTargetTransform = cameraOptionsPositionNode.Transform;
		SwitchUI("res://scenes/ui/mainmenu/MainMenuOptions.tscn");
	}

	private void ExitButton()
	{
		if (OpenUIName == "exit") return;

		OpenUIName = "exit";
		cameraTargetTransform = cameraMenuPositionNode.Transform;
		SwitchUI("res://scenes/ui/mainmenu/MainMenuExit.tscn");
		//NetworkManager.Current.CloseServer();
	}

	private void SwitchUI(string path)
	{
		OpenUI?.Free();
		OpenUI = null;
		OpenUI = GD.Load<PackedScene>(path).Instantiate<Control>();
		AddChild(OpenUI);
	}
}
