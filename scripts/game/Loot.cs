namespace Shooter.Game;

public partial class Loot : Node3D
{
    public Resource.Loot.LootState StateInfo { get; set; }

    public Action OnUse;

    public override void _Ready()
    {
        GetChild<Usable>(0).UseAction = () =>
        {
            SaveManager.CurrentSave.Loot.Add(StateInfo.Serialize());
            OnUse?.Invoke();
            Free();
        };
    }
}