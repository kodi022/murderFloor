namespace Shooter.Ui;

public partial class MapSelect : OpenUi
{
    public static bool MapSelected { get; private set; } = false;
    public static Resource.Map SelectedMapNetworked { get; private set; } = null;
    public static DifficultyConfig DifficultyConfigNetworked { get; private set; }
    public static Vector2 SelectedButtonLocation { get; private set; }

    [Export]
    public bool OnScreen { get; set; } = true;

    private static MapSelect worldMapSelect;

    [Export]
    private RichTextLabel locationRichTextLabel;
    [Export]
    private RichTextLabel difficultyRichTextLabel;
    [Export]
    private RichTextLabel overscalingRichTextLabel;
    [Export]
    private RichTextLabel gorpRichTextLabel;
    [Export]
    private RichTextLabel morpRichTextLabel;
    [Export]
    private TextureRect mapTextureRect;
    [Export]
    private Button referenceMapButton;
    [Export]
    private Panel backgroundPanel;

    [Export]
    private Panel selectionPanel;
    [Export]
    private Button selectionPanelReturnButton;
    [Export]
    private Button selectionPanelSelectButton;
    [Export]
    private RichTextLabel selectionPanelMapName;
    [Export]
    private HSlider selectionPanelDifficultySlider;
    [Export]
    private RichTextLabel selectionPanelDifficultySliderValue;
    [Export]
    private HSlider selectionPanelOverscalingSlider;
    [Export]
    private RichTextLabel selectionPanelOverscalingSliderValue;
    [Export]
    private CheckBox selectionPanelC1;
    [Export]
    private CheckBox selectionPanelC2;

    private List<Button> buttons = [];
    private List<Tween> buttonTweens;

    private Resource.Map viewedMap;
    private Vector2 buttonLocation;
    private bool c1 = false;
    private bool c2 = false;

    private StyleBox referenceNormalStyle;
    private Button worldButton;
    private Tween worldButtonTween;

