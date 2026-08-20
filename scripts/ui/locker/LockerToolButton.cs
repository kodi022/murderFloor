namespace MurderFloor;

using Loot;

public partial class LockerToolButton : Panel
{
    public LootState LootState { get; set; }

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
    private bool isTool;

    public override async void _Ready()
    {
        var lootResource = ResourceManager.LootRegistry.GetResourceRef(LootState.HashId);
        lootRarity = new LootRarity(LootState);
        isTool = lootResource is Tool;

        levelLabel.Text = lootRarity.Level.ToString();
        rect.Texture = await lootResource.GenerateThumbnailImage(256, 128);

        if (isTool)
        {
            weightLabel.Text = $"[img]res://images/ui/icon-weight.png[/img]{((Tool)lootResource).CarryWeight}";
        }
        else
        {
            weightLabel.Visible = false;
        }
    }

    public override Control _MakeCustomTooltip(string forText)
    {
        var control = new Control() { CustomMinimumSize = new Vector2(200, 200) };
        var label = new Label() { Text = forText };
        control.AddChild(label);
        return control;
    }

    public void CheckState(LootState lockerSelected)
    {
        ninePatchRect.Modulate = Tiers.TierList[lootRarity.Tier].Color;

        if (isTool)
        {
            if (LootState == lockerSelected && Player.Self.HasTool(LootState))
                colorRect.Color = new Color(0.2f, 0.38f, 0.38f);
            else if (LootState == lockerSelected)
                colorRect.Color = new Color(0.2f, 0.35f, 0.2f);
            else if (Player.Self.HasTool(LootState))
                colorRect.Color = new Color(0.2f, 0.2f, 0.35f);
            else
                colorRect.Color = new Color(0.12f, 0.12f, 0.12f);
        }
        else
        {
            if (LootState.HasCustomData("0"))
                colorRect.Color = new Color(0.2f, 0.35f, 0.2f);
            else
                colorRect.Color = new Color(0.12f, 0.12f, 0.12f);
        }
    }
}