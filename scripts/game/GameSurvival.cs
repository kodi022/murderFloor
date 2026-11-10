namespace Shooter.Game;

using Global;

public partial class GameSurvival : Game
{
    public enum StateEnum
    {
        Prepare,
        Break,
        Wave,
        Ended,
        Debrief
    }

    public static List<Mob> MobPool { get; private set; } = [];

    [Signal]
    public delegate void GameWaveStartEventHandler(int round);
    [Signal]
    public delegate void GameWaveEndEventHandler(int round);

    [Export]
    public StateEnum GameState { get; private set; } = StateEnum.Prepare;

    // - terminology help
    // Game is multiple Waves
    // Wave is multiple Groups until a wave spawn count is reached
    // Group is a count of mobs spawned in one location simultaneously

    public int MobGroupSize => DifficultyConfig.GetGroupSize(Wave);
    public int MobMaxActive => DifficultyConfig.GetMaxActive(Wave);
    public int MobWaveAmount => DifficultyConfig.GetWaveAmount(Wave);
    public ulong TimeMsBetweenGroups => DifficultyConfig.GetTimeBetweenGroups(Wave);
    public ulong TimeMsBetweenWaves => DifficultyConfig.GetTimeBetweenWaves(Wave);

    [Export]
    public int MaxWave { get; private set; } = 8;
    [Export]
    public int Wave { get; private set; } = 0;
    [Export]
    public int WaveMobsLeft { get; private set; } = 0;
    [Export]
    public int ActiveMobs { get; private set; } = 0;

    [Export]
    public MobSpawnArea[] MobSpawnAreas { get; private set; }
    [Export]
    public ResupplySpawn[] ResupplySpawns { get; private set; }


    private ulong lastGroupTime = 0ul;

    public ulong LastWaveEndTime { get; private set; } = 0ul;

    private int lastSpawnAreaIndex = -1;
    private RandomNumberGenerator rngSpawning = new();
    private RandomNumberGenerator rngLoot = new();

    private Node lootNode;
    private Node mobPoolNode;

    private readonly List<Resource.Loot.LootState> allLoot = [];
    private readonly List<Resource.Loot.LootState> unpickedLoot = [];

    // huge lagspike when loaded
    private PackedScene mobPreload;

    public override void _EnterTree()
    {
        base._EnterTree();
        mobPreload = GD.Load<PackedScene>("res://scenes/pawn/mob/LiveMob.tscn");
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        mobPoolNode.Free();
        lootNode.Free();
    }

    public override async void _Ready()
    {
        base._Ready();
        DecalManager.Ready();
        ExitArea.BodyEntered += OnBodyEntered;
        ExitArea.BodyExited += OnBodyExited;
        GameManager.Singleton.MobOnDeath += MobDeath;
    }

    public override void _Process(double delta)
    {
        if (GameState == StateEnum.Wave)
        {
            if (TimeMsBetweenGroups < Time.GetTicksMsec() - lastGroupTime && ActiveMobs < MobMaxActive)
            {
                SpawnMobGroup();
            }
        }

        if (GameState == StateEnum.Ended && ExitTimer != 999f)
        {
            ExitTimer -= delta;

            if (ExitTimer <= 0f && !GameManager.ChangingScene)
            {
                Debrief();
            }
        }

        if (GameState == StateEnum.Debrief)
        {
            ExitTimer -= delta;
        }
    }

    // ! may need rpc attribute still
    public override void AllLoadedRpc()
    {
        base.AllLoadedRpc();
        rngSpawning.Seed = GameSeed;
        rngLoot.Seed = GameSeed;

        lootNode = new Node() { Name = "Loot" };
        GameManager.ClearOnLoad.AddChild(lootNode);

        mobPoolNode = new Node() { Name = "MobPool" };
        GameManager.ClearOnLoad.AddChild(mobPoolNode);
        for (int i = 0; i < 200; i++)
        {
            var mob = mobPreload.Instantiate<Mob>();
            mob.Name = "mob_" + i;
            mob.MobPoolId = i;
            mob.MobProcessOffset = MobPool.Count % 20;
            mobPoolNode.AddChild(mob);
            MobPool.Add(mob);
            mob.SetMultiplayerAuthority(1);
        }
    }

    // ! may need rpc attribute still
    public override void StartGameRpc()
    {
        base.StartGameRpc();
        NextWave();
    }

    public override bool BlockPlayerInput()
    {
        return GameState == StateEnum.Debrief;
    }

    public void MobDeath(Godot.Collections.Dictionary<string, Variant> damageInfoVariant, int mobPoolId)
    {
        ActiveMobs--;
        WaveMobsLeft--;
        ProcessLoot(DamageInfo.FromVariant(damageInfoVariant));

        if (WaveMobsLeft <= 0)
        {
            if (Wave == MaxWave)
            {
                EndGame();
                return;
            }

            SpawnResupply();
            TimerToNextWave();
            return;
        }
    }

