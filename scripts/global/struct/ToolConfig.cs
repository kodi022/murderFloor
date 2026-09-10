namespace MurderFloor;

using Loot;

public struct ToolConfig
{
    public LootState LootState { get; set; }
    public List<LootState> AttachmentLootStates { get; set; } = [];
    // extra data

    public ToolConfig(LootState lootState, List<LootState> attachmentLootStates = null)
    {
        LootState = lootState;
        AttachmentLootStates = attachmentLootStates ?? [];
    }

    // use Layer2Delimiter first because these fit in arrays using Layer1Delimiter
    public readonly string Serialize()
    {
        var obj = new Stringified { State = LootState.Serialize(), Atts = [] };

        foreach (var att in AttachmentLootStates)
        {
            obj.Atts.Add(att.ToString());
        }

        return System.Text.Json.JsonSerializer.Serialize(obj, Global.JsonOptions);
    }

    public static ToolConfig Deserialize(string toolConfigSerialized)
    {
        var obj = System.Text.Json.JsonSerializer.Deserialize<Stringified>(toolConfigSerialized, Global.JsonOptions);
        var toolConfig = new ToolConfig { LootState = LootState.Deserialize(obj.State) };
        toolConfig.AttachmentLootStates ??= [];

        foreach (var att in obj.Atts)
        {
            toolConfig.AttachmentLootStates.Add(LootState.Deserialize(att));
        }

        return toolConfig;
    }

    private struct Stringified
    {
        public string State { get; set; }
        public List<string> Atts { get; set; }
    }
}