namespace MurderFloor;

public partial class Loot : MFResource
{
    [Export]
    public PackedScene MeshScene { get; private set; }

    // this can be serialized, saved and networked
    public struct LootStateInfo
    {
        public ulong Seed { get; private set; }
        public int Level { get; private set; }
        public Game.GameDifficultyEnum Difficulty { get; private set; }
        public string Map { get; private set; } // ! change type when implemented
        public string MapChallenge { get; private set; } // ! change type when implemented
        public double Overscaling { get; private set; }

        public LootStateInfo(ulong seed, int level, Game.GameDifficultyEnum difficulty, string map, string mapChallenge, double overscaling)
        {
            Seed = seed;
            Level = level;
            Difficulty = difficulty;
            Map = map;
            MapChallenge = mapChallenge;
            Overscaling = overscaling;
        }

        public Node3D MakeLootNode()
        {
            var newLoot = (Node3D)GD.Load<PackedScene>("res://scenes/Loot.tscn").Instantiate();
            var rarityInfo = new LootRarityInfo(this);
            ((Sprite3D)newLoot.GetChild(0).GetChild(0).GetChild(0)).Modulate = TierInfos[rarityInfo.Tier].Color;
            ((Sprite3D)newLoot.GetChild(0).GetChild(0).GetChild(1)).Modulate = TierInfos[rarityInfo.Tier].Color;
            return newLoot;
        }
    }

    public struct LootRarityInfo
    {
        public ulong Seed { get; private set; } = 0;
        public int Level { get; private set; } = 0;
        public TierEnum Tier { get; private set; }
        public WearEnum Wear { get; private set; }
        public double Superscale { get; private set; }

        // loot drop algorithm must depend on difficulty settings and player level
        // no high level loot on low difficulty
        public LootRarityInfo(LootStateInfo lootStateInfo)
        {
            Seed = lootStateInfo.Seed;
            Level = lootStateInfo.Level;

            var tierOffset = lootStateInfo.Difficulty switch
            {
                Game.GameDifficultyEnum.Easy => -2.0f,
                Game.GameDifficultyEnum.Medium => -1.4f,
                Game.GameDifficultyEnum.Challenging => -0.7f,
                Game.GameDifficultyEnum.Hard => 0f,
                Game.GameDifficultyEnum.Extreme => 0.8f,
                Game.GameDifficultyEnum.Ludicrous => 1.7f,
                _ => -2.0f,
            };
            var wearLevelOffset = ((int)lootStateInfo.Difficulty - 3) * 1.5f;

            Superscale = lootStateInfo.Overscaling * 0.1f;
            // ! map and mapchallenge affect Superscale

            var rng = new RandomNumberGenerator { Seed = Seed };
            GenerateTier(rng, tierOffset);
            GenerateWear(rng, wearLevelOffset);
        }

        private void GenerateTier(RandomNumberGenerator rng, float tierOffset)
        {
            float maxTicket = 0;
            Dictionary<TierEnum, float> tiers = new();

            void AddTierChance(TierEnum tier)
            {
                var val = Mathf.Pow((int)tier, 2.1f) + tierOffset;
                maxTicket += val;
                tiers.Add(tier, val);
            }

            if (Level < 50) AddTierChance(TierEnum.Common);
            AddTierChance(TierEnum.Uncommon);
            AddTierChance(TierEnum.Rare);
            AddTierChance(TierEnum.Epic);
            if (Level >= 50) AddTierChance(TierEnum.Exotic);
            if (Level >= 60) AddTierChance(TierEnum.Mythical);
            if (Level >= 70) AddTierChance(TierEnum.Legendary);
            if (Level >= 80) AddTierChance(TierEnum.Opalescent);

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

            if (Tier == TierEnum.Opalescent && Level >= 100)
            {
                if (tierOffset > 1.2f)
                {
                    if (rng.RandiRange(1, 8) == 1) Tier = TierEnum.Transcendent;
                }
                else
                {
                    if (rng.RandiRange(1, 12) == 1) Tier = TierEnum.Transcendent;
                }
            }
        }

        private void GenerateWear(RandomNumberGenerator rng, float wearLevelOffset)
        {
            var wear = Mathf.Max(0, rng.Randfn(Level + wearLevelOffset - 10, 6));
            // this linq is considered laggy but the alternative is a big chunk of ugly code
            var wearEnum = WearEnum.Broken;
            foreach (var val in Enum.GetValues(typeof(WearEnum)))
            {
                if (wear < (int)val) break;
                wearEnum = (WearEnum)val;
            }

            if (Level < 100 && (int)wearEnum > (int)WearEnum.Perfect)
                wearEnum = WearEnum.Perfect;

            Wear = wearEnum;
        }
    }
}