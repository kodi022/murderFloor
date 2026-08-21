namespace MurderFloor;

public static class Debug
{
    public static async Task<List<string>> DebugGenerateLoot(int count = 500000, int level = 80)
    {
        var allLootTier = new Dictionary<Game.DifficultyEnum, Dictionary<Loot.Tiers.TierEnum, int>>();
        var allLootWear = new Dictionary<Game.DifficultyEnum, Dictionary<Loot.Wears.WearEnum, int>>();

        void GenerateLoot(Game.DifficultyEnum difficulty)
        {
            var tierCount = new Dictionary<Loot.Tiers.TierEnum, int>();
            var wearCount = new Dictionary<Loot.Wears.WearEnum, int>();
            for (int i = 0; i < count; i++)
            {
                var state = new Loot.LootState((ulong)Random.Shared.NextInt64(), level, difficulty, 0, false, false, 0);
                var e = new Loot.LootRarity(state);
                if (!tierCount.TryAdd(e.Tier, 1))
                    tierCount[e.Tier] += 1;

                if (!wearCount.TryAdd(e.Wear, 1))
                    wearCount[e.Wear] += 1;
            }

            allLootTier.Add(difficulty, tierCount);
            allLootWear.Add(difficulty, wearCount);
        }

        var difficulties = Enum.GetValues<Game.DifficultyEnum>();
        var tasks = difficulties.Select(difficulty =>
            Task.Run(() => GenerateLoot(difficulty))
        );

        await Task.WhenAll(tasks);

        List<string> diffStrings = [];
        diffStrings.Add($"CountPerDifficulty:{count}  Level:{level}  CSKnife:0.25");
        diffStrings.Add("");

        diffStrings.Add("Tiers");
        foreach (var difficulty in allLootTier.OrderBy(c => c.Key))
        {
            var diff = $"{difficulty.Key.ToString()[..4]}: ";

            var values = "";
            foreach (var tier in difficulty.Value.OrderByDescending(t => (int)t.Key))
            {
                var name = tier.Key.ToString()[..4];
                var perc = (float)tier.Value / (float)count * 100f;
                values += $"{name}:{perc:0.00}, ";
            }
            values = values[..^2];

            diffStrings.Add(diff + values);
        }
        diffStrings.Add("");

        diffStrings.Add("Wears");
        foreach (var difficulty in allLootWear.OrderBy(c => c.Key))
        {
            var diff = $"{difficulty.Key.ToString()[..4]}: ";

            var values = "";
            foreach (var wear in difficulty.Value.OrderBy(t => (int)t.Key))
            {
                var name = wear.Key.ToString();
                name = name.Length > 3 ? name[..4] : name;
                var perc = (float)wear.Value / (float)count * 100f;
                values += $"{name}:{perc:0.00}, ";
            }
            values = values[..^2];

            diffStrings.Add(diff + values);
        }

        return diffStrings;
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