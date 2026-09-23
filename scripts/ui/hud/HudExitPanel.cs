namespace Shooter.Ui;

using Game;

public partial class HudExitPanel : Control
{
    private Label label;

    public override void _Ready()
    {
        label = GetChild<Label>(0);
    }

    public override void _Process(double delta)
    {
        Visible = false;

        if (Game.Current is not null)
        {
            if (Game.Current.GameState == Game.StateEnum.Ended)
            {
                Visible = true;
                label.Text = "Exit";
                var shape = Game.Current.ExitArea.GetChild<CollisionShape3D>(0);
                var margin = new Vector2(20, 20);
                var viewportSize = GetViewportRect().Size;
                if (Player.Self.Camera.IsPositionInFrustum(shape.GlobalPosition + Vector3.Up))
                {
                    var unproject = Player.Self.Camera.UnprojectPosition(shape.GlobalPosition + Vector3.Up);
                    Position = unproject.Clamp(margin, viewportSize - margin);
                }
                else
                {
                    // ! do later
                    Visible = false;
                }
            }
            return;
        }

        if (GameLobby.Current is not null)
        {
            if (MapSelect.SelectedMapNetworked is not null)
            {
                Visible = true;
                label.Text = "Start";
                var shape = GameLobby.Current.ExitArea.GetChild<CollisionShape3D>(0);
                var margin = new Vector2(20, 20);
                var viewportSize = GetViewportRect().Size;
                if (Player.Self.Camera.IsPositionInFrustum(shape.GlobalPosition + Vector3.Up))
                {
                    var unproject = Player.Self.Camera.UnprojectPosition(shape.GlobalPosition + Vector3.Up);
                    Position = unproject.Clamp(margin, viewportSize - margin);
                }
                else
                {
                    // ! do later
                    Visible = false;
                }
            }
            return;
        }
    }
}
