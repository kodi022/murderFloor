namespace Shooter.Game;

public partial class OuterControllerDead : OuterController
{
    [Export]
    private Camera3D camera3D;
    [Export]
    private Ui.HudDead hudDead;

    private Player target => hudDead.Target;

    public override void _Ready()
    {
        ViewPlayer(Player.Self);
    }

    public override void _ExitTree()
    {
        Player.Self.Camera.MakeCurrent();
    }

    public override void _Process(double delta)
    {
        camera3D.Fov = OptionsManager.CurrentOptions.FieldOfView;

        if (!target.IsDead)
        {
            camera3D.Transform = target.ViewGlobalTransform;
        }
        else
        {
            camera3D.Position = new Vector3(1f, 1.5f, 1f);
            camera3D.LookAt(Player.Self.Position + Vector3.Up * 0.8f);
        }
    }

    public void ViewPlayer(Player player)
    {
        camera3D.MakeCurrent();
        hudDead.Target = player;
    }
}