    public void OnBodyEntered(Node3D body)
    {
        if (body is not Player player) return;

        ExitPlayers.Add(player);

        if (ExitPlayers.Count == Player.AllPlayers.Count)
            ExitTimer = Mathf.Min(ExitTimer, 5f);
        else if (ExitPlayers.Count == Player.AllPlayers.Count - 1)
            ExitTimer = Mathf.Min(ExitTimer, 20f);
        else if (ExitPlayers.Count == Player.AllPlayers.Count - 2)
            ExitTimer = Mathf.Min(ExitTimer, 30f);
        else if (ExitPlayers.Count > 0)
            ExitTimer = Mathf.Min(ExitTimer, 60f);
    }

    public void OnBodyExited(Node3D body)
    {
        if (body is not Player player) return;

        ExitPlayers.Remove(player);

        if (ExitPlayers.Count == 0)
            ExitTimer = 999f;
    }

    private void ProcessLoot(DamageInfo damageInfo)
    {
        if (rngLoot.Randf() > 0.9f) // 98? 96?
        {
            var map = Ui.MapSelect.SelectedMapNetworked;
            var level = (int)Mathf.Clamp(GetLevel() + rngLoot.Randfn(0f, 2f), 0, 100);
            var challengeBitmask = (DifficultyConfig.C1 ? 1 : 0) + (DifficultyConfig.C2 ? 2 : 0); // next would be ? 4 : 0
            var lootState = new Resource.Loot.LootState(GameSeed + rngLoot.Randi(), map.HashId, level, DifficultyConfig.Difficulty, challengeBitmask, DifficultyConfig.Overscaling);

            var lootNode3d = lootState.MakeLootNode();
            lootNode.AddChild(lootNode3d);
            lootNode3d.GlobalPosition = damageInfo.HitPosition;
            ((RigidBody3D)lootNode3d.GetChild(0).GetChild(0)).LinearVelocity = new Vector3(rngLoot.RandfRange(-2f, 2f), 3f, rngLoot.RandfRange(-2f, 2f));

            allLoot.Add(lootState);
            unpickedLoot.Add(lootState);
            lootNode3d.OnUse += () => { unpickedLoot.Remove(lootState); };
        }
    }

    private void SpawnResupply()
    {
        var index = rngSpawning.RandiRange(0, ResupplySpawns.Length - 1);
        ResupplySpawns[index].SpawnResupply();
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
        WaveMobsLeft = MobWaveAmount;
        EmitSignal(SignalName.GameWaveStart, Wave);
        SpawnMobGroup();
    }

    public async void EndGame()
    {
        GameState = StateEnum.Ended;

        // allow final processing on final mob death before deletion
        await Task.Delay(100);
        foreach (var mob in MobPool) mob?.Free();

        MobPool.Clear();
        Wave = 0;
        WaveMobsLeft = 0;
        ActiveMobs = 0;
    }

    public static int GetLevel()
    {
        var map = Ui.MapSelect.SelectedMapNetworked;
        var levelFloat = (int)DifficultyConfig.Difficulty * 15f + (5f * map.MapDifficultyScale);
        return (int)Mathf.Clamp(levelFloat, 0, 100);
    }

    private void SpawnMobGroup()
    {
        lastGroupTime = Time.GetTicksMsec();

        var groupSize = MobGroupSize;
        if (groupSize + ActiveMobs > MobMaxActive) groupSize = MobMaxActive - ActiveMobs;
        if (groupSize + ActiveMobs > WaveMobsLeft) groupSize = WaveMobsLeft - ActiveMobs;

        var spawnAreaIndex = rngSpawning.RandiRange(0, MobSpawnAreas.Length - 1);

        if (spawnAreaIndex == lastSpawnAreaIndex)
            spawnAreaIndex = rngSpawning.RandiRange(0, MobSpawnAreas.Length - 1);

        lastSpawnAreaIndex = spawnAreaIndex;

        var spawned = 0;
        var spawns = MobSpawnAreas[spawnAreaIndex].GetSpawnVectorList(groupSize);
        for (int i = 0; i < MobPool.Count; i++)
        {
            if (MobPool[i].Active) continue;
            if (spawned >= groupSize) return;

            var allMob = ResourceManager.MobRegistry.GetAllResource();
            var mobResource = allMob.ElementAt(rngSpawning.RandiRange(0, allMob.Count - 1));

            var mob = MobPool[i];
            mob.MobResourceFullId = mobResource.Value.FullId;
            mob.Level = GetLevel();
            mob.OnSpawn(spawns[spawned]);
            ActiveMobs++;
            spawned++;
        }
    }

    private async void Debrief()
    {
        ExitTimer = 15f;
        GameState = StateEnum.Debrief;
        Ui.HudDebrief.EarnedXp = 100f * ((int)DifficultyConfig.Difficulty + 1) * DifficultyConfig.MapDifficultyScale;
        Ui.HudDebrief.AllEarnedLoot = allLoot;
        Ui.HudDebrief.UnpickedEarnedLoot = unpickedLoot;
        Player.Self.OpenUI("res://scenes/ui/hud/HudDebrief.tscn");

        await Task.Delay(15000);

        GameManager.ChangingScene = true;
        if (NetworkManager.Singleton.IsMultiplayerAuthority())
            NetworkManager.Singleton.Rpc(NetworkManager.MethodName.LoadGameRpc, "res://scenes/map/lobby/Lobby.tscn", false);
    }
}