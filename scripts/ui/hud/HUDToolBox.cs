namespace MurderFloor;

public partial class HUDToolBox : Panel
{
    [Export]
    public PanelContainer PanelContainer { get; private set; }
    [Export]
    public TextureRect TextureRect { get; private set; }
    [Export]
    public Label Label { get; private set; }

    public LiveTool LiveTool { get; set; }

    public bool Equipped { get; set; } = true;

    public override async void _Ready()
    {
        TextureRect.Texture = await LiveTool.ToolResource.GenerateThumbnailImage(256, 128);
        Label.Text = $"{LiveTool.CurrentMag} / {LiveTool.CurrentReserve}";
    }

    public override void _Process(double delta)
    {
        if (!Equipped) return;

        Label.Text = $"{LiveTool.CurrentMag} / {LiveTool.CurrentReserve}";
    }
}