namespace MurderFloor.Loot;

////////////
/// LootState CustomData basegame mapping
/// attachment:
///     g = Gun LootState HashId
///     r = reticle image uid
///     c = reticle color
////////////

public struct LootState
{
    private const string SerializationDelimiter = ",";
    private const string CustomDataDelimiter = "_";
    private const string CustomDataKVPDelimiter = "=";

    public readonly int HashId => GetHashCode();

    // the saved data of Loot
    public ulong Seed { get; private set; }
    public int ResourceHashId { get; private set; }
    public Version Version { get; private set; }
    public int Level { get; private set; }
    public Game.DifficultyEnum Difficulty { get; private set; }
    public int MapHashId { get; private set; }
    public int ChallengeScaling { get; private set; }
    public float OverScaling { get; private set; }
    private Dictionary<string, string> CustomData { get; set; }

    // generated values on creation
    public Dictionary<string, float> ModifiedStats { get; private set; } = [];

    public LootState() { }

    /// <summary> Constructor only for newly generated loot </summary>
    public LootState(ulong seed, int level, Game.DifficultyEnum difficulty, int mapHashId, bool c1, bool c2, float difficultyMaxer)
    {
        Seed = seed;
        ResourceHashId = GetLootHashId(Seed);
        Version = Global.GameVersion;
        Level = level;
        Difficulty = difficulty;
        MapHashId = mapHashId;
        ChallengeScaling = (c1 ? 1 : 0) + (c2 ? 2 : 0);
        OverScaling = difficultyMaxer;
        CustomData = [];
        GenerateStats();
    }

    private readonly void GenerateStats()
    {
        var loot = GetLootRef(this);
        ModifiedStats.Add("Damage", 1.2f);
    }

    public static MFResource GetLootRef(LootState self) => ResourceManager.LootRegistry.GetResourceRef(self.ResourceHashId);

    private static int GetLootHashId(ulong seed)
    {
        var rng = new RandomNumberGenerator { Seed = seed };
        var lootCount = ResourceManager.LootRegistry.Count;
        var lootIndex = rng.RandiRange(0, lootCount - 1);
        return ResourceManager.LootRegistry.GetResourceAtIndex(lootIndex).HashId;
    }

    public static Node3D MakeLootNode(LootState self)
    {
        var newLoot = GD.Load<PackedScene>("res://scenes/Loot.tscn").Instantiate<LiveLoot>();
        newLoot.Position = Vector3.Up * 0.1f;
        newLoot.StateInfo = self;
        var importYaw = 0f;
        var rigidBody = newLoot.FindChildren("RigidBody3D").First();
        var loot = ResourceManager.LootRegistry.GetResourceRef(self.ResourceHashId);
        var meshScene = loot.MeshScene.Instantiate<Node3D>();
        meshScene.RotationDegrees = new Vector3(90, importYaw, 0);
        rigidBody.AddChild(meshScene);

        var rarityInfo = new LootRarity(self);
        ((Sprite3D)rigidBody.GetChild(0)).Modulate = Tiers.TierList[rarityInfo.Tier].Color;
        ((Sprite3D)rigidBody.GetChild(1)).Modulate = Tiers.TierList[rarityInfo.Tier].Color;
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
            GD.PushError($"LootState.AddCustomData key cannot contain: \"{SerializationDelimiter}\" \"{CustomDataDelimiter}\" \"{CustomDataKVPDelimiter}\"");
            return;
        }
        if (!CustomDataArgIsValid(key))
        {
            GD.PushError($"LootState.AddCustomData value cannot contain: \"{SerializationDelimiter}\" \"{CustomDataDelimiter}\" \"{CustomDataKVPDelimiter}\"");
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
        if (!Compression.ArithmeticBase64.Contains(key))
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
        if (key.Contains(CustomDataDelimiter))
        {
            GD.PushError($"LootState.RemoveCustomData key cannot contain \"{CustomDataDelimiter}\"");
            return false;
        }

        return CustomData.Remove(key);
    }

    public static string Serialize(LootState self)
    {
        var str = Compression.ULToAB64(self.Seed) + SerializationDelimiter;
        str += Compression.IntToAB64(self.ResourceHashId) + SerializationDelimiter;
        str += self.Version.ToString() + SerializationDelimiter;
        str += self.Level + SerializationDelimiter;
        str += (int)self.Difficulty + SerializationDelimiter;
        str += Compression.IntToAB64(self.MapHashId) + SerializationDelimiter;
        str += self.ChallengeScaling + SerializationDelimiter;
        str += self.OverScaling.ToString(".00") + SerializationDelimiter;
        str += SerializeCustomData(self.CustomData);
        return str;
    }

    public static LootState Deserialize(string state)
    {
        var strs = state.Split(SerializationDelimiter);
        var ls = new LootState()
        {
            Seed = Compression.AB64ToUL(strs[0]),
            ResourceHashId = Compression.AB64ToInt(strs[1]),
            Version = Version.FromString(strs[2]),
            Level = strs[3].ToInt(),
            Difficulty = (Game.DifficultyEnum)strs[4].ToInt(),
            MapHashId = Compression.AB64ToInt(strs[5]),
            ChallengeScaling = strs[6].ToInt(),
            OverScaling = strs[7].ToFloat(),
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
            str += kvp.Key + CustomDataKVPDelimiter + kvp.Value + CustomDataDelimiter;
        }
        return str;
    }

    private static Dictionary<string, string> DeserializeCustomData(string customData)
    {
        if (string.IsNullOrEmpty(customData)) return [];

        Dictionary<string, string> vals = [];
        var kvps = customData.Split(CustomDataDelimiter);
        foreach (var kvp in kvps)
        {
            var split = kvp.Split(CustomDataKVPDelimiter);
            vals.Add(split[0], split[1]);
        }
        return vals;
    }

    private static bool CustomDataArgIsValid(string arg)
    {
        return !(arg.Contains(SerializationDelimiter) || arg.Contains(CustomDataDelimiter) || arg.Contains(CustomDataKVPDelimiter));
    }

    public readonly override int GetHashCode() => GetStableHash();
    private readonly int GetStableHash()
    {
        unchecked
        {
            int hash = 13466917 + Seed.GetHashCode();
            hash = hash * 31 + ResourceHashId;
            hash = hash * 31 + Level;
            hash = hash * 31 + Version.GetHashCode();
            hash = hash * 31 + (int)Difficulty;
            hash = hash * 31 + MapHashId;
            hash = hash * 31 + ChallengeScaling;
            hash = hash * 31 + OverScaling.GetHashCode();
            return hash;
        }
    }

    // LootState
    public readonly bool Equals(LootState other) => HashId == other.HashId;
    public readonly override bool Equals(object obj) => obj is LootState other && Equals(other);
    public static bool operator ==(LootState left, LootState right) => left.Equals(right);
    public static bool operator !=(LootState left, LootState right) => !left.Equals(right);
}