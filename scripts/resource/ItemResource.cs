namespace Shooter.Resource;

public partial class ItemResource : GameResource
{
    /// <summary>(String, Sign)</summary>
    public virtual Dictionary<string, StatInfo> ModifiableStats { get; set; } = [];

    public struct StatInfo(int sign, string postfix = "", string numFmt = "0.##", string rngSep = "-", string modPre = "x", string enblStt = "")
    {
        public int Sign = sign;
        public string Postfix = postfix;
        public string NumberFormat = numFmt;
        public string RangeSeparator = rngSep;
        public string ModifierPrefix = modPre;
        public string EnableState = enblStt;
    }

    /// <summary>Gets stat or -1.0f if not found</summary>
    public virtual Variant GetStat(string statName, float mult)
    {
        return -1f;
    }

    /// <summary>Gets StatInfo or default if not found</summary>
    public virtual StatInfo GetStatInfo(string statName)
    {
        if (!ModifiableStats.TryGetValue(statName, out StatInfo value))
        {
            GD.PushWarning($"{GetType().Name} does not have stat {statName}. Returning -1");
            return default;
        }

        return value;
    }

    /// <summary>Gets names of all stats or empty array if none</summary>
    public virtual string[] GetStatsNames()
    {
        List<string> a = [];
        foreach (var val in ModifiableStats) a.Add(val.Key);
        return [.. a];
    }
}