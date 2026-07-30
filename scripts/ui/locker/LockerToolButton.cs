namespace MurderFloor;

using Loot;

public partial class LockerToolButton : Control
{
    public LootState LootStateInfo { get; set; }

    [Export]
    public Button Button { get; private set; }
    [Export]
    private ColorRect colorRect;
    [Export]
    private NinePatchRect ninePatchRect;
    [Export]
    private TextureRect rect;
    [Export]
    private Label label;

    public override async void _Ready()
    {
        var lootResource = ResourceManager.LootRegistry.GetResourceRef(LootStateInfo.HashId);
        var lootRarity = new LootRarity(LootStateInfo);

        label.Text = lootRarity.Level.ToString();
        rect.Texture = await lootResource.GenerateThumbnailImage(128, 80);
        colorRect.Color = Tiers.TierList[lootRarity.Tier].Color;

        if (Player.Self.HasTool(LootStateInfo))
            ninePatchRect.Modulate = new Color(1, 1, 1);
        else
            ninePatchRect.Modulate = Tiers.TierList[lootRarity.Tier].Color;
    }

    public override GodotObject _MakeCustomTooltip(string forText)
    {
        var control = new Control();
        control.CustomMinimumSize = new Vector2(200, 200);
        var label = new Label() { Text = forText };
        control.AddChild(label);
        return control;
    }
}