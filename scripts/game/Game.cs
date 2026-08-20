namespace MurderFloor;

public partial class Game : Node
{
    public enum DifficultyEnum
    {
        Easy = 0,
        Medium = 1,
        Challenging = 2, // ! find different name?
        Hard = 3,
        Extreme = 4,
        Ludicrous = 5,
    }
    public enum StateEnum
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
    public StateEnum GameState { get; private set; } = StateEnum.Stopped;
    [Export]
    public DifficultyEnum GameDifficulty { get; private set; } = DifficultyEnum.Easy;
    [Export]
    public ulong GameSeed { get; private set; } = 12345678;

    [Export]
    public int MaxRound { get; private set; } = 5; // 5?
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

    private int FuncMobRoundAmount => (int)(30f + Round * ((float)GameDifficulty + 1f * 0.33f));

    private List<MobSpawnArea> spawnAreas = [];
    private int lastSpawnAreaIndex = -1;
    private ulong lastWaveTime = 0ul;
    private RandomNumberGenerator rngSpawning = new();
    private RandomNumberGenerator rngLoot = new();

    private Node mobPoolNode;
    private Node lootNode;

    public override void _EnterTree()
    {
        Current = this;
        rngSpawning.Seed = GameSeed;
        rngLoot.Seed = GameSeed;
    }

    public override void _ExitTree()
    {
        mobPoolNode.Free();
        lootNode.Free();
    }

    public override void _Ready()
    {
        NetworkManager.Current.RpcId(1, "PlayerLoaded");
    }

    public override void _Process(double delta)
    {
        if (GameState == StateEnum.Round)
        {
            if (4000ul < Time.GetTicksMsec() - lastWaveTime && ActiveMobs < MaxActiveMobs)
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

        var root = GetTree().Root;
        mobPoolNode = new Node() { Name = "MobPool" };
        root.AddChild(mobPoolNode);
        lootNode = new Node() { Name = "Loot" };
        root.AddChild(lootNode);

        var mobScene = GD.Load<PackedScene>("res://scenes/pawn/mob/LiveMob.tscn");
        for (int i = 0; i < 200; i++)
        {
            var mob = mobScene.Instantiate<LiveMob>();
            mob.Name = "mob_" + i;
            mob.MobPoolId = i;
            mob.MobProcessOffset = MobPool.Count % 20;
            mobPoolNode.AddChild(mob);
            MobPool.Add(mob);
            mob.SetMultiplayerAuthority(1);
        }
        EmitSignal(SignalName.GameStart);

        NextRound();
    }

    public void MobDeath(DamageInfo damageInfo, int mobPoolId)
    {
        ActiveMobs--;
        RoundMobsLeft--;
        ProcessLoot(damageInfo, mobPoolId);

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

    private void ProcessLoot(DamageInfo damageInfo, int mobPoolId)
    {
        // ! dont drop loot until end of round or end of game?
        if (rngLoot.Randf() > 0.9f)
        {
            // ! level = map difficulty * difficulty + challenge or something
            var state = new Loot.LootState(GameSeed + rngLoot.Randi(), 0, DifficultyEnum.Hard, 0, false, false, 0f);
            var lootNode3d = Loot.LootState.MakeLootNode(state);
            lootNode.AddChild(lootNode3d);
            lootNode3d.GlobalPosition = damageInfo.HitPosition;
            ((RigidBody3D)lootNode3d.GetChild(0).GetChild(0)).LinearVelocity = new Vector3(rngLoot.RandfRange(-2f, 2f), 3f, rngLoot.RandfRange(-2f, 2f));

            var loot = new Loot.LootRarity(state);
            GD.Print($"{loot.Tier} ({(int)loot.Tier}),  {loot.Wear} ({(int)loot.Wear})");
        }
    }

    public async void TimerToNextRound()
    {
        GameState = StateEnum.Break;
        EmitSignal(SignalName.GameRoundEnd, Round);
        LastRoundEndTime = Time.GetTicksMsec();
        await Task.Delay(TimeMsBetweenRounds);

        NextRound();
    }

    public void NextRound()
    {
        Round++;
        GameState = StateEnum.Round;
        MaxActiveMobs = FuncMobMaxActive;
        RoundMobsLeft = FuncMobRoundAmount;
        EmitSignal(SignalName.GameRoundStart, Round);
        SpawnMobWave();
    }

    public void EndGame()
    {
        GameState = StateEnum.Stopped;

        SaveManager.CurrentSave.AddXp(100f);
        SaveManager.Save(SaveManager.CurrentSave);

        foreach (var mob in MobPool)
            mob?.Free();

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

        var spawnAreaIndex = rngSpawning.RandiRange(0, spawnAreas.Count - 1);

        if (spawnAreaIndex == lastSpawnAreaIndex)
            spawnAreaIndex = rngSpawning.RandiRange(0, spawnAreas.Count - 1);

        lastSpawnAreaIndex = spawnAreaIndex;

        var spawned = 0;
        var spawns = spawnAreas[spawnAreaIndex].GetSpawnVectorList(waveSize);
        for (int i = 0; i < MobPool.Count; i++)
        {
            if (MobPool[i].Active) continue;
            if (spawned >= waveSize) return;

            var allMob = ResourceManager.MobRegistry.GetAllResource();
            var mob = allMob.ElementAt(rngSpawning.RandiRange(0, allMob.Count - 1));

            MobPool[i].OnSpawn(spawns[spawned], mob.Value.FullId);
            ActiveMobs++;
            spawned++;
        }
    }
}