namespace MurderFloor;

public partial class MapSelect : Control
{
    [Export]
    public bool OnScreen { get; set; } = true;

    [Export]
    RichTextLabel locationRichTextLabel;
    [Export]
    RichTextLabel difficultyRichTextLabel;
    [Export]
    RichTextLabel overscalingRichTextLabel;
    [Export]
    RichTextLabel gorpRichTextLabel;
    [Export]
    RichTextLabel morpRichTextLabel;
    [Export]
    TextureRect mapTextureRect;
    [Export]
    Button referenceMapButton;
    [Export]
    Panel backgroundPanel;
    [Export]
    Button startMapButton;

    [Export]
    Panel selectionPanel;
    [Export]
    Button selectionPanelReturnButton;
    [Export]
    Button selectionPanelSelectButton;
    [Export]
    RichTextLabel selectionPanelMapName;
    [Export]
    HSlider selectionPanelDifficultySlider;
    [Export]
    RichTextLabel selectionPanelDifficultySliderValue;
    [Export]
    HSlider selectionPanelOverscalingSlider;
    [Export]
    RichTextLabel selectionPanelOverscalingSliderValue;
    [Export]
    CheckButton selectionPanelC1;
    [Export]
    CheckButton selectionPanelC2;

    private Map selectedMap;
    private DifficultyConfig difficultyConfig;

    private List<Button> buttons = [];
    private List<Tween> buttonTweens;
    private Map viewedMap;

    public override void _Ready()
    {
        selectionPanel.Visible = false;
        var styleBoxNormal = (StyleBoxFlat)referenceMapButton.GetThemeStylebox("normal").Duplicate();
        var styleBoxPressed = (StyleBoxFlat)referenceMapButton.GetThemeStylebox("pressed").Duplicate();
        var styleBoxHovered = (StyleBoxFlat)referenceMapButton.GetThemeStylebox("hover").Duplicate();
        var styleBoxDisabled = (StyleBoxFlat)referenceMapButton.GetThemeStylebox("disabled").Duplicate();
        referenceMapButton.Free();

        if (!OnScreen) return;

        backgroundPanel.Visible = true;

        startMapButton.Pressed += () =>
        {
            if (selectedMap is null) return;

            // ! if everybody is ready

            NetworkManager.Current.Rpc("LoadGame", selectedMap.MeshScene.ResourcePath);
        };

        foreach (var map in ResourceManager.MapRegistry.GetAllResource())
        {
            var btn = new Button
            {
                OffsetTransformEnabled = true,
                CustomMinimumSize = new Vector2(80, 80),
                Position = mapTextureRect.Size * map.Value.MapLocation - new Vector2(40, 40),
            };

            if (map.Value.MapLevelRequirement > SaveManager.CurrentSave.Level)
                btn.Disabled = true;

            btn.Pressed += () =>
            {
                if (map.Value.MapLevelRequirement > SaveManager.CurrentSave.Level) return;
                if (viewedMap is not null) return;

                viewedMap = map.Value;
                selectionPanelMapName.Text = viewedMap.FullId;

                var targetPos = -(mapTextureRect.Size * map.Value.MapLocation - new Vector2(40, 40)) * 2;
                targetPos += new Vector2(100, 200);
                var tween = mapTextureRect.CreateTween();
                tween
                    .TweenProperty(mapTextureRect, "scale", new Vector2(2, 2), 0.7f)
                    .SetTrans(Tween.TransitionType.Cubic);
                tween
                    .Parallel()
                    .TweenProperty(mapTextureRect, "position", targetPos, 0.7f)
                    .SetTrans(Tween.TransitionType.Cubic);
                tween.TweenCallback(Callable.From(() =>
                {
                    selectionPanel.Visible = true;
                }));
            };

            btn.AddThemeStyleboxOverride("normal", styleBoxNormal);
            btn.AddThemeStyleboxOverride("pressed", styleBoxPressed);
            btn.AddThemeStyleboxOverride("hover", styleBoxHovered);
            btn.AddThemeStyleboxOverride("disabled", styleBoxDisabled);
            mapTextureRect.AddChild(btn);
            buttons.Add(btn);
        }

        selectionPanelDifficultySlider.ValueChanged += (a) =>
        {
            selectionPanelDifficultySliderValue.Text = ((Game.DifficultyEnum)a).ToString();
        };

        selectionPanelOverscalingSlider.ValueChanged += (a) =>
        {
            selectionPanelOverscalingSliderValue.Text = a.ToString("0.00");
        };

        ((CheckButton)selectionPanelOverscalingSlider.GetChild(2)).Toggled += (toggled) =>
        {
            if (toggled)
            {
                selectionPanelOverscalingSlider.MaxValue = 10;
            }
            else
            {
                selectionPanelOverscalingSlider.Value = Math.Min(selectionPanelOverscalingSlider.Value, 1);
                selectionPanelOverscalingSlider.MaxValue = 1;
            }
        };

        selectionPanelReturnButton.Pressed += () =>
        {
            viewedMap = null;
            selectionPanel.Visible = false;
            var tween = mapTextureRect.CreateTween();
            tween
                .TweenProperty(mapTextureRect, "scale", new Vector2(0.25f, 0.25f), 0.7f)
                .SetTrans(Tween.TransitionType.Cubic);
            tween
                .Parallel()
                .TweenProperty(mapTextureRect, "position", new Vector2(10, 10), 0.7f)
                .SetTrans(Tween.TransitionType.Cubic);
        };

        selectionPanelSelectButton.Pressed += () =>
        {
            selectedMap = viewedMap;
            difficultyConfig = new DifficultyConfig()
            {
                Difficulty = (Game.DifficultyEnum)(int)selectionPanelDifficultySlider.Value,
                Overscaling = (float)selectionPanelOverscalingSlider.Value,
                C1 = selectionPanelC1.ToggleMode,
                C2 = selectionPanelC2.ToggleMode,
                MapDifficultyScale = selectedMap.MapDifficultyScale,
            };

            locationRichTextLabel.Text = selectedMap.FullId;
            difficultyRichTextLabel.Text = difficultyConfig.Difficulty.ToString();
            overscalingRichTextLabel.Text = difficultyConfig.Overscaling.ToString("0.00");
        };

        var arr = new Tween[buttons.Count];
        Array.Fill(arr, null);
        buttonTweens = [.. arr];
    }

