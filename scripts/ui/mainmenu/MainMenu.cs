namespace MurderFloor;

public partial class MainMenu : Control
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
		cameraTargetTransform = cameraMenuPositionNode.Transform;
		((Button)buttonsList.GetChild(0)).ButtonDown += LocalButton;
		((Button)buttonsList.GetChild(1)).ButtonDown += OnlineButton;
		((Button)buttonsList.GetChild(2)).ButtonDown += OptionsButton;
		((Button)buttonsList.GetChild(3)).ButtonDown += ExitButton;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		camera.Transform = camera.Transform.InterpolateWith(cameraTargetTransform, (float)delta * 2f);
	}

	private void LocalButton()
	{
		cameraTargetTransform = cameraLocalPositionNode.Transform;
		NetworkManager.Current.ServerIP = default;
		NetworkManager.Current.Port = default;
		NetworkManager.Current.CreateServer();
		GetTree().ChangeSceneToFile("res://scenes/Dev.tscn");
	}

	private void OnlineButton()
	{
		if (OpenUIName == "online") return;

		cameraTargetTransform = cameraOnlinePositionNode.Transform;
		OpenUI?.Free();
		OpenUI = GD.Load<PackedScene>("res://scenes/ui/mainmenu/MainMenuOnline.tscn").Instantiate();
		GetTree().Root.AddChild(OpenUI);
		OpenUIName = "online";
		//Rpc("LoadGame", "res://scenes/Dev.tscn");
	}

	private void OptionsButton()
	{
		if (OpenUIName == "options") return;

		cameraTargetTransform = cameraOptionsPositionNode.Transform;
		OpenUI?.Free();
		//OpenUI = GD.Load<PackedScene>("res://scenes/ui/mainmenu/MainMenuOnline.tscn").Instantiate();
		OpenUIName = "options";
	}

	private void ExitButton()
	{
		NetworkManager.Current.CloseServer();

	}
}
