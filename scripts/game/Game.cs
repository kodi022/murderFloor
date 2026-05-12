namespace MurderFloor;

public partial class Game : Node
{
    public static Game Current;
    public static List<Mob> MobPool { get; private set; } = [];

    public override void _Ready()
    {
        Current = this;
        NetworkManager.Current.RpcId(1, "PlayerLoaded");
    }

    public static void StartGame()
    {
        var mobScene = GD.Load<PackedScene>("res://scenes/Mob.tscn");
        for (int i = 0; i < 100; i++)
        {
            var mob = mobScene.Instantiate();
            mob.Name = "mob_" + i;
            Current.GetTree().Root.AddChild(mob);
            MobPool.Add((Mob)mob);
        }
        MobPool[35].Active = true;
    }

    public static void EndGame()
    {
        foreach (var mob in MobPool)
        {
            mob?.Free();
        }
        MobPool.Clear();
    }
}