    public override void _Ready()
    {
        selectionPanel.Visible = false;
        referenceNormalStyle = (StyleBoxFlat)referenceMapButton.GetThemeStylebox("normal").Duplicate();
        var styleBoxPressed = (StyleBoxFlat)referenceMapButton.GetThemeStylebox("pressed").Duplicate();
        var styleBoxHovered = (StyleBoxFlat)referenceMapButton.GetThemeStylebox("hover").Duplicate();
        var styleBoxDisabled = (StyleBoxFlat)referenceMapButton.GetThemeStylebox("disabled").Duplicate();
        referenceMapButton.Free();

        if (!OnScreen)
        {
            worldMapSelect = this;
            return;
        }

        if (MapSelected) UpdateMapInfo(this);

        Modulate = new Color(0, 0, 0);
        Scale = new Vector2(0.8f, 0.8f);
        var tween = CreateTween();
        tween
            .TweenProperty(this, "modulate", new Color(1, 1, 1), 0.4f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
        tween
            .Parallel()
            .TweenProperty(this, "scale", Vector2.One, 0.4f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        backgroundPanel.Visible = true;

        selectionPanelC1.Toggled += (a) => { c1 = a; };
        selectionPanelC2.Toggled += (a) => { c2 = a; };

        foreach (var map in ResourceManager.MapRegistry.GetAllResource())
        {
            var btn = new Button
            {
                OffsetTransformEnabled = true,
                Size = new Vector2(80, 80),
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
                buttonLocation = mapTextureRect.Size * map.Value.MapLocation - new Vector2(40, 40);

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

            btn.AddThemeStyleboxOverride("normal", referenceNormalStyle);
            btn.AddThemeStyleboxOverride("pressed", styleBoxPressed);
            btn.AddThemeStyleboxOverride("hover", styleBoxHovered);
            btn.AddThemeStyleboxOverride("disabled", styleBoxDisabled);
            mapTextureRect.AddChild(btn);
            buttons.Add(btn);
        }

        selectionPanelDifficultySlider.ValueChanged += (a) =>
        {
            selectionPanelDifficultySliderValue.Text = ((Game.Game.DifficultyEnum)a).ToString();
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
            var diffConfig = new DifficultyConfig()
            {
                Difficulty = (Game.Game.DifficultyEnum)(int)selectionPanelDifficultySlider.Value,
                Overscaling = (float)selectionPanelOverscalingSlider.Value,
                C1 = c1,
                C2 = c2,
                MapDifficultyScale = viewedMap.MapDifficultyScale,
            };

            SelectedMapNetworked = viewedMap;
            DifficultyConfigNetworked = diffConfig;
            UpdateMapInfo(this);
            worldMapSelect.Rpc(MethodName.MapInfoRpc, viewedMap.FullId, diffConfig.Serialize(), buttonLocation);
        };

        var arr = new Tween[buttons.Count];
        Array.Fill(arr, null);
        buttonTweens = [.. arr];
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
            .TweenProperty(this, "scale", new Vector2(0.8f, 0.8f), 0.4f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
        tween.TweenCallback(Callable.From(Free));
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
        if (!OnScreen)
        {
            if (SelectedButtonLocation != Vector2.Zero)
            {
                if (worldButton is null)
                {
                    worldButton = new Button { OffsetTransformEnabled = true, Size = new Vector2(120, 120) };
                    mapTextureRect.AddChild(worldButton);
                    worldButton.AddThemeStyleboxOverride("normal", referenceNormalStyle);
                }

                if (worldButtonTween is null || !worldButtonTween.IsRunning())
                {
                    worldButtonTween = worldButton.CreateTween();
                    worldButtonTween
                        .TweenProperty(worldButton, "offset_transform_scale", new Vector2(1.2f, 1.2f), 0.4f)
                        .SetTrans(Tween.TransitionType.Sine);
                    worldButtonTween
                        .Parallel()
                        .TweenProperty(worldButton, "offset_transform_position", new Vector2(0, -4f), 0.4f)
                        .SetTrans(Tween.TransitionType.Sine);
                    worldButtonTween
                        .TweenProperty(worldButton, "offset_transform_scale", Vector2.One, 0.4f)
                        .SetTrans(Tween.TransitionType.Sine);
                    worldButtonTween
                        .Parallel()
                        .TweenProperty(worldButton, "offset_transform_position", Vector2.Zero, 0.4f)
                        .SetTrans(Tween.TransitionType.Sine);
                    worldButtonTween.TweenInterval(0.4f);
                }

                worldButton.Position = SelectedButtonLocation;
            }

            return;
        }

        for (int i = 0; i < buttonTweens.Count; i++)
        {
            var oldTween = buttonTweens[i];
            var btn = buttons[i];
            if (btn.Disabled || oldTween is null || !oldTween.IsRunning())
            {
                var tween = btn.CreateTween();
                tween
                    .TweenProperty(btn, "offset_transform_scale", new Vector2(1.1f, 1.1f), 0.4f)
                    .SetTrans(Tween.TransitionType.Sine);
                tween
                    .Parallel()
                    .TweenProperty(btn, "offset_transform_position", new Vector2(0, -2f), 0.4f)
                    .SetTrans(Tween.TransitionType.Sine);
                tween
                    .TweenProperty(btn, "offset_transform_scale", Vector2.One, 0.4f)
                    .SetTrans(Tween.TransitionType.Sine);
                tween
                    .Parallel()
                    .TweenProperty(btn, "offset_transform_position", Vector2.Zero, 0.4f)
                    .SetTrans(Tween.TransitionType.Sine);
                tween.TweenInterval(0.6f);
                buttonTweens[i] = tween;
            }
        }
    }

    // do not make static, godot does not support static Rpcs
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void MapInfoRpc(string mapFullId, string difficultyConfig, Vector2 buttonLocation)
    {
        SelectedMapNetworked = ResourceManager.MapRegistry.GetResourceRef(mapFullId);
        DifficultyConfigNetworked = DifficultyConfig.Deserialize(difficultyConfig);
        SelectedButtonLocation = buttonLocation;
        MapSelected = true;
        UpdateMapInfo(worldMapSelect);
    }

    private static void UpdateMapInfo(MapSelect mapSelect)
    {
        mapSelect.locationRichTextLabel.Text = SelectedMapNetworked.FullId;
        mapSelect.difficultyRichTextLabel.Text = DifficultyConfigNetworked.Difficulty.ToString();
        mapSelect.overscalingRichTextLabel.Text = DifficultyConfigNetworked.Overscaling.ToString("0.00");
    }
}
