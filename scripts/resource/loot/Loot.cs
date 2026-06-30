namespace MurderFloor;

public partial class Loot : MFResource
{
    // uses normal distribution of at level for value
    // scales loots strength
    public enum LootWearEnum
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

    // based on random value, with some level filtering
    // https://www.desmos.com/calculator/hlps9tjlqp
    // scales loot strength (less than wear) but also determines other factors like reforging? or attachments?
    public enum LootTierEnum
    {
        Common = 8,         // 0 - 50
        Uncommon = 7,       // 0 - 100
        Rare = 6,           // 0 - 100
        Epic = 5,           // 0 - 100
        Mystical = 4,       // 50 - 100
        Legendary = 3,      // 60 - 100
        Opalescent = 2,     // 70 - 100
        Transcendent = 1,   // 80 - 100 // ! rename to something else, make TranscendentP just Transcendent
        TranscendentP = -1, // only possible at level 100, 1/7 on ludicrous, 1/10 otherwise
        Alien = -2,         // special items
        Unknown = -3,       // special items
    }

    public static Dictionary<LootTierEnum, LootTierInfo> LootTierInfos { get; private set; } = new()
    {
        {LootTierEnum.Common,
        new LootTierInfo("base.loot.tier.common",
        Color.FromHtml("#aaaaaa5b"), 1.00f)},
        {LootTierEnum.Uncommon,
        new LootTierInfo("base.loot.tier.uncommon",
        Color.FromHtml("#acffb65c"), 1.05f)},
        {LootTierEnum.Rare,
        new LootTierInfo("base.loot.tier.rare",
        Color.FromHtml("#acb3ff6f"), 1.10f)},
        {LootTierEnum.Epic,
        new LootTierInfo("base.loot.tier.epic",
        Color.FromHtml("#d499ff81"), 1.15f)},
        {LootTierEnum.Mystical,
        new LootTierInfo("base.loot.tier.mystical",
        Color.FromHtml("#ffff76a6"), 1.20f)},
        {LootTierEnum.Legendary,
        new LootTierInfo("base.loot.tier.legendary",
        Color.FromHtml("#ff9500c6"), 1.25f)},
        {LootTierEnum.Opalescent,
        new LootTierInfo("base.loot.tier.opalescent",
        Color.FromHtml("#a2e2ff"), 1.30f)},
        {LootTierEnum.Transcendent,
        new LootTierInfo("base.loot.tier.transcendent",
        Color.FromHtml("#f153ff"), 1.35f)},
        {LootTierEnum.TranscendentP,
        new LootTierInfo("base.loot.tier.transcendentp",
        Color.FromHtml("#7300ff"), 1.40f)},
        {LootTierEnum.Alien,
        new LootTierInfo("base.loot.tier.alien",
        Color.FromHtml("#006a35"), 1.25f)},
        {LootTierEnum.Unknown,
        new LootTierInfo("base.loot.tier.unknown",
        Color.FromHtml("#ffd1d1"), 1.25f)},
    };

    [Export]
    public PackedScene MeshScene { get; private set; }

    public struct LootTierInfo
    {
        public string NameLocalizationKey { get; private set; }
        public Color Color { get; private set; }
        public float PowerScale { get; private set; }
        // ! abilities?
        // ! additional attachments?
        // ! special stats?

        public LootTierInfo(string locKey, Color color, float powerScale)
        {
            NameLocalizationKey = locKey;
            Color = color;
            PowerScale = powerScale;
        }
    }

    public struct LootRarityInfo
    {
        public ulong LootSeed { get; private set; } = 0;
        public int LootLevel { get; private set; } = 0;
        public LootTierEnum LootTier { get; private set; }
        public LootWearEnum LootWear { get; private set; }
        public float DifficultyStatScale { get; private set; } = 1f;

        // ! loot drop algorithm must depend on difficulty settings and player level
        // ! no high level loot on low difficulty

        // ! get information like difficulty, map, map challenge
        public LootRarityInfo(ulong seed, int level, Game.GameDifficultyEnum difficulty)
        {
            LootSeed = seed;
            LootLevel = level;

            DifficultyStatScale = difficulty switch
            {
                Game.GameDifficultyEnum.Easy => 0.85f,
                Game.GameDifficultyEnum.Medium => 0.92f,
                Game.GameDifficultyEnum.Challenging => 1f,
                Game.GameDifficultyEnum.Hard => 1.08f,
                Game.GameDifficultyEnum.Extreme => 1.15f,
                Game.GameDifficultyEnum.Ludicrous => 1.2f,
                _ => 0.8f,
            };

            var tierOffset = difficulty switch
            {
                Game.GameDifficultyEnum.Easy => -2.0f,
                Game.GameDifficultyEnum.Medium => -1.4f,
                Game.GameDifficultyEnum.Challenging => -0.7f,
                Game.GameDifficultyEnum.Hard => 0f,
                Game.GameDifficultyEnum.Extreme => 0.8f,
                Game.GameDifficultyEnum.Ludicrous => 1.7f,
                _ => -2.0f,
            };
            var wearLevelOffset = ((int)difficulty - 3) * 1.5f;

            var rng = new RandomNumberGenerator { Seed = seed };
            GenerateTier(rng, tierOffset);
            GenerateWear(rng, wearLevelOffset);
        }

        private void GenerateTier(RandomNumberGenerator rng, float tierOffset)
        {
            float maxTicket = 0;
            Dictionary<LootTierEnum, float> tiers = new();

            void AddTierChance(LootTierEnum tier)
            {
                var val = Mathf.Pow((int)tier, 2f) + tierOffset;
                maxTicket += val;
                tiers.Add(tier, val);
            }

            if (LootLevel < 50) AddTierChance(LootTierEnum.Common);
            AddTierChance(LootTierEnum.Uncommon);
            AddTierChance(LootTierEnum.Rare);
            AddTierChance(LootTierEnum.Epic);
            if (LootLevel >= 50) AddTierChance(LootTierEnum.Mystical);
            if (LootLevel >= 60) AddTierChance(LootTierEnum.Legendary);
            if (LootLevel >= 70) AddTierChance(LootTierEnum.Opalescent);
            if (LootLevel >= 80) AddTierChance(LootTierEnum.Transcendent);

            var ticket = rng.RandfRange(0, maxTicket);
            foreach (var tier in tiers.Reverse())
            {
                if (ticket <= tier.Value)
                {
                    LootTier = tier.Key;
                    break;
                }
                ticket -= tier.Value;
            }

            if (LootTier == LootTierEnum.Transcendent && LootLevel >= 100)
            {
                if (tierOffset > 1.2f)
                {
                    if (rng.RandiRange(1, 7) == 1) LootTier = LootTierEnum.TranscendentP;
                }
                else
                {
                    if (rng.RandiRange(1, 10) == 1) LootTier = LootTierEnum.TranscendentP;
                }
            }
        }

        private void GenerateWear(RandomNumberGenerator rng, float wearLevelOffset)
        {
            var wear = Mathf.Max(0, rng.Randfn(LootLevel + wearLevelOffset - 10, 6));
            // this linq is considered laggy but the alternative is a big chunk of ugly code
            var wearEnum = LootWearEnum.Broken;
            foreach (var val in Enum.GetValues(typeof(LootWearEnum)))
            {
                if (wear < (int)val) break;
                wearEnum = (LootWearEnum)val;
            }

            if (LootLevel < 100 && (int)wearEnum > (int)LootWearEnum.Perfect)
                wearEnum = LootWearEnum.Perfect;

            LootWear = wearEnum;
        }
    }
}