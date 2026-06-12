namespace MurderFloor;

public partial class RandomLoot : MFResource
{
    // ! loot drop algorithm must depend on difficulty settings and player level
    // ! no high level loot on low difficulty

    // based on level
    // any at or under level is available
    // reduce chance of lower at higher level
    enum RandomLootQualityEnum
    {
        Broken = 0,
        Tarnished = 3,      // 3
        Tattered = 6,       // 3
        Worn = 10,          // 4
        Used = 14,          // 4
        Decent = 19,        // 5
        Average = 25,       // 5
        Fine = 31,          // 6
        Clean = 37,         // 6
        Spotless = 43,      // 6
        Polished = 49,      // 7
        Shiny = 56,         // 7
        Mint = 63,          // 7
        New = 70,           // 7
        Excellent = 77,     // 7
        Pristine = 84,      // 7
        Flawless = 92,      // 8
        Perfect = 100,      // 8
    }

    // based on randomness
    // stop common for above level 40 items
    // stop uncommon for above level 70 items
    enum RandomLootTierEnum
    {
        Common = 1,
        Uncommon = 2,
        Rare = 6,
        Incredible = 18,
        Mystical = 54,
        Legendary = 162,
        Opalescent = 486,
        Transcendent = 1458,
        Bloodied = 0,          // special items // ! wip name
    }

    [Export]
    public PackedScene MeshScene { get; private set; }
}