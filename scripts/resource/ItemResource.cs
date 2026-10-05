namespace Shooter.Resource;

public partial class ItemResource : GameResource
{
    /// <summary>Gets stat or -1.0f if not found</summary>
    public virtual Variant GetStat(string statName, float mult)
    {
        return -1f;
    }

    /// <summary>Gets names of all stats or empty array if none</summary>
    public virtual string[] GetStatsEnum()
    {
        return [];
    }

    /// <summary>Gets sign of stat based on its enum</summary>
    public virtual int GetStatSign(string statName)
    {
        return -1;
    }
}