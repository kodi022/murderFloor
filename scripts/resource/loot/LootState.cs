namespace Shooter.Resource.Loot;

/// LootState CustomData base mapping
/// attachment:
///     g = Gun LootState HashId
/// -- optic:
///     r = reticle image uid
///     c = reticle color

public struct LootState
{
    private const string Layer1Delimiter = ",";
    private const string Layer2Delimiter = "_";
    private const string Layer3Delimiter = "=";

    // the saved data of Loot
    public ulong Seed { get; private set; }
    public int MapHashId { get; private set; }
    public int ResourceHashId { get; private set; }
    public Version Version { get; private set; }
    public int Level { get; private set; }
    public Game.Game.DifficultyEnum Difficulty { get; private set; }
    public int ChallengeBitmask { get; private set; }
    public int Overscaling { get; private set; }

    // custom set data
    private Dictionary<string, string> CustomData { get; set; } // needs to be property

    // generated values on creation. string of property name and value
    public Dictionary<string, float> StatMultipliers { get; private set; } = [];
    public float MapDifficultyScale { get; private set; }
    public float Superscale { get; private set; }

    public LootState() { }

    /// <summary> Constructor only for newly generated loot </summary>
    public LootState(ulong seed, int mapHashId, int level, Game.Game.DifficultyEnum difficulty, int challengeBitmask, float overscaling)
    {
        Seed = seed;
        ResourceHashId = GetLootHashId();
        MapHashId = mapHashId;
        Version = Global.GameManager.GameVersion;
        Level = level;
        Difficulty = difficulty;
        ChallengeBitmask = challengeBitmask;
        Overscaling = (int)(overscaling * 100f);
        CustomData = [];
        GenerateStats();
    }

    private void GenerateStats()
    {
        MapDifficultyScale = ResourceManager.MapRegistry.GetResourceRef(MapHashId)?.MapDifficultyScale ?? 0.5f;
        Superscale = (1f + Mathf.Log(1f + (Overscaling / 100f))) * MapDifficultyScale;

        var loot = GetLootRef();
        if (loot is null) return;

        var itemResource = (ItemResource)loot;
        var toolStatsStringified = itemResource.GetStatsNames();

        var rarity = new LootRarity(this);
        var rng = new RandomNumberGenerator { Seed = Seed };
        var tier = Tiers.TierInfos[rarity.Tier];
        var wear = Wears.WearInfos[rarity.Wear];

        float ScaleForInt(int count, float value)
        {
            return Mathf.Floor(value * count) / count;
        }

        float MakeF(int level, float overScale, float rngMin, float rngMax)
        {
            var a = rng.RandfRange(rngMin, rngMax);
            var b = tier.PowerScale * wear.PowerScale;
            var c = 1 + level * 0.01f;
            var d = overScale * 0.01f;
            return a * b * c + d;
        }

        float MakeFn(int level, float overScale, float mean, float deviation)
        {
            var a = rng.Randfn(mean, deviation);
            var b = tier.PowerScale * wear.PowerScale;
            var c = 1 + level * 0.01f;
            var d = overScale * 0.01f;
            return a * b * c + d;
        }

        foreach (var stat in toolStatsStringified)
        {
            if (stat == "Damages" || stat == "Damage")
            {
                if (rng.Randf() < tier.StatChance * 4f)
                {
                    var add = MakeFn(Level, Overscaling, 0.1f, 0.05f);

                    if (add < 0.01f) continue;
                    if (itemResource.GetStatInfo(stat).Sign == 1)
                        StatMultipliers.Add(stat, 1f + add);
                    else
                        StatMultipliers.Add(stat, 1f - add);
                }
                continue;
            }

            // ! support rare chance of negative, less common for higher tiers
            if (rng.Randf() < tier.StatChance)
            {
                if (stat == "ManualFireDelayMs" && loot is ToolFirearm tf && tf.FireMode != ToolFirearm.FireModeEnum.Manual) continue;

                var add = stat switch
                {
                    "PelletCount" => ScaleForInt(((ToolFirearm)loot).PelletCount, MakeF(Level, Superscale, 0.1f, 0.2f)),
                    "MagSize" => ScaleForInt(((ToolFirearm)loot).MagsReserve, MakeF(Level, Superscale, 0.1f, 0.2f)),
                    _ => MakeF(Level, Superscale, 0.05f, 0.1f),
                };

                if (add < 0.01f) continue;
                if (itemResource.GetStatInfo(stat).Sign == 1)
                    StatMultipliers.Add(stat, 1f + add);
                else
                    StatMultipliers.Add(stat, 1f / (1f + add));
            }
        }
    }

    public readonly GameResource GetLootRef()
    {
        if (ResourceHashId == 184465471) return GD.Load<GameResource>("res://resources/tool/melee/fists.tres"); // fists

        var loot = ResourceManager.LootRegistry.GetResourceRef(ResourceHashId);
        if (loot is null) GD.PushWarning($"LootState.GetLootRef: GetResourceRef returned null. ({ResourceHashId})");
        return loot;
    }

