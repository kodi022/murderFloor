namespace MurderFloor;

public partial class Loot : MFResource
{
    // ! loot drop algorithm must depend on difficulty settings and player level
    // ! no high level loot on low difficulty

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
        Ultimate = 103,     // only possible at 100 (approx 2.40202%, 1/42) chances from average of 5 million level 100 spawns
        UltimateP = 107,    // only possible at 100 (approx 0.54496%, 1/183)
        UltimatePP = 110,   // only possible at 100 (approx 0.21702%, 1/461)
    }

    // based on random value, with some level filtering
    // https://www.desmos.com/calculator/hlps9tjlqp
    // scales loot strength (less than wear) but also determines other factors like reforging? or attachments?
    public enum LootTierEnum
    {
        Common = 8,       // 0 - 50    8
        Uncommon = 7,     // 0 - 100   7
        Rare = 6,         // 0 - 100   6
        Incredible = 5,   // 0 - 100   5 // ! wip name
        Mystical = 4,      // 50 - 100  4
        Legendary = 3,     // 60 - 100  3
        Opalescent = 2,     // 70 - 100   2
        Transcendent = 1,   // 80 - 100   1
        TranscendentP = -1, // 10% chance from a transcendent drop
        Bloodied = -2,      // special items // ! wip name
    }

    public Dictionary<LootTierEnum, LootTierInfo> LootTierInfos = new()
    {
        {LootTierEnum.Common,
        new LootTierInfo("base.loot.tier.common", new Color(0.60f, 0.60f, 0.60f), 0.5f)},
        {LootTierEnum.Uncommon,
        new LootTierInfo("base.loot.tier.uncommon", new Color(0.20f, 0.80f, 0.20f), 0.65f)},
        {LootTierEnum.Rare,
        new LootTierInfo("base.loot.tier.rare", new Color(0.20f, 0.60f, 1.00f), 0.8f)},
        {LootTierEnum.Incredible,
        new LootTierInfo("base.loot.tier.incredible", new Color(0.60f, 0.20f, 1.00f), 1f)},
        {LootTierEnum.Mystical,
        new LootTierInfo("base.loot.tier.mystical", new Color(0.80f, 0.40f, 1.00f), 1.3f)},
        {LootTierEnum.Legendary,
        new LootTierInfo("base.loot.tier.legendary", new Color(1.00f, 0.65f, 0.00f), 1.65f)},
        {LootTierEnum.Opalescent,
        new LootTierInfo("base.loot.tier.opalescent", new Color(0.00f, 0.90f, 0.90f), 2f)},
        {LootTierEnum.Transcendent,
        new LootTierInfo("base.loot.tier.transcendent", new Color(1.00f, 1.00f, 1.00f), 2.5f)},
        {LootTierEnum.TranscendentP,
        new LootTierInfo("base.loot.tier.transcendentp", new Color(1.00f, 1.00f, 1.00f), 2.75f)},
        {LootTierEnum.Bloodied,
        new LootTierInfo("base.loot.tier.bloodied", new Color(0.70f, 0.15f, 0.15f), 2f)},
    };

    [Export]
    public PackedScene MeshScene { get; private set; }


    public struct LootTierInfo
    {
        public string NameLocalizationKey { get; private set; }
        public Color Color { get; private set; }
        public float PowerScale { get; private set; }
        // ! abilities?

        public LootTierInfo(string locKey, Color color, float powerScale)
        {
            NameLocalizationKey = locKey;
            Color = color;
            PowerScale = powerScale;
        }
    }

    public struct LootRarityInfo
    {
        public int LootLevel { get; private set; } = 0;
        public int LootSeed { get; private set; } = 0;
        public float LootDifficultyScale { get; private set; } = 0;
        public LootTierEnum LootTier { get; private set; }
        public LootWearEnum LootWear { get; private set; }

        public LootRarityInfo(int seed, int level, Game.GameDifficultyEnum difficulty)
        {
            LootSeed = seed;
            LootLevel = level;

            // ! get information like difficulty, map, map challenge
            LootDifficultyScale = (float)difficulty / 4f;

            var rng = new RandomNumberGenerator { Seed = (ulong)seed };
            GenerateTier(rng);
            GenerateWear(rng);
        }

        private void GenerateTier(RandomNumberGenerator rng)
        {
            float maxTicket = 0;
            Dictionary<LootTierEnum, float> tiers = new();

            void AddTierChance(LootTierEnum tier)
            {
                var val = Mathf.Pow((int)tier, 1.5f);
                maxTicket += val;
                tiers.Add(tier, val);
            }

            if (LootLevel < 50) AddTierChance(LootTierEnum.Common);
            AddTierChance(LootTierEnum.Uncommon);
            AddTierChance(LootTierEnum.Rare);
            AddTierChance(LootTierEnum.Incredible);
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

            if (LootTier == LootTierEnum.Transcendent && rng.RandiRange(1, 10) == 1)
                LootTier = LootTierEnum.TranscendentP;
        }

        private void GenerateWear(RandomNumberGenerator rng)
        {
            var wear = Mathf.Max(0, rng.Randfn(LootLevel - 10, 6));
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