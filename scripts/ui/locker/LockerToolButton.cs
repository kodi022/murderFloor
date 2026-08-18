namespace MurderFloor;

using Loot;

public partial class LockerToolButton : Panel
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
    private Label levelLabel;
    [Export]
    private RichTextLabel weightLabel;

    private LootRarity lootRarity;

    public override async void _Ready()
    {
        var lootResource = (Tool)ResourceManager.LootRegistry.GetResourceRef(LootStateInfo.HashId);
        lootRarity = new LootRarity(LootStateInfo);

        levelLabel.Text = lootRarity.Level.ToString();
        weightLabel.Text = $"[img]res://images/ui/icon-weight.png[/img]{lootResource.CarryWeight}";
        rect.Texture = await lootResource.GenerateThumbnailImage(256, 128);
    }

    public override Control _MakeCustomTooltip(string forText)
    {
        var control = new Control();
        control.CustomMinimumSize = new Vector2(200, 200);
        var label = new Label() { Text = forText };
        control.AddChild(label);
        return control;
    }

    public void CheckState(LootState lockerSelected)
    {
        ninePatchRect.Modulate = Tiers.TierList[lootRarity.Tier].Color;

        if (LootStateInfo == lockerSelected && Player.Self.HasTool(LootStateInfo))
            colorRect.Color = new Color(0.2f, 0.38f, 0.38f);
        else if (LootStateInfo == lockerSelected)
            colorRect.Color = new Color(0.2f, 0.35f, 0.2f);
        else if (Player.Self.HasTool(LootStateInfo))
            colorRect.Color = new Color(0.2f, 0.2f, 0.35f);
        else
            colorRect.Color = new Color(0.12f, 0.12f, 0.12f);
    }
}