    private readonly int GetLootHashId()
    {
        var rng = new RandomNumberGenerator { Seed = Seed };
        var lootCount = ResourceManager.LootRegistry.Count;
        var lootIndex = rng.RandiRange(0, lootCount - 1);
        return ResourceManager.LootRegistry.GetResourceAtIndex(lootIndex).HashId;
    }

    public readonly Game.Loot MakeLootNode()
    {
        var newLoot = GD.Load<PackedScene>("res://scenes/pickup/Loot.tscn").Instantiate<Game.Loot>();
        newLoot.Position = Vector3.Up * 0.1f;
        newLoot.StateInfo = this;
        var importYaw = 0f;
        var rigidBody = newLoot.FindChildren("RigidBody3D").First();
        var loot = ResourceManager.LootRegistry.GetResourceRef(ResourceHashId);
        var meshScene = loot.MeshScene.Instantiate<Node3D>();
        meshScene.RotationDegrees = new Vector3(90, importYaw, 0);
        rigidBody.AddChild(meshScene);

        var rarityInfo = new LootRarity(this);
        ((Sprite3D)rigidBody.GetChild(0)).Modulate = Tiers.TierInfos[rarityInfo.Tier].Color;
        ((Sprite3D)rigidBody.GetChild(1)).Modulate = Tiers.TierInfos[rarityInfo.Tier].Color;
        return newLoot;
    }

    public readonly bool HasCustomData(string key) => CustomData.ContainsKey(key);

    /// <summary>Tries to get custom data from LootState. Returns true if successful. Use HasCustomData(key) if value is not needed.</summary>
    public readonly bool GetCustomData(string key, out string value) => CustomData.TryGetValue(key, out value);

    /// <summary>Adds data to be saved to LootState. Highly recommended to keep key and value as short as possible.</summary>
    public readonly void AddCustomData(string key, string value)
    {
        if (CustomData.ContainsKey(key))
        {
            GD.PushError($"LootState.AddCustomData already contains key \"{key}\"");
            return;
        }
        if (!CustomDataArgIsValid(key))
        {
            GD.PushError($"LootState.AddCustomData key cannot contain: \"{Layer1Delimiter}\" \"{Layer2Delimiter}\" \"{Layer3Delimiter}\"");
            return;
        }
        if (!CustomDataArgIsValid(value))
        {
            GD.PushError($"LootState.AddCustomData value cannot contain: \"{Layer1Delimiter}\" \"{Layer2Delimiter}\" \"{Layer3Delimiter}\"");
            return;
        }
        if (key.Length < 2)
        {
            GD.PushError($"LootState.AddCustomData key cannot be 0 or 1 length");
            return;
        }

        CustomData.Add(key, value);
    }

    /// <summary> internal function reserved for base game. base64 single character only. </summary>
    internal readonly void AddCustomData(char key, string value)
    {
        if (!Utils.Compression.ArithmeticBase64.Contains(key))
        {
            GD.PushError("LootState.AddCustomData does not contain char key");
            return;
        }

        if (CustomData.ContainsKey(key.ToString()))
        {
            GD.PushError("LootState.AddCustomData already contains key");
            return;
        }

        CustomData.Add(key.ToString(), value);
    }

    /// <summary>Removes data saved to LootState. Returns true if successfully removed.</summary>
    public readonly bool RemoveCustomData(string key)
    {
        if (key.Contains(Layer2Delimiter))
        {
            GD.PushError($"LootState.RemoveCustomData key cannot contain \"{Layer2Delimiter}\"");
            return false;
        }

        return CustomData.Remove(key);
    }

    public override readonly string ToString() => Serialize();
    public readonly string Serialize()
    {
        var str = Utils.Compression.ULToAB64(Seed) + Layer1Delimiter;
        str += Utils.Compression.IntToAB64(ResourceHashId) + Layer1Delimiter;
        str += Utils.Compression.IntToAB64(MapHashId) + Layer1Delimiter;
        str += Version.ToString() + Layer1Delimiter;
        str += Level + Layer1Delimiter;
        str += (int)Difficulty + Layer1Delimiter;
        str += Utils.Compression.IntToAB64(ChallengeBitmask) + Layer1Delimiter;
        str += Utils.Compression.IntToAB64(Overscaling) + Layer1Delimiter;
        str += SerializeCustomData(CustomData);
        return str;
    }

    public static LootState Deserialize(string state)
    {
        var strs = state.Split(Layer1Delimiter);
        var ls = new LootState()
        {
            Seed = Utils.Compression.AB64ToUL(strs[0]),
            ResourceHashId = Utils.Compression.AB64ToInt(strs[1]),
            MapHashId = Utils.Compression.AB64ToInt(strs[2]),
            Version = Version.FromString(strs[3]),
            Level = strs[4].ToInt(),
            Difficulty = (Game.Game.DifficultyEnum)strs[5].ToInt(),
            ChallengeBitmask = Utils.Compression.AB64ToInt(strs[6]),
            Overscaling = Utils.Compression.AB64ToInt(strs[7]),
            CustomData = DeserializeCustomData(strs[8]),
        };
        ls.GenerateStats();
        return ls;
    }