    public override void _Input(InputEvent @event)
    {
        if (!OS.HasFeature("editor")) return;

        base._Input(@event);
        if (@event is InputEventMouseButton eventMouseButton)
        {
            if (eventMouseButton.ButtonIndex == MouseButton.Left)
            {
                if (eventMouseButton.Pressed && mapTextureRect.GetGlobalRect().HasPoint(eventMouseButton.Position))
                {
                    var mapPos = eventMouseButton.Position - mapTextureRect.GetGlobalRect().Position;
                    GD.Print(mapPos / mapTextureRect.GetGlobalRect().Size);
                }
            }
        }
    }

    public override void _Process(double delta)
    {
        if (!OnScreen) return;
        for (int i = 0; i < buttonTweens.Count; i++)
        {
            var oldTween = buttonTweens[i];
            var btn = buttons[i];
            if (btn.Disabled || oldTween is null || !oldTween.IsRunning())
            {
                var tween = btn.CreateTween();
                tween.TweenProperty(btn, "offset_transform_scale", new Vector2(1.1f, 1.1f), 0.4f).SetTrans(Tween.TransitionType.Sine);
                tween.Parallel().TweenProperty(btn, "offset_transform_position", new Vector2(0, -2f), 0.4f).SetTrans(Tween.TransitionType.Sine);
                tween.TweenProperty(btn, "offset_transform_scale", Vector2.One, 0.4f).SetTrans(Tween.TransitionType.Sine);
                tween.Parallel().TweenProperty(btn, "offset_transform_position", Vector2.Zero, 0.4f).SetTrans(Tween.TransitionType.Sine);
                tween.TweenInterval(0.6f);
                buttonTweens[i] = tween;
            }
        }
    }
}
