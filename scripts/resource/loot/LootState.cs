namespace Shooter.Resource.Loot;

[Flags]
public enum PossibleStats
{
    Damages,
    FalloffRanges,
    RPM,
    PelletCount,
    HoldingSpeed,
    ReloadDelayMs,
    MagSize,
    MagsReserve,
    InitialDegreeSpread,
    MaxDegreeSpread,
    SpreadRecoveryRate,
    SpreadIncreasePerShot,
    SlowWalkSpreadMult,
    FastWalkSpreadMult,
    AimSpreadMult,
    AimShiftRangeVertical,
    AimShiftRangeHorizontal,
}

/// LootState CustomData basegame mapping
/// attachment:
///     g = Gun LootState HashId
///     r = reticle image uid
///     c = reticle color

public struct LootState
{
    private const string Layer1Delimiter = ",";
    private const string Layer2Delimiter = "_";
    private const string Layer3Delimiter = "=";

    // the saved data of Loot
    public ulong Seed { get; private set; }
    public int ResourceHashId { get; private set; }
    public Version Version { get; private set; }
    public int Level { get; private set; }
    public Game.Game.DifficultyEnum Difficulty { get; private set; }
    public int MapHashId { get; private set; }
    public int OverScaling { get; private set; }
    private Dictionary<string, string> CustomData { get; set; } // needs to be property

    // generated values on creation
    public Dictionary<PossibleStats, float> ModifiedStats { get; private set; } = [];

    public LootState() { }

    /// <summary> Constructor only for newly generated loot </summary>
    public LootState(ulong seed, int level, Game.Game.DifficultyEnum difficulty, int mapHashId, float overscaling)
    {
        Seed = seed;
        ResourceHashId = GetLootHashId();
        Version = Global.GameManager.GameVersion;
        Level = level;
        Difficulty = difficulty;
        MapHashId = mapHashId;
        OverScaling = (int)(overscaling * 10);
        CustomData = [];
        GenerateStats();
    }

    private readonly void GenerateStats()
    {
        var loot = GetLootRef();
        if (loot is null) return;

        var rarity = new LootRarity(this);
        var rng = new RandomNumberGenerator { Seed = Seed };
        var tier = Tiers.TierInfos[rarity.Tier];
        var wear = Wears.WearInfos[rarity.Wear];

        if (loot is ToolFirearm firearm)
        {
            // cast speeds up iteration
            foreach (var val in (PossibleStats[])Enum.GetValues(typeof(PossibleStats)))
            {
                if (rng.Randf() < tier.StatChance)
                {
                    ModifiedStats.Add(val, tier.PowerScale * wear.PowerScale);
                }
            }
        }
    }

    public readonly GameResource GetLootRef()
    {
        if (ResourceHashId == 184465471) return null; // fistd

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

    public readonly Node3D MakeLootNode()
    {
        var newLoot = GD.Load<PackedScene>("res://scenes/Loot.tscn").Instantiate<Game.Loot>();
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
        str += Version.ToString() + Layer1Delimiter;
        str += Level + Layer1Delimiter;
        str += (int)Difficulty + Layer1Delimiter;
        str += Utils.Compression.IntToAB64(MapHashId) + Layer1Delimiter;
        str += OverScaling.ToString() + Layer1Delimiter;
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
            Version = Version.FromString(strs[2]),
            Level = strs[3].ToInt(),
            Difficulty = (Game.Game.DifficultyEnum)strs[4].ToInt(),
            MapHashId = Utils.Compression.AB64ToInt(strs[5]),
            OverScaling = strs[6].ToInt(),
            CustomData = DeserializeCustomData(strs[7]),
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
            hash = hash * 31 + Version.GetHashCode();
            hash = hash * 31 + Level;
            hash = hash * 31 + (int)Difficulty;
            hash = hash * 31 + MapHashId;
            hash = hash * 31 + OverScaling.GetHashCode();
            return hash;
        }
    }

    // LootState
    public readonly bool Equals(LootState other) => GetHashCode() == other.GetHashCode();
    public readonly override bool Equals(object obj) => obj is LootState other && Equals(other);
    public static bool operator ==(LootState left, LootState right) => left.Equals(right);
    public static bool operator !=(LootState left, LootState right) => !left.Equals(right);
}