    private static string SerializeCustomData(Dictionary<string, string> customData)
    {
        var str = "";
        foreach (var kvp in customData)
        {
            str += kvp.Key + Layer3Delimiter + kvp.Value + Layer2Delimiter;
        }
        return str;
    }

    private static Dictionary<string, string> DeserializeCustomData(string customData)
    {
        if (string.IsNullOrEmpty(customData)) return [];

        Dictionary<string, string> vals = [];
        var kvps = customData.Split(Layer2Delimiter);
        foreach (var kvp in kvps)
        {
            var split = kvp.Split(Layer3Delimiter);
            if (split.Length < 2) continue;
            vals.Add(split[0], split[1]);
        }
        return vals;
    }

    private static bool CustomDataArgIsValid(string arg)
    {
        return !(arg.Contains(Layer1Delimiter) || arg.Contains(Layer2Delimiter) || arg.Contains(Layer3Delimiter));
    }

    public readonly override int GetHashCode() => GetStableHash();
    private readonly int GetStableHash()
    {
        unchecked
        {
            int hash = 13466917 + Seed.GetHashCode();
            hash = hash * 31 + ResourceHashId;
            hash = hash * 31 + MapHashId;
            hash = hash * 31 + Version.GetHashCode();
            hash = hash * 31 + Level;
            hash = hash * 31 + (int)Difficulty;
            hash = hash * 31 + ChallengeBitmask;
            hash = hash * 31 + Overscaling;
            return hash;
        }
    }

    // LootState
    public readonly bool Equals(LootState other) => GetHashCode() == other.GetHashCode();
    public readonly override bool Equals(object obj) => obj is LootState other && Equals(other);
    public static bool operator ==(LootState left, LootState right) => left.Equals(right);
    public static bool operator !=(LootState left, LootState right) => !left.Equals(right);

    public static async Task<List<string>> DebugGenerateLoot(int count = 500000, int level = 80)
    {
        var allLootTier = new Dictionary<Game.Game.DifficultyEnum, Dictionary<Resource.Loot.Tiers.TierEnum, int>>();
        var allLootWear = new Dictionary<Game.Game.DifficultyEnum, Dictionary<Resource.Loot.Wears.WearEnum, int>>();

        void GenerateLoot(Game.Game.DifficultyEnum difficulty)
        {
            var tierCount = new Dictionary<Resource.Loot.Tiers.TierEnum, int>();
            var wearCount = new Dictionary<Resource.Loot.Wears.WearEnum, int>();
            for (int i = 0; i < count; i++)
            {
                var state = new Resource.Loot.LootState((ulong)Random.Shared.NextInt64(), 0, level, difficulty, 0, 0);
                var e = new Resource.Loot.LootRarity(state);
                if (!tierCount.TryAdd(e.Tier, 1))
                    tierCount[e.Tier] += 1;

                if (!wearCount.TryAdd(e.Wear, 1))
                    wearCount[e.Wear] += 1;
            }

            allLootTier.Add(difficulty, tierCount);
            allLootWear.Add(difficulty, wearCount);
        }

        var difficulties = Enum.GetValues<Game.Game.DifficultyEnum>();
        var tasks = difficulties.Select(difficulty =>
            Task.Run(() => GenerateLoot(difficulty))
        );

        await Task.WhenAll(tasks);

        List<string> diffStrings = [];
        diffStrings.Add($"CountPerDifficulty:{count}  Level:{level}  CSKnife:0.25");
        diffStrings.Add("");

        diffStrings.Add("Tiers");
        foreach (var difficulty in allLootTier.OrderBy(c => c.Key))
        {
            var diff = $"{difficulty.Key.ToString()[..4]}: ";

            var values = "";
            foreach (var tier in difficulty.Value.OrderByDescending(t => (int)t.Key))
            {
                var name = tier.Key.ToString()[..4];
                var perc = (float)tier.Value / (float)count * 100f;
                values += $"{name}:{perc:0.00}, ";
            }
            values = values[..^2];

            diffStrings.Add(diff + values);
        }
        diffStrings.Add("");

        diffStrings.Add("Wears");
        foreach (var difficulty in allLootWear.OrderBy(c => c.Key))
        {
            var diff = $"{difficulty.Key.ToString()[..4]}: ";

            var values = "";
            foreach (var wear in difficulty.Value.OrderBy(t => (int)t.Key))
            {
                var name = wear.Key.ToString();
                name = name.Length > 3 ? name[..4] : name;
                var perc = (float)wear.Value / (float)count * 100f;
                values += $"{name}:{perc:0.00}, ";
            }
            values = values[..^2];

            diffStrings.Add(diff + values);
        }

        return diffStrings;
    }
}