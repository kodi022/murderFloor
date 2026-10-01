namespace Shooter.Ui;

public partial class HudDebrief : OpenUi
{
    public static float EarnedXp { get; set; }
    public static List<Resource.Loot.LootState> AllEarnedLoot { get; set; } = [];
    public static List<Resource.Loot.LootState> UnpickedEarnedLoot { get; set; } = [];

    private float currentXp;

    public override async void _Ready()
    {
        currentXp = SaveManager.CurrentSave.Xp;

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
            .SetEase(Tween.EaseType.Out)
            .SetDelay(0.5f);

        var xp = currentXp + EarnedXp;
        var xpNext = SaveManager.CurrentSave.XpToNextLevel();
        var bar = (Panel)panel.GetChild(1).GetChild(0);
        bar.Position = new Vector2(-(1 - (currentXp / xpNext)) * bar.Size.X, 0);

        var e = 0;
        var label = (Label)panel.GetChild(0);
        var lastStr = "";
        while (e < EarnedXp)
        {
            e += 1;
            var str = $"Exp +{e}";
            tween.TweenMethod(Callable.From<string>(SetLabelText), lastStr, str, 0f).SetDelay(0.01f);
            lastStr = str;
        }

        void SetLabelText(string str)
        {
            label.Text = str;
        }

        for (int i = 0; i < 100; i++)
        {
            xpNext = SaveManager.CurrentSave.XpToNextLevel(SaveManager.CurrentSave.Level + i);
            if (xp >= xpNext)
            {
                var diff = (-bar.Position.X / bar.Size.X) - MathF.Min(i * 0.1f, 0.4f);
                tween
                    .TweenProperty(bar, "position", new Vector2(0, 0), diff * 3)
                    .SetTrans(Tween.TransitionType.Linear);
                tween
                    .TweenProperty(bar, "position", new Vector2(-bar.Size.X, 0), 0);
                xp -= xpNext;
            }
            else
            {
                var diff = (-bar.Position.X / bar.Size.X) - MathF.Min(i * 0.1f, 0.4f);
                tween
                    .TweenProperty(bar, "position", new Vector2(-(1 - (xp / xpNext)) * bar.Size.X, 0), diff * 3)
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
            .SetEase(Tween.EaseType.Out)
            .SetDelay(0.5f);

        foreach (var loot in AllEarnedLoot)
        {
            tween.TweenMethod(Callable.From<string>(AddPanel), "", loot.Serialize(), 0f).SetDelay(0.35f);
        }

        void AddPanel(string lootState)
        {
            panel.GetChild(0).AddChild(new Label() { Text = lootState });
        }

        tween.Play();
        await ToSignal(tween, Tween.SignalName.Finished);
    }
}