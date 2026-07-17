namespace MurderFloor;

public partial class LiveLoot : Node3D
{
    public Loot.LootStateInfo StateInfo { get; set; }

    public override void _Ready()
    {
        GetChild<Usable>(0).UseAction = () =>
        {
            SaveManager.CurrentSave.Loot.Add(Loot.LootStateInfo.Serialize(StateInfo));
            GD.Print(Loot.LootStateInfo.Serialize(StateInfo));
            Free();
        };
    }
}