namespace MurderFloor;

public partial class FramerateDisplay : Control
{
    [Export]
    private Label label;

    public override void _Process(double delta)
    {
        label.Text = $"{Engine.GetFramesPerSecond():#}";
    }
}