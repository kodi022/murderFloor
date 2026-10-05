namespace Shooter;

using Resource;
using Resource.Loot;

public struct ToolConfig
{
    public LootState LootState { get; private set; }
    public List<LootState> AttachmentLootStates { get; private set; } = [];

    public Dictionary<string, float> SummedStatMultipliers { get; private set; } = [];

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

    /// <summary>Gets stat or -1.0f if not found</summary>
    public readonly Variant GetStat(string propertyName)
    {
        return ((Tool)LootState.GetLootRef()).GetStat(propertyName, GetStatMultiplier(propertyName));
    }

    /// <summary>Gets stat multiplier or 1.0f if not found</summary>
    public readonly float GetStatMultiplier(string propertyName)
    {
        if (SummedStatMultipliers.TryGetValue(propertyName, out float val))
            return val;
        else
            return 1f;
    }

    /// <summary>Gets stat multiplier for tool only</summary>
    public readonly float GetStatToolMultipliers(string propertyName)
    {
        // GD.Print(SummedStatMultipliers);
        // if (SummedStatMultipliers.TryGetValue(propertyName, out float val))
        //     return val;
        // else
        //     return 1f;
        return 1f;
    }

    /// <summary>Gets stat multiplier for attachments combined</summary>
    public readonly float GetStatAttachmentMultipliers(string propertyName)
    {
        // GD.Print(SummedStatMultipliers);
        // if (SummedStatMultipliers.TryGetValue(propertyName, out float val))
        //     return val;
        // else
        //     return 1f;
        return 1f;
    }

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

        List<LootState> states = [];
        foreach (var att in obj.Atts)
        {
            states.Add(LootState.Deserialize(att));
        }

        return new ToolConfig(LootState.Deserialize(obj.State), states); ;
    }

    private struct Stringified
    {
        public string State { get; set; }
        public List<string> Atts { get; set; }
    }
}