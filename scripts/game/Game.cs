namespace Shooter.Game;

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
        Prepare,
        Break,
        Wave,
        Ended,
        Debrief
    }

    public static Game Current { get; private set; }

    public static List<Mob> MobPool { get; private set; } = [];

    [Signal]
    public delegate void GameWaveStartEventHandler(int round);
    [Signal]
    public delegate void GameWaveEndEventHandler(int round);
    [Signal]
    public delegate void GameWinEventHandler();
    [Signal]
    public delegate void GameStartEventHandler();

    [Export]
    public StateEnum GameState { get; private set; } = StateEnum.Prepare;
    [Export]
    public ulong GameSeed { get; private set; } = 12345678;

    public static DifficultyConfig DifficultyConfig => Ui.MapSelect.DifficultyConfigNetworked;

    public int MobGroupSize => DifficultyConfig.GetGroupSize(Wave);
    public int MobMaxActive => DifficultyConfig.GetMaxActive(Wave);
    public int MobWaveAmount => DifficultyConfig.GetWaveAmount(Wave);
    public ulong TimeMsBetweenGroups => DifficultyConfig.GetTimeBetweenGroups(Wave);
    public ulong TimeMsBetweenWaves => DifficultyConfig.GetTimeBetweenWaves(Wave);

    [Export]
    public int MaxWave { get; private set; } = 1;
    [Export]
    public int Wave { get; private set; } = 0;
    [Export]
    public int MaxActiveMobs { get; private set; } = 25;
    [Export]
    public int WaveMobsLeft { get; private set; } = 0;
    [Export]
    public int ActiveMobs { get; private set; } = 0;

    [Export]
    public Area3D ExitArea { get; private set; }

    public List<Player> ExitPlayers { get; private set; } = [];
    public double ExitTimer { get; private set; } = 999f;
    private bool startedLoading = false;

    private ulong lastGroupTime = 0ul;

    public ulong LastWaveEndTime { get; private set; } = 0ul;

    private readonly List<MobSpawnArea> spawnAreas = [];
    private int lastSpawnAreaIndex = -1;
    private RandomNumberGenerator rngSpawning = new();
    private RandomNumberGenerator rngLoot = new();

    private Node lootNode;
    private Node mobPoolNode;

    private readonly List<Resource.Loot.LootState> allLoot = [];
    private readonly List<Resource.Loot.LootState> unpickedLoot = [];

    // huge lagspike when loaded
    private PackedScene ammoBoxPreload;

    public override void _EnterTree()
    {
        Current = this;
        ammoBoxPreload = GD.Load<PackedScene>("res://scenes/pickup/Ammobox.tscn");
    }

    public override void _ExitTree()
    {
        mobPoolNode.Free();
        lootNode.Free();
    }

    public override void _Ready()
    {
        Global.NetworkManager.Singleton.RpcId(1, Global.NetworkManager.MethodName.PlayerLoadedRpc, Multiplayer.MultiplayerPeer.GetUniqueId());
        ExitArea.BodyEntered += OnBodyEntered;
        ExitArea.BodyExited += OnBodyExited;
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

        if (GameState == StateEnum.Ended && ExitTimer != 999f)
        {
            ExitTimer -= delta;

            if (ExitTimer <= 0f && !startedLoading)
            {
                Debrief();
            }
        }

        if (GameState == StateEnum.Debrief)
        {
            ExitTimer -= delta;
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void AllLoadedRpc()
    {
        GameSeed = Ui.MapSelect.SelectedSeedNetworked;
        rngSpawning.Seed = GameSeed;
        rngLoot.Seed = GameSeed;

        foreach (var child in GetChildren())
        {
            if (child is MobSpawnArea a)
            {
                spawnAreas.Add(a);
            }
        }

        lootNode = new Node() { Name = "Loot" };
        Global.GameManager.ClearOnLoad.AddChild(lootNode);

        mobPoolNode = new Node() { Name = "MobPool" };
        Global.GameManager.ClearOnLoad.AddChild(mobPoolNode);
        var mobScene = GD.Load<PackedScene>("res://scenes/pawn/mob/LiveMob.tscn");
        for (int i = 0; i < 200; i++)
        {
            var mob = mobScene.Instantiate<Mob>();
            mob.Name = "mob_" + i;
            mob.MobPoolId = i;
            mob.MobProcessOffset = MobPool.Count % 20;
            mobPoolNode.AddChild(mob);
            MobPool.Add(mob);
            mob.SetMultiplayerAuthority(1);
        }

        Global.DecalManager.Ready();
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void StartGameRpc()
    {
        EmitSignal(SignalName.GameStart);
        NextWave();
    }

    public void MobDeath(DamageInfo damageInfo, int mobPoolId)
    {
        ActiveMobs--;
        WaveMobsLeft--;
        ProcessLoot(damageInfo);

        if (WaveMobsLeft <= 0)
        {
            if (Wave == MaxWave)
            {
                EndGame();
                return;
            }

            SpawnAmmo(damageInfo);
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
        else if (ExitPlayers.Count == 1)
            ExitTimer = Mathf.Min(ExitTimer, 60f);
        else if (ExitPlayers.Count == 2)
            ExitTimer = Mathf.Min(ExitTimer, 30f);
        else if (ExitPlayers.Count > 2)
            ExitTimer = Mathf.Min(ExitTimer, 20f);
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
        if (rngLoot.Randf() > 0.98f)
        {
            // mapdifficulty > 1.33 on Ludicrous allows for easy 100s
            var map = Ui.MapSelect.SelectedMapNetworked;
            var levelFloat = (int)DifficultyConfig.Difficulty * 15f * map.MapDifficultyScale * rngLoot.Randfn(1, 0.05f);
            var level = (int)Mathf.Clamp(levelFloat, 0, 100);
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

    private void SpawnAmmo(DamageInfo damageInfo)
    {
        var ammobox = (Node3D)ammoBoxPreload.Instantiate();
        Global.GameManager.ClearOnLoad.AddChild(ammobox);
        ammobox.GlobalPosition = damageInfo.HitPosition;
        ((RigidBody3D)ammobox.GetChild(0).GetChild(0)).LinearVelocity = new Vector3(rngLoot.RandfRange(-2f, 2f), 3f, rngLoot.RandfRange(-2f, 2f));
        ((RigidBody3D)ammobox.GetChild(0).GetChild(0)).AngularVelocity = Vector3.One * rngLoot.RandfRange(-2f, 2f);
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

    private async void Debrief()
    {
        ExitTimer = 15f;
        GameState = StateEnum.Debrief;
        Ui.HudDebrief.EarnedXp = 100f * ((int)DifficultyConfig.Difficulty + 1) * DifficultyConfig.MapDifficultyScale;
        Ui.HudDebrief.AllEarnedLoot = allLoot;
        Ui.HudDebrief.UnpickedEarnedLoot = unpickedLoot;
        Player.Self.OpenUI("res://scenes/ui/hud/HudDebrief.tscn");

        await Task.Delay(15000);

        startedLoading = true;
        if (Global.NetworkManager.Singleton.IsMultiplayerAuthority())
            Global.NetworkManager.Singleton.Rpc(Global.NetworkManager.MethodName.LoadGameRpc, "res://scenes/map/lobby/Lobby.tscn");
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