namespace MurderFloor;

public partial class HUDToolBox : Panel
{
    [Export]
    public NinePatchRect NinePatchRect { get; private set; }
    [Export]
    public TextureRect TextureRect { get; private set; }
    [Export]
    public Label Label { get; private set; }

    public LiveTool LiveTool { get; set; }

    public bool Equipped { get; set; } = true;

    public override async void _Ready()
    {
        NinePatchRect.Visible = Equipped;
        TextureRect.Texture = await LiveTool.ToolResource.GenerateThumbnailImage(256, 128);
        Label.Text = $"{LiveTool.CurrentMag} / {LiveTool.CurrentReserve}";

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

        Label.Text = $"{LiveTool.CurrentMag} / {LiveTool.CurrentReserve}";
    }
}