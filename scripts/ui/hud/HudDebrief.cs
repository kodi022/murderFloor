namespace Shooter.Ui;

public partial class HudDebrief : OpenUi
{
    public static float EarnedXp { get; set; }
    public static List<Resource.Loot.LootState> AllEarnedLoot { get; set; } = [];
    public static List<Resource.Loot.LootState> UnpickedEarnedLoot { get; set; } = [];

    public override async void _Ready()
    {
        SaveManager.CurrentSave.AddXp(EarnedXp);
        foreach (var loot in UnpickedEarnedLoot) SaveManager.CurrentSave.Loot.Add(loot.Serialize());
        SaveManager.Save(SaveManager.CurrentSave);

        Modulate = new Color(0, 0, 0);
        ((Panel)FindChild("XpPanel")).Modulate = new Color(0, 0, 0, 0);
        ((Panel)FindChild("LevelPanel?")).Modulate = new Color(0, 0, 0, 0);
        ((Panel)FindChild("LootPanel")).Modulate = new Color(0, 0, 0, 0);

        Modulate = new Color(0, 0, 0);
        OffsetTransformScale = new Vector2(0.8f, 0.8f);
        var tween = CreateTween();
        tween
            .TweenProperty(this, "modulate", new Color(1, 1, 1), 0.4f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween
            .Parallel()
            .TweenProperty(this, "offset_transform_scale", Vector2.One, 0.4f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        tween.Finished += async () =>
        {
            tween = CreateTween();
            tween.Stop();
            await XpBar(tween);
            tween = CreateTween();
            tween.Stop();
            await LootPanel(tween);

            EarnedXp = 0;
            AllEarnedLoot = [];
            UnpickedEarnedLoot = [];
        };
    }

    public override void Close()
    {
        var tween = CreateTween();
        tween
            .TweenProperty(this, "modulate", new Color(0, 0, 0), 0.4f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        tween
            .Parallel()
            .TweenProperty(this, "offset_transform_scale", new Vector2(0.8f, 0.8f), 0.4f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        tween.TweenCallback(Callable.From(Free));
    }

    private async Task XpBar(Tween tween)
    {
        var panel = (Panel)FindChild("XpPanel");
        tween
            .TweenProperty(panel, "modulate", new Color(1, 1, 1), 0.5f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        await Task.Delay(500);

        var xp = SaveManager.CurrentSave.Xp + EarnedXp;
        var xpNext = SaveManager.CurrentSave.XpToNextLevel();
        var bar = (Panel)panel.GetChild(1).GetChild(0);
        bar.Position = new Vector2(-(1 - (SaveManager.CurrentSave.Xp / xpNext)) * bar.Size.X, 0);

        for (int i = 0; i < 100; i++)
        {
            xpNext = SaveManager.CurrentSave.XpToNextLevel(SaveManager.CurrentSave.Level + i);
            if (xp >= xpNext)
            {
                GD.Print("ae");
                var diff = (-bar.Position.X / bar.Size.X) - MathF.Min(i * 0.1f, 0.5f);
                tween
                    .TweenProperty(bar, "position", new Vector2(0, 0), diff * 4)
                    .SetTrans(Tween.TransitionType.Linear);
                tween
                    .TweenProperty(bar, "position", new Vector2(-bar.Size.X, 0), 0);

                xp -= xpNext;
            }
            else
            {
                GD.Print("aeb");
                var diff = -bar.Position.X / bar.Size.X;
                tween
                    .TweenProperty(bar, "position", new Vector2(-(1 - (xp / xpNext)) * bar.Size.X, 0), diff)
                    .SetTrans(Tween.TransitionType.Linear);

                break;
            }
        }

        tween.Play();
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    private async Task LootPanel(Tween tween)
    {
        var panel = (Panel)FindChild("LootPanel");
        tween
            .TweenProperty(panel, "modulate", new Color(1, 1, 1), 0.5f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        foreach (var loot in AllEarnedLoot)
        {
            AddChild(new Label() { Text = "Lol new item" });
            await Task.Delay(250);
        }

        tween.Play();
        await ToSignal(tween, Tween.SignalName.Finished);
    }
}