using System.Threading.Tasks;

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
    public static int MaxActiveMobs { get; private set; } = 25;
    public static int WaveMobsLeft { get; private set; } = 0;
    public static int ActiveMobs { get; private set; } = 0;

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
            LiveMob lMob = (LiveMob)mob;
            lMob.MobPoolId = i;
            lMob.Health = 100;
            lMob.MaxHealth = 100;
            Current.GetTree().Root.AddChild(mob);
            MobPool.Add((LiveMob)mob);
        }

        NextWave();
    }

    public static void MobDeath(int mobPoolId)
    {
        ActiveMobs--;
        WaveMobsLeft--;

        if (ActiveMobs < 10)
        {
            SpawnMobGroup();
        }

        if (WaveMobsLeft <= 0)
        {
            if (Wave == MaxWave)
            {
                EndGame();
                return;
            }

            TimerToNextWave();
        }
    }

    public static async void TimerToNextWave()
    {
        GameState = GameStateType.Break;

        await Task.Delay(5000);

        NextWave();
    }

    public static void NextWave()
    {
        GameState = GameStateType.Wave;
        Wave++;
        WaveMobsLeft = Wave * 10;
        SpawnMobGroup();
    }

    public static void EndGame()
    {
        GameState = GameStateType.Stopped;
        foreach (var mob in MobPool)
        {
            mob?.Free();
        }
        MobPool.Clear();
        Wave = 0;
        WaveMobsLeft = 0;
        ActiveMobs = 0;
    }

    private static void SpawnMobGroup()
    {
        for (int i = 0; i < Math.Min(MaxActiveMobs - ActiveMobs, WaveMobsLeft - ActiveMobs); i++)
        {
            if (MobPool[i].Active) continue;
            MobPool[i].OnSpawn(new Vector3(Random.Shared.NextSingle() * 5, 0, Random.Shared.NextSingle() * 5), 0);
            ActiveMobs++;
        }
    }
}