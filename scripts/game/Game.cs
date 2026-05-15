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

    public static List<LiveMob> MobPool { get; private set; } = [];

    [Export]
    public GameStateType GameState { get; private set; } = GameStateType.Stopped;

    [Export]
    public int MaxWave { get; private set; } = 5;
    [Export]
    public int Wave { get; private set; } = 0;
    [Export]
    public int MaxActiveMobs { get; private set; } = 25;
    [Export]
    public int WaveMobsLeft { get; private set; } = 0;
    [Export]
    public int ActiveMobs { get; private set; } = 0;

    public override void _Ready()
    {
        Current = this;
        NetworkManager.Current.RpcId(1, "PlayerLoaded");
    }

    [Rpc(CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
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
        Current.ActiveMobs--;
        Current.WaveMobsLeft--;

        if (Current.ActiveMobs < 10)
        {
            SpawnMobGroup();
        }

        if (Current.WaveMobsLeft <= 0)
        {
            if (Current.Wave == Current.MaxWave)
            {
                EndGame();
                return;
            }

            TimerToNextWave();
        }
    }

    public static async void TimerToNextWave()
    {
        Current.GameState = GameStateType.Break;

        await Task.Delay(5000);

        NextWave();
    }

    public static void NextWave()
    {
        Current.GameState = GameStateType.Wave;
        Current.Wave++;
        Current.WaveMobsLeft = Current.Wave * 10;
        SpawnMobGroup();
    }

    public static void EndGame()
    {
        Current.GameState = GameStateType.Stopped;
        foreach (var mob in MobPool)
        {
            mob?.Free();
        }
        MobPool.Clear();
        Current.Wave = 0;
        Current.WaveMobsLeft = 0;
        Current.ActiveMobs = 0;
    }

    private static void SpawnMobGroup()
    {
        for (int i = 0; i < Math.Min(Current.MaxActiveMobs - Current.ActiveMobs, Current.WaveMobsLeft - Current.ActiveMobs); i++)
        {
            if (MobPool[i].Active) continue;
            MobPool[i].OnSpawn(new Vector3(Random.Shared.NextSingle() * 5, 0, Random.Shared.NextSingle() * 5), 0);
            Current.ActiveMobs++;
        }
    }
}