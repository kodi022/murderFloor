namespace MurderFloor;

public partial class Game : Node
{
    public enum GameDifficultyEnum
    {
        Easy = 1,
        Medium = 2,
        Challenging = 3,
        Hard = 4,
        Extreme = 5,
        Ludicrous = 6,
    }
    public enum GameStateEnum
    {
        Stopped,
        Break,
        Round,
    }

    public static Game Current;
    public static List<LiveMob> MobPool { get; private set; } = [];

    [Signal]
    public delegate void GameRoundStartEventHandler(int round);
    [Signal]
    public delegate void GameRoundEndEventHandler(int round);
    [Signal]
    public delegate void GameWinEventHandler();
    [Signal]
    public delegate void GameStartEventHandler();

    [Export]
    public GameStateEnum GameState { get; private set; } = GameStateEnum.Stopped;
    [Export]
    public GameDifficultyEnum GameDifficulty { get; private set; } = GameDifficultyEnum.Easy;
    [Export]
    public ulong GameSeed { get; private set; } = (ulong)Random.Shared.NextInt64();

    [Export]
    public int MaxRound { get; private set; } = 5;
    [Export]
    public int Round { get; private set; } = 0;
    [Export]
    public int MaxActiveMobs { get; private set; } = 25;
    [Export]
    public int RoundMobsLeft { get; private set; } = 0;
    [Export]
    public int ActiveMobs { get; private set; } = 0;

    [Export]
    public int TimeMsBetweenRounds { get; private set; } = 20000;

    public ulong LastRoundEndTime { get; private set; } = 0ul;

    // vsc says these should be uppercase
    private int FuncMobRoundWaveSize => 5 + Round + (int)GameDifficulty * 2;

    private int FuncMobMaxActive => 30 + (Round * 3) + ((int)GameDifficulty * 10);
    //private int FuncMobMaxActive => 1;

    private int FuncMobRoundAmount => (int)(30f + Round * ((float)GameDifficulty + 1f * 0.33f));

    private List<MobSpawnArea> spawnAreas = [];
    private int lastSpawnAreaIndex = -1;
    private ulong lastWaveTime = 0ul;
    private RandomNumberGenerator rng = new();

    public override void _EnterTree()
    {
        Current = this;
        rng.Seed = GameSeed;
    }

    public override void _ExitTree()
    {
        foreach (var mob in MobPool) mob.QueueFree();
    }

    public override void _Ready()
    {
        NetworkManager.Current.RpcId(1, "PlayerLoaded");
    }

    public override void _Process(double delta)
    {
        if (GameState == GameStateEnum.Round)
        {
            if (20000ul < Time.GetTicksMsec() - lastWaveTime && ActiveMobs < MaxActiveMobs)
            {
                SpawnMobWave();
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void StartGame()
    {
        foreach (var child in GetChildren())
        {
            if (child is MobSpawnArea mob)
            {
                spawnAreas.Add(mob);
            }
        }

        var mobScene = GD.Load<PackedScene>("res://scenes/Mob.tscn");
        for (int i = 0; i < 200; i++)
        {
            var mob = mobScene.Instantiate();
            mob.Name = "mob_" + i;
            LiveMob lMob = (LiveMob)mob;
            lMob.MobPoolId = i;
            lMob.MobProcessOffset = MobPool.Count % 20;
            GetTree().Root.AddChild(mob);
            MobPool.Add((LiveMob)mob);
            mob.SetMultiplayerAuthority(1);
        }
        EmitSignal(SignalName.GameStart);

        NextRound();
    }

    public void MobDeath(int mobPoolId)
    {
        if (!MobPool[mobPoolId].Active) return;

        ActiveMobs--;
        RoundMobsLeft--;

        if (RoundMobsLeft <= 0)
        {
            if (Round == MaxRound)
            {
                EndGame();
                return;
            }

            TimerToNextRound();
            return;
        }

        if (ActiveMobs < 8)
        {
            SpawnMobWave();
        }
    }

    public async void TimerToNextRound()
    {
        GameState = GameStateEnum.Break;
        EmitSignal(SignalName.GameRoundEnd, Round);
        LastRoundEndTime = Time.GetTicksMsec();
        await Task.Delay(TimeMsBetweenRounds);

        NextRound();
    }

    public void NextRound()
    {
        Round++;
        GameState = GameStateEnum.Round;
        MaxActiveMobs = FuncMobMaxActive;
        RoundMobsLeft = FuncMobRoundAmount;
        EmitSignal(SignalName.GameRoundStart, Round);
        SpawnMobWave();
    }

    public void EndGame()
    {
        GameState = GameStateEnum.Stopped;
        foreach (var mob in MobPool)
        {
            mob?.Free();
        }
        MobPool.Clear();
        Round = 0;
        RoundMobsLeft = 0;
        ActiveMobs = 0;
    }

    private void SpawnMobWave()
    {
        lastWaveTime = Time.GetTicksMsec();

        var waveSize = FuncMobRoundWaveSize;
        if (waveSize + ActiveMobs > MaxActiveMobs) waveSize = MaxActiveMobs - ActiveMobs;
        if (waveSize + ActiveMobs > RoundMobsLeft) waveSize = RoundMobsLeft - ActiveMobs;

        var spawnAreaIndex = rng.RandiRange(0, spawnAreas.Count - 1);

        if (spawnAreaIndex == lastSpawnAreaIndex)
            spawnAreaIndex = rng.RandiRange(0, spawnAreas.Count - 1);

        lastSpawnAreaIndex = spawnAreaIndex;

        var spawned = 0;
        var spawns = spawnAreas[spawnAreaIndex].GetSpawnVectorList(waveSize);
        for (int i = 0; i < MobPool.Count; i++)
        {
            if (MobPool[i].Active) continue;
            if (spawned >= waveSize) return;

            MobPool[i].OnSpawn(spawns[spawned], 0);
            ActiveMobs++;
            spawned++;
        }
    }
}