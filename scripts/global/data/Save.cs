namespace MurderFloor;

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

        var json = System.Text.Json.JsonSerializer.Serialize(save);
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
        var save = System.Text.Json.JsonSerializer.Deserialize<SaveData>(file.GetAsText());
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
        var index = System.Text.Json.JsonSerializer.Deserialize<int>(file.GetAsText());
        return index;
    }

    public class SaveData
    {
        public int Level { get; set; } = 0;
        public int Xp { get; set; } = 0;
        public List<string> Loot { get; set; } = [];
        public List<string> Beans { get; set; } = [];
    }
}