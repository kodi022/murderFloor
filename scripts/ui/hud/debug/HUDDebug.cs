namespace Shooter.Ui.Debug;

public partial class HudDebug : Control
{
    [Export]
    private Label debugLabel;

    public override void _Process(double delta)
    {
        debugLabel.Text = $"game state:null";

        if (Game.Game.IsValidAndOfType(out Game.GameSurvival survival))
        {
            debugLabel.Text = $"game state:{survival.GameState}\n" +
                $"game maxwave:{survival.MaxWave} wave:{survival.Wave}\n" +
                $"game wavemobleft:{survival.WaveMobsLeft}\n" +
                $"game mobmax:{survival.MobMaxActive} mobactive:{survival.ActiveMobs}"
            ;
        }
    }
}