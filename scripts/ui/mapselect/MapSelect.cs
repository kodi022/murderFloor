namespace MurderFloor;

public partial class MapSelect : Control
{
    [Export]
    RichTextLabel locationRichTextLabel;
    [Export]
    RichTextLabel difficultyRichTextLabel;
    [Export]
    RichTextLabel overscalingRichTextLabel;
    [Export]
    RichTextLabel gorpRichTextLabel;
    [Export]
    RichTextLabel morpRichTextLabel;
    [Export]
    TextureRect mapTextureRect;


    public override void _Ready()
    {
        foreach (var map in ResourceManager.MapRegistry.GetAllResource())
        {
            var btn = new Button
            {
                CustomMinimumSize = new Vector2(40, 40),
                Position = mapTextureRect.Size * map.Value.MapLocation
            };
            mapTextureRect.AddChild(btn);
        }
    }
}
