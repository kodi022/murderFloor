namespace MurderFloor.Loot;

/// <summary>
/// The rarity info of Loot, built from LootState
/// </summary>
public struct LootRarity
{
    public ulong Seed { get; private set; } = 0;
    public int Level { get; private set; } = 0;
    public Tiers.TierEnum Tier { get; private set; }
    public Wears.WearEnum Wear { get; private set; }
    public double SuperScale { get; private set; }

    // loot drop algorithm must depend on difficulty settings and player level
    // no high level loot on low difficulty
    public LootRarity(LootState lootState)
    {
        Seed = lootState.Seed;
        Level = lootState.Level;

        var tierOffset = lootState.Difficulty switch
        {
            Game.DifficultyEnum.Easy => -2.0f,
            Game.DifficultyEnum.Medium => -1.4f,
            Game.DifficultyEnum.Challenging => -0.7f,
            Game.DifficultyEnum.Hard => 0f,
            Game.DifficultyEnum.Extreme => 0.8f,
            Game.DifficultyEnum.Ludicrous => 1.7f,
            _ => -2.0f,
        };
        var wearLevelOffset = ((int)lootState.Difficulty - 3) * 1.5f;

        SuperScale = lootState.ChallengeScaling * 0.5f;
        SuperScale += lootState.OverScaling;
        // ! map affect Superscale

        var rng = new RandomNumberGenerator { Seed = Seed };
        GenerateTier(rng, tierOffset);
        GenerateWear(rng, wearLevelOffset);
    }

    private void GenerateTier(RandomNumberGenerator rng, float tierOffset)
    {
        float maxTicket = 0;
        Dictionary<Tiers.TierEnum, float> tiers = new();

        void AddTierChance(Tiers.TierEnum tier)
        {
            var val = Mathf.Pow((int)tier, 2.1f) + tierOffset;
            maxTicket += val;
            tiers.Add(tier, val);
        }

        if (Level < 50) AddTierChance(Tiers.TierEnum.Common);
        AddTierChance(Tiers.TierEnum.Uncommon);
        AddTierChance(Tiers.TierEnum.Rare);
        AddTierChance(Tiers.TierEnum.Epic);
        if (Level >= 50) AddTierChance(Tiers.TierEnum.Exotic);
        if (Level >= 60) AddTierChance(Tiers.TierEnum.Mythical);
        if (Level >= 70) AddTierChance(Tiers.TierEnum.Legendary);
        if (Level >= 80) AddTierChance(Tiers.TierEnum.Opalescent);

        var ticket = rng.RandfRange(0, maxTicket);
        foreach (var tier in tiers.Reverse())
        {
            if (ticket <= tier.Value)
            {
                Tier = tier.Key;
                break;
            }
            ticket -= tier.Value;
        }

        if (Tier == Tiers.TierEnum.Opalescent && Level >= 100)
        {
            if (tierOffset > 1.2f)
            {
                if (rng.RandiRange(1, 8) == 1) Tier = Tiers.TierEnum.Transcendent;
            }
            else
            {
                if (rng.RandiRange(1, 12) == 1) Tier = Tiers.TierEnum.Transcendent;
            }
        }
    }

    private void GenerateWear(RandomNumberGenerator rng, float wearLevelOffset)
    {
        var wear = Mathf.Max(0, rng.Randfn(Level + wearLevelOffset - 10, 6));
        // this linq is considered laggy but the alternative is a big chunk of ugly code
        var wearEnum = Wears.WearEnum.Broken;
        foreach (var val in Enum.GetValues(typeof(Wears.WearEnum)))
        {
            if (wear < (int)val) break;
            wearEnum = (Wears.WearEnum)val;
        }

        if (Level < 100 && (int)wearEnum > (int)Wears.WearEnum.Perfect)
            wearEnum = Wears.WearEnum.Perfect;

        Wear = wearEnum;
    }
}