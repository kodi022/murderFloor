namespace MurderFloor.Loot;

public static class Tiers
{
    /// <summary>
    /// Based on random value, with some level filtering.
    /// Scales loot strength (less than wear) but also determines other factors like 
    /// reforging? sockets? attachments? skins?
    /// </summary>
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

    public static Dictionary<TierEnum, TierInfo> TierList { get; private set; } = new()
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
        Color.FromHtml("#cb82ff81"), 1.09f)},
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
        Color.FromHtml("#766666"), 1.10f)},
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

public static class Wears
{
    // uses normal distribution of at level for value
    // scales loots strength
    public enum WearEnum
    {
        Broken = 0,
        Tarnished = 3,      // 3
        Tattered = 6,       // 3
        Worn = 10,          // 4
        Used = 14,          // 4
        Decent = 19,        // 5
        Average = 25,       // 5
        Fine = 31,          // 6
        Clean = 37,         // 6
        Spotless = 43,      // 6
        Polished = 49,      // 7
        Shiny = 56,         // 7
        Mint = 63,          // 7
        New = 70,           // 7
        Excellent = 77,     // 7
        Pristine = 84,      // 7
        Flawless = 92,      // 8
        Perfect = 100,      // 8
        Ultimate = 103,     // only possible at level 100
        UltimateP = 107,    // only possible at level 100
        UltimatePP = 110,   // only possible at level 100
    }

    public static Dictionary<WearEnum, WearInfo> WearList { get; private set; } = new()
    {
        {WearEnum.Broken,       new WearInfo("base.loot.wear.broken",       1.00f)}, // increasing by 0.04
        {WearEnum.Tarnished,    new WearInfo("base.loot.wear.tarnished",    1.04f)},
        {WearEnum.Tattered,     new WearInfo("base.loot.wear.tattered",     1.08f)},
        {WearEnum.Worn,         new WearInfo("base.loot.wear.worn",         1.12f)},
        {WearEnum.Used,         new WearInfo("base.loot.wear.used",         1.16f)},
        {WearEnum.Decent,       new WearInfo("base.loot.wear.decent",       1.20f)},
        {WearEnum.Average,      new WearInfo("base.loot.wear.average",      1.24f)},
        {WearEnum.Fine,         new WearInfo("base.loot.wear.fine",         1.29f)}, // increasing by 0.05
        {WearEnum.Clean,        new WearInfo("base.loot.wear.clean",        1.34f)},
        {WearEnum.Spotless,     new WearInfo("base.loot.wear.spotless",     1.39f)},
        {WearEnum.Polished,     new WearInfo("base.loot.wear.polished",     1.44f)},
        {WearEnum.Shiny,        new WearInfo("base.loot.wear.shiny",        1.49f)},
        {WearEnum.Mint,         new WearInfo("base.loot.wear.mint",         1.54f)},
        {WearEnum.New,          new WearInfo("base.loot.wear.new",          1.60f)}, // increasing by 0.06
        {WearEnum.Excellent,    new WearInfo("base.loot.wear.excellent",    1.66f)},
        {WearEnum.Pristine,     new WearInfo("base.loot.wear.pristine",     1.72f)},
        {WearEnum.Flawless,     new WearInfo("base.loot.wear.flawless",     1.78f)},
        {WearEnum.Perfect,      new WearInfo("base.loot.wear.perfect",      1.84f)},
        {WearEnum.Ultimate,     new WearInfo("base.loot.wear.ultimate",     1.91f)}, // increasing by 0.07
        {WearEnum.UltimateP,    new WearInfo("base.loot.wear.ultimatep",    1.98f)},
        {WearEnum.UltimatePP,   new WearInfo("base.loot.wear.ultimatepp",   2.05f)},
    };

    public struct WearInfo
    {
        public string NameLocalizationKey { get; private set; }
        public float PowerScale { get; private set; }
        // ! skin cleanness?

        public WearInfo(string locKey, float powerScale)
        {
            NameLocalizationKey = locKey;
            PowerScale = powerScale;
        }
    }
}