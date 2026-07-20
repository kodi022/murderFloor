namespace MurderFloor;

public static class Loot
{
    // this can be serialized, saved and networked
    public struct LootStateInfo
    {
        public ulong Seed { get; private set; }
        public int LootHashId { get; private set; } // necessary to save, if new loot is added, rng changes
        public int Level { get; private set; }
        public Game.GameDifficultyEnum Difficulty { get; private set; }
        public int MapHashId { get; private set; }
        public int ChallengeScaling { get; private set; }
        public float OverScaling { get; private set; }

        public LootStateInfo() { }

        public LootStateInfo(ulong seed, int level, Game.GameDifficultyEnum difficulty, int mapHashId, bool challenge1, bool challenge2, float overscaling)
        {
            Seed = seed;
            var lootCount = ResourceManager.LootRegistry.Count;
            var rng = new RandomNumberGenerator() { Seed = seed };
            var lootIndex = rng.RandiRange(0, lootCount - 1);
            LootHashId = ResourceManager.LootRegistry[lootIndex].HashId;

            Level = level;
            Difficulty = difficulty;
            MapHashId = mapHashId;
            ChallengeScaling = (challenge1 ? 1 : 0) + (challenge2 ? 2 : 0);
            OverScaling = overscaling;
        }

        public static Node3D MakeLootNode(LootStateInfo self)
        {
            var newLoot = GD.Load<PackedScene>("res://scenes/Loot.tscn").Instantiate<LiveLoot>();
            newLoot.Position = Vector3.Up * 0.1f;
            newLoot.StateInfo = self;
            var importYaw = 0f;
            var rigidBody = newLoot.FindChildren("RigidBody3D").First();
            var loot = ResourceManager.LootRegistry.FirstOrDefault(l => l.HashId == self.LootHashId, null);
            var meshScene = loot.MeshScene.Instantiate<Node3D>();
            meshScene.RotationDegrees = new Vector3(90, importYaw, 0);
            rigidBody.AddChild(meshScene);

            var rarityInfo = new LootRarityInfo(self);
            ((Sprite3D)rigidBody.GetChild(0)).Modulate = TierInfos[rarityInfo.Tier].Color;
            ((Sprite3D)rigidBody.GetChild(1)).Modulate = TierInfos[rarityInfo.Tier].Color;
            return newLoot;
        }

        public static string Serialize(LootStateInfo self)
        {
            var str = "";
            str += self.Seed + ",";
            str += self.LootHashId + ",";
            str += self.Level + ",";
            str += (int)self.Difficulty + ",";
            str += self.MapHashId + ",";
            str += self.ChallengeScaling + ",";
            str += self.OverScaling.ToString("#.#");
            return str;
        }

        public static LootStateInfo Deserialize(string state)
        {
            var strs = state.Split(',');
            return new LootStateInfo()
            {
                Seed = Convert.ToUInt64(strs[0]),
                LootHashId = strs[1].ToInt(),
                Level = strs[2].ToInt(),
                Difficulty = (Game.GameDifficultyEnum)strs[3].ToInt(),
                MapHashId = strs[4].ToInt(),
                ChallengeScaling = strs[5].ToInt(),
                OverScaling = strs[6].ToFloat()
            };
        }
    }

    public struct LootRarityInfo
    {
        public ulong Seed { get; private set; } = 0;
        public int Level { get; private set; } = 0;
        public TierEnum Tier { get; private set; }
        public WearEnum Wear { get; private set; }
        public double SuperScale { get; private set; }

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

            SuperScale = lootStateInfo.ChallengeScaling * 0.5f;
            SuperScale += lootStateInfo.OverScaling;
            // ! map affect Superscale

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

    public static Dictionary<WearEnum, WearInfo> WearInfos { get; private set; } = new()
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