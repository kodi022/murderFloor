namespace MurderFloor;

public static class SaveManager
{
    public static SaveData CurrentSave { get; private set; } = new SaveData();
    public static string SavePath => "user://saves/" + $"save{SaveIndex}.json";
    public static int SaveIndex { get; private set; } = 0;

    public static void Save(SaveData save)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(save);
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        file.StoreString(json);
    }

    public static SaveData Load(int index)
    {
        SaveIndex = Math.Abs(index) % 5; // keep positive
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        if (file is null) return new SaveData();
        var options = System.Text.Json.JsonSerializer.Deserialize<SaveData>(file.GetAsText());
        return options;
    }

    public class SaveData
    {
        public List<string> Loot { get; set; } = [];
    }
}