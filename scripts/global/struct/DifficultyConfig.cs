namespace Shooter;

public struct DifficultyConfig
{
    public Game.Game.DifficultyEnum Difficulty;
    public float Overscaling;
    public bool C1;
    public bool C2;
    public float MapDifficultyScale;

    public readonly int GetMaxActive(int wave)
    {
        var max = 20;
        max += wave * 4;
        max += (int)Difficulty * 8;
        return max;
    }

    public readonly int GetWaveAmount(int wave)
    {
        var amount = 20;
        var waveScale = (float)(Difficulty + 1) * 0.5f;
        amount += (int)(wave * waveScale);
        return amount;
    }

    public readonly int GetGroupSize(int wave)
    {
        var size = 5;
        size += (int)Difficulty * 2;
        return size;
    }

    public readonly ulong GetTimeBetweenGroups(int wave)
    {
        var ms = 10000ul;
        ms -= (ulong)wave * 400ul;
        ms -= (ulong)Difficulty * 600ul;
        return ms;
    }

    public readonly ulong GetTimeBetweenWaves(int wave)
    {
        var ms = 20000ul;
        ms -= (ulong)Difficulty * 1000ul;
        return ms;
    }

    public static DifficultyConfig Deserialize(string difficultyConfig)
    {
        var diff = new DifficultyConfig();
        var strs = difficultyConfig.Split(',');
        diff.Difficulty = (Game.Game.DifficultyEnum)strs[0].ToInt();
        diff.Overscaling = strs[1].ToFloat();
        diff.C1 = strs[2] == "1";
        diff.C2 = strs[3] == "1";
        diff.MapDifficultyScale = strs[4].ToFloat();
        return diff;
    }

    public readonly string Serialize()
    {
        var str = "";
        str += Difficulty + ',';
        str += Overscaling.ToString(".00") + ',';
        str += (C1 ? '1' : '0') + ',';
        str += (C2 ? '1' : '0') + ',';
        str += MapDifficultyScale.ToString(".00");
        return str;
    }
}