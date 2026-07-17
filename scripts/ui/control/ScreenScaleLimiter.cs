namespace MurderFloor;

public partial class ScreenScaleLimiter : Control
{
    private readonly Vector2 _baseSize = new(1920, 1080);
    private Vector2 viewportSize;

    public override void _Process(double delta)
    {
        var newViewportSize = GetViewportRect().Size;
        if (viewportSize != newViewportSize)
        {
            viewportSize = newViewportSize;

            var aspectRatio = viewportSize.X / viewportSize.Y;

            if (aspectRatio > 1.7778f)
            {
                Size = new Vector2I((int)(viewportSize.Y * 1.7778f), (int)viewportSize.Y);
                Position = (viewportSize - _baseSize) * 0.5f;
            }
            else
            {
                Size = new Vector2I((int)viewportSize.X, (int)viewportSize.Y);
                Position = Vector2.Zero;
            }
        }
    }
}