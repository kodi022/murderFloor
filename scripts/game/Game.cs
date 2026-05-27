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

    public List<MobSpawnArea> SpawnAreas { get; private set; } = [];

    private ulong lastWaveTime = 0ul;
    private RandomNumberGenerator rng = new();

    public override void _EnterTree()
    {
        Current = this;
        rng.Seed = GameSeed;

        foreach (var child in GetChildren())
        {
            if (child is MobSpawnArea mob)
            {
                SpawnAreas.Add(mob);
            }
        }
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

    [Rpc(mode: MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void StartGame()
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
            GetTree().Root.AddChild(mob);
            MobPool.Add((LiveMob)mob);
            mob.SetMultiplayerAuthority(1);
        }

        MaxActiveMobs = 40 + ((int)GameDifficulty * 10);

        NextRound();
    }

    public void MobDeath(int mobPoolId)
    {
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

        await Task.Delay(5000);

        NextRound();
    }

    public void NextRound()
    {
        GameState = GameStateEnum.Round;
        Round++;
        RoundMobsLeft = (int)(30f + Round * ((float)GameDifficulty + 1f * 0.33f));
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

        var waveSize = 5 + (int)GameDifficulty * 2;
        if (waveSize + ActiveMobs > MaxActiveMobs) waveSize = MaxActiveMobs - ActiveMobs;
        if (waveSize + ActiveMobs > RoundMobsLeft) waveSize = RoundMobsLeft - ActiveMobs;

        var spawned = 0;
        var spawnAreaIndex = rng.RandiRange(0, SpawnAreas.Count);
        var spawns = SpawnAreas[(int)spawnAreaIndex].GetSpawnVectorList(waveSize);

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