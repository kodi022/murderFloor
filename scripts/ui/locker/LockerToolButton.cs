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
        var lootResource = ResourceManager.LootRegistry.GetResourceRef(LootState.ResourceHashId);
        lootRarity = new LootRarity(LootState);
        isTool = lootResource is Tool;

        levelLabel.Text = lootRarity.Level.ToString();
        rect.Texture = await lootResource.GenerateThumbnailImage(256, 128);

        if (isTool)
        {
            weightLabel.Text = $"[img=16]res://images/ui/TablerWeight.png[/img]{((Tool)lootResource).CarryWeight}";
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

        OffsetTransformScale = Vector2.One;
        ZIndex = 0;
        var defaultVal = 0.1764f;
        colorRect.Color = new Color(defaultVal, defaultVal, defaultVal);

        if (isTool)
        {
            if (LootState == lockerSelected)
            {
                ZIndex = 1;
                OffsetTransformScale = new Vector2(1.12f, 1.12f);
            }

            if (Player.Self.HasTool(LootState))
                colorRect.Color = new Color(defaultVal, 0.25f, defaultVal);
        }
        else
        {
            if (LootState.GetCustomData("g", out string value))
            {
                if (Compression.AB64ToInt(value) == lockerSelected.GetHashCode())
                {
                    colorRect.Color = new Color(0.2f, 0.33f, 0.2f);
                }
                else
                {
                    colorRect.Color = new Color(0.2f, 0.2f, 0.33f);
                }
            }
        }
    }
}