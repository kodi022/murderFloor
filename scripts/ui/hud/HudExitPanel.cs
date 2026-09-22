namespace Shooter.Ui;

using Game;

public partial class HudExitPanel : Control
{
    private Label label;

    public override void _Ready()
    {
        label = GetChild<Label>(0);
        Visible = false;
    }

    public override void _Process(double delta)
    {
        if (Game.Current is not null)
        {
            if (Game.Current.GameState == Game.StateEnum.Ended)
            {
                Visible = true;
                var shape = Game.Current.ExitArea.GetChild<CollisionShape3D>(0);
                var unproject = Player.Self.Camera.UnprojectPosition(shape.Position + Vector3.Up * 2f);
                Position = unproject + new Vector2(-250f, 0f);
            }

            return;
        }

        if (GameLobby.Current is not null)
        {
            if (MapSelect.SelectedMapNetworked is not null)
            {
                Visible = true;
                var shape = GameLobby.Current.ExitArea.GetChild<CollisionShape3D>(0);
                var unproject = Player.Self.Camera.UnprojectPosition(shape.Position + Vector3.Up * 2f);
                Position = unproject + new Vector2(-250f, 0f);
            }

            return;
        }
    }
}
