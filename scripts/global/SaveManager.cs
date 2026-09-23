namespace Shooter;

using Resource.Loot;

public static class SaveManager
{
    public static SaveData CurrentSave { get; private set; } = new SaveData();
    public static string SaveFolder => "user://saves";
    public static string SaveIndexPath => SaveFolder + "/saveindex.txt";
    public static string SavePath => SaveFolder + $"/save{SaveIndex}.json";
    public static int SaveIndex { get; private set; } = 0;

    public static void Save(SaveData save)
    {
        if (!DirAccess.DirExistsAbsolute(SaveFolder)) DirAccess.MakeDirAbsolute("user://saves");

        var json = System.Text.Json.JsonSerializer.Serialize(save, Utils.Defaults.JsonOptions);
        SaveSaveIndex();

        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        file.StoreString(json);
        GD.Print("Saved save " + SaveIndex);
    }

    public static SaveData Load(int index = -1)
    {
        if (index < 0)
            SaveIndex = LoadSaveIndex() % 5;
        else
            SaveIndex = Math.Abs(index) % 5;

        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        if (file is null) return new SaveData();

        var text = file.GetAsText();
        if (string.IsNullOrEmpty(text)) return new SaveData();

        var save = System.Text.Json.JsonSerializer.Deserialize<SaveData>(file.GetAsText(), Utils.Defaults.JsonOptions);
        GD.Print("Loaded save " + SaveIndex);
        return save;
    }

    public static void Apply(SaveData save)
    {
        CurrentSave = save;
    }

    private static void SaveSaveIndex()
    {
        using var file = FileAccess.Open(SaveIndexPath, FileAccess.ModeFlags.Write);
        file.StoreString(SaveIndex.ToString());
    }

    private static int LoadSaveIndex()
    {
        using var file = FileAccess.Open(SaveIndexPath, FileAccess.ModeFlags.Read);
        if (file is null) return 0;
        var index = System.Text.Json.JsonSerializer.Deserialize<int>(file.GetAsText(), Utils.Defaults.JsonOptions);
        return index;
    }

    public class SaveData
    {
        public int Level { get; set; } = 0;
        public float Xp { get; internal set; } = 0f;
        public double TotalXp { get; internal set; } = 0d;

        // both should only be directly used inside this class
        public List<string> Loot { get; set; } = []; // Serialized LootStates
        public List<int> Equipped { get; set; } = []; // HashCodes of LootStates

        public void AddXp(float amount)
        {
            Xp += amount;
            TotalXp += amount;
            while (Xp >= XpToNextLevel())
            {
                Xp -= XpToNextLevel();
                Level++;
            }
        }

        public float XpToNextLevel(int level = -1)
        {
            if (level == -1)
                return 200 + Mathf.Pow(Level + 1, 2.5f) - Level;
            else
                return 200 + Mathf.Pow(level + 1, 2.5f) - level;
        }

        /// <summary>Get loot by HashCode. Returns matched LootState or default value.</summary>
        public LootState GetLoot(int hashCode)
        {
            foreach (var val in Loot)
            {
                var lootState = LootState.Deserialize(val);
                if (lootState.GetHashCode() == hashCode)
                    return lootState;
            }

            return default;
        }

        /// <summary>Get loot by HashCode. Returns matched LootState or default value.</summary>
        public List<LootState> GetAttachmentsOnTool(LootState toolLootState)
        {
            List<LootState> atts = [];
            var hash = toolLootState.GetHashCode();
            foreach (var val in Loot)
            {
                var lootState = LootState.Deserialize(val);
                if (lootState.GetCustomData("g", out string id))
                {
                    if (hash == Utils.Compression.AB64ToInt(id))
                        atts.Add(lootState);
                }
            }

            return atts;
        }

        /// <summary>Useful for setting custom data. Returns false if not found.</summary>
        public bool ReplaceLoot(LootState lootState)
        {
            foreach (var val in Loot)
            {
                var valLootState = LootState.Deserialize(val);
                if (valLootState.GetHashCode() == lootState.GetHashCode())
                {
                    var index = Loot.IndexOf(val);
                    Loot[index] = lootState.Serialize();
                    return true;
                }
            }

            return false;
        }

        public List<LootState> GetAllLoot()
        {
            List<LootState> all = [];
            foreach (var val in Loot) all.Add(LootState.Deserialize(val));
            return all;
        }

        public List<LootState> GetAllLootOfType<T>() where T : Resource.GameResource
        {
            List<LootState> type = [];
            foreach (var val in Loot)
            {
                var lootState = LootState.Deserialize(val);
                if (lootState.GetLootRef() is T)
                    type.Add(lootState);
            }
            return type;
        }

        public List<LootState> GetEquippedLoot()
        {
            List<LootState> equippedLoot = [];
            List<int> notFound = [];
            foreach (var val in Equipped)
            {
                bool found = false;
                foreach (var loot in Loot)
                {
                    var lootState = LootState.Deserialize(loot);
                    if (lootState.GetHashCode() == val)
                    {
                        equippedLoot.Add(lootState);
                        found = true;
                        break;
                    }
                }

                if (!found) notFound.Add(val);
            }

            if (notFound.Count > 0)
            {
                foreach (var l in notFound)
                {
                    Equipped.Remove(l);
                }
                SaveManager.Save(this);
            }

            return equippedLoot;
        }
    }
}