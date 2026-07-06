namespace MurderFloor;

public partial class Loot : MFResource
{
    // based on random value, with some level filtering
    // https://www.desmos.com/calculator/hlps9tjlqp
    // scales loot strength (less than wear) but also determines other factors like 
    // changes power for reforging? sockets? attachments? skins?
    public enum TierEnum
    {
        Common = 8,         // 0 - 50
        Uncommon = 7,       // 0 - 100
        Rare = 6,           // 0 - 100
        Epic = 5,           // 0 - 100
        Exotic = 4,         // 50 - 100
        Mythical = 3,       // 60 - 100
        Legendary = 2,      // 70 - 100
        Opalescent = 1,     // 80 - 100
        Transcendent = -1,  // only possible at level 100, 1/8 on ludicrous, 1/12 otherwise
        Alien = -2,         // special items
        Unknown = -3,       // special items
    }

    public static Dictionary<TierEnum, TierInfo> TierInfos { get; private set; } = new()
    {
        {TierEnum.Common,
        new TierInfo("base.loot.tier.common",
        Color.FromHtml("#aaaaaa5b"), 1.00f)},
        {TierEnum.Uncommon,
        new TierInfo("base.loot.tier.uncommon",
        Color.FromHtml("#acffb65c"), 1.03f)},
        {TierEnum.Rare,
        new TierInfo("base.loot.tier.rare",
        Color.FromHtml("#acb3ff6f"), 1.06f)},
        {TierEnum.Epic,
        new TierInfo("base.loot.tier.epic",
        Color.FromHtml("#d499ff81"), 1.09f)},
        {TierEnum.Exotic,
        new TierInfo("base.loot.tier.exotic",
        Color.FromHtml("#f153ff"), 1.12f)},
        {TierEnum.Mythical,
        new TierInfo("base.loot.tier.mythical",
        Color.FromHtml("#b1ff3da6"), 1.15f)},
        {TierEnum.Legendary,
        new TierInfo("base.loot.tier.legendary",
        Color.FromHtml("#ff9500c6"), 1.18f)},
        {TierEnum.Opalescent,
        new TierInfo("base.loot.tier.opalescent",
        Color.FromHtml("#a2e2ff"), 1.21f)},
        {TierEnum.Transcendent,
        new TierInfo("base.loot.tier.transcendent",
        Color.FromHtml("#7300ff"), 1.25f)},
        {TierEnum.Alien,
        new TierInfo("base.loot.tier.alien",
        Color.FromHtml("#006a35"), 1.10f)},
        {TierEnum.Unknown,
        new TierInfo("base.loot.tier.unknown",
        Color.FromHtml("#ffd1d1"), 1.10f)},
    };

    public struct TierInfo
    {
        public string NameLocalizationKey { get; private set; }
        public Color Color { get; private set; }
        public float PowerScale { get; private set; }
        // ! abilities?
        // ! additional attachments?
        // ! special stats?

        public TierInfo(string locKey, Color color, float powerScale)
        {
            NameLocalizationKey = locKey;
            Color = color;
            PowerScale = powerScale;
        }
    }
}