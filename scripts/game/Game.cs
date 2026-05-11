namespace MurderFloor;

public partial class Game : Node
{
    public static List<Mob> MobPool { get; private set; } = [];

    public void StartGame()
    {
        var mobScene = GD.Load<PackedScene>("res://scenes/Mob.tscn");
        for (int i = 0; i < 100; i++)
        {
            var mob = mobScene.Instantiate();
            GetTree().Root.AddChild(mob);
            MobPool.Add((Mob)mob);
        }
    }

    public void EndGame()
    {
        foreach (var mob in MobPool)
        {
            mob?.Free();
        }
    }
}