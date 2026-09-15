namespace Shooter.Game;

public partial class Loot : Node3D
{
    public Resource.Loot.LootState StateInfo { get; set; }

    public override void _Ready()
    {
        GetChild<Usable>(0).UseAction = () =>
        {
            SaveManager.CurrentSave.Loot.Add(StateInfo.Serialize());
            Free();
        };
    }
}