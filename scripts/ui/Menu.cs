namespace Shooter.Ui;

public partial class Menu : OpenUi
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

        Scale = new Vector2(0.8f, 0.8f);
        var tween = CreateTween();
        tween
            .TweenProperty(this, "scale", Vector2.One, 0.4f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    public override void Close()
    {
        var tween = CreateTween();
        tween
            .TweenProperty(this, "scale", new Vector2(0.8f, 0.8f), 0.4f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        tween.TweenCallback(Callable.From(Free));
    }

    public override void _Process(double delta)
    {
        if (IsInstanceValid(openMenu))
            GetChild<Control>(0).Modulate = new Color(0, 0, 0, 0);
        else
            GetChild<Control>(0).Modulate = new Color(1, 1, 1, 1);
    }


    private void OptionButton()
    {
        openMenu = GD.Load<PackedScene>("res://scenes/ui/options/OptionsMenu.tscn").Instantiate<Control>();
        AddChild(openMenu);
    }

    private void ExitButton()
    {
        Global.NetworkManager.Singleton.CloseServer();
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
        foreach (var player in Game.Player.AllPlayers) player.QueueFree();
    }

    private void ExitDesktopButton()
    {
        Global.NetworkManager.Singleton.CloseServer();
        GetTree().Quit();
    }
}
