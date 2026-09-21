namespace Shooter.Ui;

public partial class HudDebrief : OpenUi
{
    public override void _Ready()
    {
        Modulate = new Color(0, 0, 0);
        Scale = new Vector2(0.8f, 0.8f);
        var tween = CreateTween();
        tween
            .TweenProperty(this, "modulate", new Color(1, 1, 1), 0.4f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween
            .Parallel()
            .TweenProperty(this, "scale", Vector2.One, 0.4f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }

    public override void Close()
    {
        var tween = CreateTween();
        tween
            .TweenProperty(this, "modulate", new Color(0, 0, 0), 0.4f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        tween
            .Parallel()
            .TweenProperty(this, "scale", new Vector2(0.8f, 0.8f), 0.4f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        tween.TweenCallback(Callable.From(Free));
    }
}