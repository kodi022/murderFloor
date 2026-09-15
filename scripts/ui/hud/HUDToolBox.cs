namespace Shooter.Ui;

using Game;

public partial class HudToolBox : Panel
{
    [Export]
    public NinePatchRect NinePatchRect { get; private set; }
    [Export]
    public TextureRect TextureRect { get; private set; }
    [Export]
    public Label Label { get; private set; }

    public Tool Tool { get; set; }

    public bool Equipped { get; set; } = true;

    public override async void _Ready()
    {
        NinePatchRect.Visible = Equipped;

        var texture = await Tool.ToolResource.GenerateThumbnailImage(256, 128);
        if (!IsInstanceValid(TextureRect)) return;
        TextureRect.Texture = texture;

        Label.Text = $"{Tool.CurrentMag} / {Tool.CurrentReserve}";

        if (Equipped)
        {
            var tween = CreateTween();
            tween.TweenProperty(this, "offset_transform_position", new Vector2(0f, -8f), 0.15f).SetTrans(Tween.TransitionType.Bounce);
            tween.TweenProperty(this, "offset_transform_position", new Vector2(0f, 0f), 0.15f).SetTrans(Tween.TransitionType.Bounce);
        }
    }

    public override void _Process(double delta)
    {
        NinePatchRect.Visible = Equipped;
        if (!Equipped) return;

        Label.Text = $"{Tool.CurrentMag} / {Tool.CurrentReserve}";
    }
}