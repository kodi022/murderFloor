namespace MurderFloor;

public static class Debug
{
    public static async void DebugGenerateLoot()
    {
        var allLootTier = new Dictionary<Game.GameDifficultyEnum, Dictionary<Loot.TierEnum, int>>();
        var allLootWear = new Dictionary<Game.GameDifficultyEnum, Dictionary<Loot.WearEnum, int>>();
        var lootCount = 500_000;
        var level = 80;

        void GenerateLoot(Game.GameDifficultyEnum difficulty)
        {
            var tierCount = new Dictionary<Loot.TierEnum, int>();
            var wearCount = new Dictionary<Loot.WearEnum, int>();
            for (int i = 0; i < lootCount; i++)
            {
                var state = new Loot.LootStateInfo((ulong)Random.Shared.NextInt64(), level, difficulty, 0, false, false, 0);
                var e = new Loot.LootRarityInfo(state);
                if (!tierCount.TryAdd(e.Tier, 1))
                    tierCount[e.Tier] += 1;

                if (!wearCount.TryAdd(e.Wear, 1))
                    wearCount[e.Wear] += 1;
            }

            allLootTier.Add(difficulty, tierCount);
            allLootWear.Add(difficulty, wearCount);
        }

        var a = Task.Run(async () => { GenerateLoot(Game.GameDifficultyEnum.Easy); });
        var b = Task.Run(async () => { GenerateLoot(Game.GameDifficultyEnum.Medium); });
        var c = Task.Run(async () => { GenerateLoot(Game.GameDifficultyEnum.Challenging); });
        var d = Task.Run(async () => { GenerateLoot(Game.GameDifficultyEnum.Hard); });
        var e = Task.Run(async () => { GenerateLoot(Game.GameDifficultyEnum.Extreme); });
        var f = Task.Run(async () => { GenerateLoot(Game.GameDifficultyEnum.Ludicrous); });
        Task.WaitAll(a, b, c, d, e, f);
        await Task.Delay(20); // necessary or above line isnt guaranteed

        List<string> diffStrings = [];
        diffStrings.Add($"CountPerDifficulty:{lootCount}  Level:{level}  CSKnife:0.25");

        diffStrings.Add("Tiers");
        foreach (var difficulty in allLootTier.OrderBy(c => c.Key))
        {
            var diff = $"{difficulty.Key.ToString()[..4]}: ";

            var values = "";
            foreach (var tier in difficulty.Value.OrderByDescending(t => (int)t.Key))
            {
                var name = tier.Key.ToString()[..4];
                var perc = (float)tier.Value / (float)lootCount * 100f;
                values += $"{name}:{perc:0.00}, ";
            }
            values = values[..^2];

            diffStrings.Add(diff + values);
        }

        diffStrings.Add("Wears");
        foreach (var difficulty in allLootWear.OrderBy(c => c.Key))
        {
            var diff = $"{difficulty.Key.ToString()[..4]}: ";

            var values = "";
            foreach (var wear in difficulty.Value.OrderBy(t => (int)t.Key))
            {
                var name = wear.Key.ToString();
                name = name.Length > 3 ? name[..4] : name;
                var perc = (float)wear.Value / (float)lootCount * 100f;
                values += $"{name}:{perc:0.00}, ";
            }
            values = values[..^2];

            diffStrings.Add(diff + values);
        }

        PrintContainerStrings(diffStrings);
    }

    private static void PrintContainerStrings(List<string> strings)
    {
        var longest = 0;

        foreach (var str in strings)
        {
            if (str.Length > longest) longest = str.Length;
        }

        GD.Print('╔' + "".PadRight(longest, '═') + '╗');

        foreach (var str in strings)
        {
            GD.Print('║' + str.PadRight(longest) + '║');
        }

        GD.Print('╚' + "".PadRight(longest, '═') + '╝');
    }

    public static void DebugDot(Node3D parentNode, Vector3 position, float scale = 1f, Color? color = null, ulong msToDelete = 10000ul)
    {
        var debugDot = GD.Load<PackedScene>("res://scenes/debug/DebugBulletDecal.tscn").Instantiate<Node3D>();
        debugDot.Position = position;
        debugDot.Scale = Vector3.One * scale;

        var debugBulletDecal = (DebugBulletDecal)debugDot;
        debugBulletDecal.MsToDelete = msToDelete;

        if (color is not null && debugBulletDecal.GetActiveMaterial(0) is StandardMaterial3D shared)
        {
            var inst = (StandardMaterial3D)shared.Duplicate(true);
            inst.AlbedoColor = (Color)color;
            debugBulletDecal.MaterialOverride = inst;
        }

        parentNode.AddChild(debugDot);
    }

    public static void DebugDot(Vector3 position, float scale = 1f, Color? color = null, ulong msToDelete = 10000ul)
    {
        DebugDot((Node3D)((SceneTree)Engine.GetMainLoop()).CurrentScene, position, scale, color, msToDelete);
    }
}