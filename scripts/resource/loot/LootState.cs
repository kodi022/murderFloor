namespace MurderFloor.Loot;

public struct LootState
{
    private const string SerializationDelimiter = ",";
    private const string CustomDataDelimiter = "_";
    private const string CustomDataKVPDelimiter = "=";

    // the saved data of Loot
    public ulong Seed { get; private set; }
    public int HashId { get; private set; }
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
        HashId = GetLootHashId(Seed);
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

    public static MFResource GetLootRef(LootState self)
    {
        GD.Print(self.HashId);
        return ResourceManager.LootRegistry.GetResourceRef(self.HashId);
    }

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
        var loot = ResourceManager.LootRegistry.GetResourceRef(self.HashId);
        var meshScene = loot.MeshScene.Instantiate<Node3D>();
        meshScene.RotationDegrees = new Vector3(90, importYaw, 0);
        rigidBody.AddChild(meshScene);

        var rarityInfo = new LootRarity(self);
        ((Sprite3D)rigidBody.GetChild(0)).Modulate = Tiers.TierList[rarityInfo.Tier].Color;
        ((Sprite3D)rigidBody.GetChild(1)).Modulate = Tiers.TierList[rarityInfo.Tier].Color;
        return newLoot;
    }

    public readonly bool HasCustomData(string key)
    {
        return CustomData.ContainsKey(key);
    }

    /// <summary>Tries to get custom data from LootState. Returns true if successful. Use HasCustomData(key) if value is not needed.</summary>
    public readonly bool GetCustomData(string key, out string value)
    {
        return CustomData.TryGetValue(key, out value);
    }

    /// <summary>Adds data to be saved to LootState. Highly recommended to keep key and value as short as possible.</summary>
    public readonly void AddCustomData(string key, string value)
    {
        if (CustomData.ContainsKey(key))
        {
            GD.PushError("LootState.AddCustomData already contains key");
            return;
        }
        if (key.Contains(CustomDataDelimiter))
        {
            GD.PushError($"LootState.AddCustomData key cannot contain \"{CustomDataDelimiter}\"");
            return;
        }
        if (key.Length < 2)
        {
            GD.PushError($"LootState.AddCustomData key cannot be 0 or 1 length");
            return;
        }

        CustomData.Add(key, value);
    }

    /// <summary> internal function reserved for base game. key 0 - 63 only. </summary>
    internal readonly void AddCustomData(int key, string value)
    {
        if (key > 63)
        {
            GD.PushError("LootState.AddCustomData key greater than 63");
            return;
        }

        var strKey = Compression.ArithmeticBase64[key].ToString();
        if (CustomData.ContainsKey(strKey))
        {
            GD.PushError("LootState.AddCustomData already contains key");
            return;
        }

        CustomData.Add(strKey, value);
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
        var str = self.Seed + SerializationDelimiter;
        str += self.HashId + SerializationDelimiter;
        str += self.Version.ToString() + SerializationDelimiter;
        str += self.Level + SerializationDelimiter;
        str += (int)self.Difficulty + SerializationDelimiter;
        str += self.MapHashId + SerializationDelimiter;
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
            Seed = Convert.ToUInt64(strs[0]),
            HashId = strs[1].ToInt(),
            Version = Version.FromString(strs[2]),
            Level = strs[3].ToInt(),
            Difficulty = (Game.DifficultyEnum)strs[4].ToInt(),
            MapHashId = strs[5].ToInt(),
            ChallengeScaling = strs[6].ToInt(),
            OverScaling = strs[7].ToFloat(),
            CustomData = [],
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
        Dictionary<string, string> vals = [];
        var kvps = customData.Split(CustomDataDelimiter);
        foreach (var kvp in kvps)
        {
            var split = kvp.Split(CustomDataKVPDelimiter);
            vals.Add(split[0], split[1]);
        }
        return vals;
    }

    public readonly override int GetHashCode() => GetStableHash();
    private readonly int GetStableHash()
    {
        unchecked
        {
            int hash = 13466917 + Seed.GetHashCode();
            hash = hash * 31 + HashId;
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
    public readonly bool Equals(LootState other) => GetHashCode() == other.GetHashCode();
    public readonly override bool Equals(object obj) => obj is LootState other && Equals(other);
    public static bool operator ==(LootState left, LootState right) => left.Equals(right);
    public static bool operator !=(LootState left, LootState right) => !left.Equals(right);

    // int
    public readonly bool Equals(int other) => GetHashCode() == other;
    public static bool operator ==(LootState left, int right) => left.Equals(right);
    public static bool operator !=(LootState left, int right) => !left.Equals(right);
    public static bool operator ==(int left, LootState right) => right.Equals(left);
    public static bool operator !=(int left, LootState right) => !right.Equals(left);
}