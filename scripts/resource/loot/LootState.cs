namespace MurderFloor.Loot;

public struct LootState
{
    // the saved data of Loot
    public ulong Seed { get; private set; }
    public int HashId { get; private set; }
    public Version Version { get; private set; }
    public int Level { get; private set; }
    public Game.DifficultyEnum Difficulty { get; private set; }
    public int MapHashId { get; private set; }
    public int ChallengeScaling { get; private set; }
    public float OverScaling { get; private set; }

    // generated values on creation
    public Dictionary<string, float> ModifiedStats { get; private set; } = [];

    public LootState()
    {
        GenerateStats();
    }

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
        GenerateStats();
    }

    private readonly void GenerateStats()
    {
        var loot = GetLootRef(this);
        ModifiedStats.Add("Damage", 1.2f);
    }

    public static MFResource GetLootRef(LootState self)
    {
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

    public static string Serialize(LootState self)
    {
        var str = "";
        str += self.Seed + ",";
        str += self.HashId + ",";
        str += self.Version.ToString() + ",";
        str += self.Level + ",";
        str += (int)self.Difficulty + ",";
        str += self.MapHashId + ",";
        str += self.ChallengeScaling + ",";
        str += self.OverScaling.ToString(".0");
        return str;
    }

    public static LootState Deserialize(string state)
    {
        var strs = state.Split(',');
        return new LootState()
        {
            Seed = Convert.ToUInt64(strs[0]),
            HashId = strs[1].ToInt(),
            Version = Version.FromString(strs[2]),
            Level = strs[3].ToInt(),
            Difficulty = (Game.DifficultyEnum)strs[4].ToInt(),
            MapHashId = strs[5].ToInt(),
            ChallengeScaling = strs[6].ToInt(),
            OverScaling = strs[7].ToFloat()
        };
    }

    public readonly int GetStableHash()
    {
        unchecked
        {
            int hash = 13466917 + Seed.GetHashCode();
            hash = hash * 31 + HashId;
            hash = hash * 31 + Level;
            hash = hash * 31 + (int)Difficulty;
            hash = hash * 31 + MapHashId;
            hash = hash * 31 + ChallengeScaling;
            hash = hash * 31 + OverScaling.GetHashCode();
            return hash;
        }
    }

    public readonly override int GetHashCode() => GetStableHash();
    public readonly bool Equals(LootState other) => GetStableHash() == other.GetStableHash();
    public readonly override bool Equals(object obj) => obj is LootState other && Equals(other);
    public static bool operator ==(LootState left, LootState right) => left.Equals(right);
    public static bool operator !=(LootState left, LootState right) => !left.Equals(right);
}