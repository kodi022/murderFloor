namespace Shooter;

using Resource.Loot;

public struct ToolConfig
{
    public LootState LootState { get; private set; }
    public List<LootState> AttachmentLootStates { get; private set; } = [];

    public Dictionary<PossibleStatEnum, float> SummedStatMultipliers { get; private set; } = [];

    public ToolConfig(LootState lootState, List<LootState> attachmentLootStates = null)
    {
        LootState = lootState;
        AttachmentLootStates = attachmentLootStates ?? [];
        SummedStatMultipliers = LootState.StatMultipliers;

        foreach (var att in AttachmentLootStates)
        {
            foreach (var stat in att.StatMultipliers)
            {
                if (!SummedStatMultipliers.TryAdd(stat.Key, stat.Value))
                    SummedStatMultipliers[stat.Key] += stat.Value;
            }
        }
    }

    /// <summary>Gets stat multiplier or 1.0f if not found</summary>
    public readonly float GetStatMultiplier(PossibleStatEnum stat)
    {
        if (SummedStatMultipliers.TryGetValue(stat, out float val))
            return val;
        else
            return 1f;
    }

    // use Layer2Delimiter first because these fit in arrays using Layer1Delimiter
    public readonly string Serialize()
    {
        var obj = new Stringified { State = LootState.Serialize(), Atts = [] };

        foreach (var att in AttachmentLootStates)
        {
            obj.Atts.Add(att.ToString());
        }

        return System.Text.Json.JsonSerializer.Serialize(obj, Utils.Defaults.JsonOptions);
    }

    public static ToolConfig Deserialize(string toolConfigSerialized)
    {
        var obj = System.Text.Json.JsonSerializer.Deserialize<Stringified>(toolConfigSerialized, Utils.Defaults.JsonOptions);
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