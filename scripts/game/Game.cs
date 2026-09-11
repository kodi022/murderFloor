namespace MurderFloor;

// - terminology help
// Game is multiple Waves
// Wave is multiple Groups until a wave spawn count is reached
// Group is a count of mobs spawned in one location simultaneously

public partial class Game : Node
{
    public enum DifficultyEnum
    {
        Easy = 0,
        Medium = 1,
        Challenging = 2,
        Hard = 3,
        Extreme = 4,
        Ludicrous = 5,
    }
    public enum StateEnum
    {
        Stopped,
        Break,
        Wave,
    }

    public static Game Current { get; private set; }
    public static List<LiveMob> MobPool { get; private set; } = [];

    [Signal]
    public delegate void GameWaveStartEventHandler(int round);
    [Signal]
    public delegate void GameWaveEndEventHandler(int round);
    [Signal]
    public delegate void GameWinEventHandler();
    [Signal]
    public delegate void GameStartEventHandler();

    [Export]
    public StateEnum GameState { get; private set; } = StateEnum.Stopped;
    [Export]
    public ulong GameSeed { get; private set; } = 12345678;

    public static DifficultyConfig DifficultyConfig { get; set; }

    public int MobGroupSize => DifficultyConfig.GetGroupSize(Wave);
    public int MobMaxActive => DifficultyConfig.GetMaxActive(Wave);
    public int MobWaveAmount => DifficultyConfig.GetWaveAmount(Wave);
    public ulong TimeMsBetweenGroups => DifficultyConfig.GetTimeBetweenGroups(Wave);
    public ulong TimeMsBetweenWaves => DifficultyConfig.GetTimeBetweenWaves(Wave);

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

    private ulong lastGroupTime = 0ul;

    public ulong LastWaveEndTime { get; private set; } = 0ul;

    private List<MobSpawnArea> spawnAreas = [];
    private int lastSpawnAreaIndex = -1;
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
        NetworkManager.Singleton.RpcId(1, "PlayerLoaded");
    }

    public override void _Process(double delta)
    {
        if (GameState == StateEnum.Wave)
        {
            if (TimeMsBetweenGroups < Time.GetTicksMsec() - lastGroupTime && ActiveMobs < MaxActiveMobs)
            {
                SpawnMobGroup();
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

        mobPoolNode = new Node() { Name = "MobPool" };
        Global.ClearOnLoad.AddChild(mobPoolNode);
        lootNode = new Node() { Name = "Loot" };
        Global.ClearOnLoad.AddChild(lootNode);

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

        NextWave();
    }

    public void MobDeath(DamageInfo damageInfo, int mobPoolId)
    {
        ActiveMobs--;
        WaveMobsLeft--;
        ProcessLoot(damageInfo, mobPoolId);

        if (WaveMobsLeft <= 0)
        {
            if (Wave == MaxWave)
            {
                EndGame();
                return;
            }

            TimerToNextWave();
            return;
        }
    }

    private void ProcessLoot(DamageInfo damageInfo, int mobPoolId)
    {
        // ! dont drop loot until end of round or end of game?
        if (rngLoot.Randf() > 0.9f)
        {
            // ! level = map difficulty * difficulty + challenge or something
            var lootState = new Loot.LootState(GameSeed + rngLoot.Randi(), 0, DifficultyEnum.Hard, 0, 0);
            var lootNode3d = lootState.MakeLootNode();
            lootNode.AddChild(lootNode3d);
            lootNode3d.GlobalPosition = damageInfo.HitPosition;
            ((RigidBody3D)lootNode3d.GetChild(0).GetChild(0)).LinearVelocity = new Vector3(rngLoot.RandfRange(-2f, 2f), 3f, rngLoot.RandfRange(-2f, 2f));

            var loot = new Loot.LootRarity(lootState);
            GD.Print($"{loot.Tier} ({(int)loot.Tier}),  {loot.Wear} ({(int)loot.Wear})");
        }
    }

    public async void TimerToNextWave()
    {
        GameState = StateEnum.Break;
        EmitSignal(SignalName.GameWaveEnd, Wave);
        LastWaveEndTime = Time.GetTicksMsec();
        await Task.Delay((int)TimeMsBetweenWaves);

        NextWave();
    }

    public void NextWave()
    {
        Wave++;
        GameState = StateEnum.Wave;
        MaxActiveMobs = MobMaxActive;
        WaveMobsLeft = MobWaveAmount;
        EmitSignal(SignalName.GameWaveStart, Wave);
        SpawnMobGroup();
    }

    public void EndGame()
    {
        GameState = StateEnum.Stopped;

        SaveManager.CurrentSave.AddXp(100f * (int)DifficultyConfig.Difficulty);
        SaveManager.Save(SaveManager.CurrentSave);

        foreach (var mob in MobPool)
            mob?.Free();

        MobPool.Clear();
        Wave = 0;
        WaveMobsLeft = 0;
        ActiveMobs = 0;
    }

    private void SpawnMobGroup()
    {
        lastGroupTime = Time.GetTicksMsec();

        var groupSize = MobGroupSize;
        if (groupSize + ActiveMobs > MaxActiveMobs) groupSize = MaxActiveMobs - ActiveMobs;
        if (groupSize + ActiveMobs > WaveMobsLeft) groupSize = WaveMobsLeft - ActiveMobs;

        var spawnAreaIndex = rngSpawning.RandiRange(0, spawnAreas.Count - 1);

        if (spawnAreaIndex == lastSpawnAreaIndex)
            spawnAreaIndex = rngSpawning.RandiRange(0, spawnAreas.Count - 1);

        lastSpawnAreaIndex = spawnAreaIndex;

        var spawned = 0;
        var spawns = spawnAreas[spawnAreaIndex].GetSpawnVectorList(groupSize);
        for (int i = 0; i < MobPool.Count; i++)
        {
            if (MobPool[i].Active) continue;
            if (spawned >= groupSize) return;

            var allMob = ResourceManager.MobRegistry.GetAllResource();
            var mobResource = allMob.ElementAt(rngSpawning.RandiRange(0, allMob.Count - 1));

            var mob = MobPool[i];
            mob.MobResourceFullId = mobResource.Value.FullId;
            mob.OnSpawn(spawns[spawned]);
            ActiveMobs++;
            spawned++;
        }
    }
}