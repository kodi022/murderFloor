namespace Shooter.Ui;

public partial class FramerateDisplay : Control
{
    [Export]
    private RichTextLabel label;

    public override void _Process(double delta)
    {
        label.Text = $"{Engine.GetFramesPerSecond():#}[img=24]res://images/ui/TablerHeartRateMonitor.png[/img]";
    }
}