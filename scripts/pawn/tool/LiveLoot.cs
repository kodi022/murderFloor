namespace MurderFloor;

using Loot;

public partial class LiveLoot : Node3D
{
    public LootState StateInfo { get; set; }

    public override void _Ready()
    {
        GetChild<Usable>(0).UseAction = () =>
        {
            SaveManager.CurrentSave.Loot.Add(LootState.Serialize(StateInfo));
            Free();
        };
    }
}