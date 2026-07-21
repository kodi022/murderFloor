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
    private TextureRect rect;
    [Export]
    private Label label;

    public override void _Ready()
    {
        AsyncReady();
    }

    private async void AsyncReady()
    {
        var lootResource = ResourceManager.LootRegistry.First(c => LootStateInfo.LootHashId == c.HashId);
        var lootRarity = new LootRarity(LootStateInfo);

        label.Text = lootRarity.Level.ToString();
        rect.Texture = await lootResource.GenerateThumbnailImage(128, 80);
        colorRect.Color = Tiers.TierList[lootRarity.Tier].Color;
    }
}