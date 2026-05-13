namespace MurderFloor;

public partial class Game : Node
{
    public enum GameStateType
    {
        Stopped,
        Break,
        Wave,
    }

    public static Game Current;

    public static GameStateType GameState { get; private set; } = GameStateType.Stopped;
    public static List<LiveMob> MobPool { get; private set; } = [];

    public static int MaxWave { get; private set; } = 5;
    public static int Wave { get; private set; } = 0;
    public static int ActiveMobs { get; private set; } = 0;

    public override void _Ready()
    {
        Current = this;
        NetworkManager.Current.RpcId(1, "PlayerLoaded");
    }

    public static void StartGame()
    {
        Wave = 1;
        GameState = GameStateType.Wave;
        var mobScene = GD.Load<PackedScene>("res://scenes/Mob.tscn");
        for (int i = 0; i < 100; i++)
        {
            var mob = mobScene.Instantiate();
            mob.Name = "mob_" + i;
            Current.GetTree().Root.AddChild(mob);
            MobPool.Add((LiveMob)mob);
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