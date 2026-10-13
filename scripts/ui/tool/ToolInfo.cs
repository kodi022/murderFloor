namespace Shooter.Ui;

public partial class ToolInfo : Control
{
    [Export]
    private RichTextLabel title;
    [Export]
    private RichTextLabel tierLabel;
    [Export]
    private RichTextLabel wearLabel;
    [Export]
    private RichTextLabel statsLabel;

    private record StatDisplay(string PostFix = "", string NumberFormat = "0.##", string RangeSeparator = "-", string ModifierPrefix = "x")
    {
        public string Postfix = PostFix;
        public string NumberFormat = NumberFormat;
        public string RangeSeparator = RangeSeparator;
        public string ModifierPrefix = ModifierPrefix;
    }

    private static readonly Dictionary<string, StatDisplay> StatDisplays = new()
    {
        ["RPM"] = new("rpm", "0"),
        ["ReloadDelayMs"] = new("ms", "0"),
        ["ManualFireDelayMs"] = new("ms", "0"),
        ["PelletCount"] = new(""),
        ["InitialDegreeSpread"] = new("°"),
        ["MaxDegreeSpread"] = new("°"),
        ["..."] = new(""),
    };

    public void Build(ToolConfig toolConfig)
    {
        var ls = toolConfig.LootState;
        var itemRes = (Resource.ItemResource)ls.GetLootRef();
        var lootRarity = new Resource.Loot.LootRarity(ls);
        title.Text = itemRes.FullId;
        wearLabel.Text = $"[color={Resource.Loot.Wears.WearInfos[lootRarity.Wear]}]{lootRarity.Wear}";
        tierLabel.Text = $"[color={Resource.Loot.Tiers.TierInfos[lootRarity.Tier].Color.ToHtml()}]{lootRarity.Tier}";

        var str = "";
        foreach (var stat in toolConfig.SummedStatMultipliers)
        {
            var name = stat.Key;
            var font = "\n[font=res://fonts/IBMPlexMono-Regular.ttf]";
            var formatted = FormatStat(stat.Key, stat.Value, itemRes);
            var end = $"[/font]\n\n";
            str += name + font + formatted + end;
        }

        statsLabel.Text = str;
    }

    private static string FormatStat(string statName, float modifier, Resource.ItemResource res)
    {
        StatDisplay sd;
        string FmtFlt(float number) => number.ToString(sd.NumberFormat);

        var rawVal = res.GetStat(statName, modifier);

        if (StatDisplays.TryGetValue(statName, out var d))
            sd = d;
        else
            sd = StatDisplays["..."];

        var formattedVal = rawVal.VariantType switch
        {
            Variant.Type.Vector2 => string.Join(
                sd.RangeSeparator,
                FmtFlt(((Vector2)rawVal).X),
                FmtFlt(((Vector2)rawVal).Y)
            ),
            Variant.Type.Float => FmtFlt((float)rawVal),
            Variant.Type.Int => ((int)rawVal).ToString(sd.NumberFormat),
            _ => rawVal.ToString(),
        };

        var val = $"{formattedVal}{sd.Postfix}".PadRight(20);
        var mod = $"[color=afa][font_size=10]{sd.ModifierPrefix}{modifier:0.##}[/font_size][/color]";

        return val + mod;
    }
}
