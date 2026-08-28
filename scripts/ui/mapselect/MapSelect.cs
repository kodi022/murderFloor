namespace MurderFloor;

public partial class MapSelect : Control
{
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
    public bool OnScreen { get; set; } = true;

    private List<Button> buttons = [];
    private List<Tween> buttonTweens;

    public override void _Ready()
    {
        var styleBoxNormal = (StyleBoxFlat)referenceMapButton.GetThemeStylebox("normal").Duplicate();
        var styleBoxPressed = (StyleBoxFlat)referenceMapButton.GetThemeStylebox("pressed").Duplicate();
        var styleBoxHovered = (StyleBoxFlat)referenceMapButton.GetThemeStylebox("hover").Duplicate();
        var styleBoxDisabled = (StyleBoxFlat)referenceMapButton.GetThemeStylebox("disabled").Duplicate();
        referenceMapButton.Free();

        if (!OnScreen) return;

        backgroundPanel.Visible = true;
        foreach (var map in ResourceManager.MapRegistry.GetAllResource())
        {
            var btn = new Button
            {
                OffsetTransformEnabled = true,
                CustomMinimumSize = new Vector2(80, 80),
                Position = mapTextureRect.Size * map.Value.MapLocation - new Vector2(40, 40),
            };

            btn.Pressed += () =>
            {
                locationRichTextLabel.Text = map.Value.FullId;

                var targetPos = -(mapTextureRect.Size * map.Value.MapLocation - new Vector2(40, 40)) * 2;
                targetPos += new Vector2(100, 400);

                var tween = mapTextureRect.CreateTween();
                tween
                    .TweenProperty(mapTextureRect, "scale", new Vector2(2, 2), 0.7f)
                    .SetTrans(Tween.TransitionType.Cubic);
                tween
                    .Parallel()
                    .TweenProperty(mapTextureRect, "position", targetPos, 0.7f)
                    .SetTrans(Tween.TransitionType.Cubic);
                tween
                    .TweenInterval(2f);
                tween
                    .TweenProperty(mapTextureRect, "scale", new Vector2(0.25f, 0.25f), 0.7f)
                    .SetTrans(Tween.TransitionType.Cubic);
                tween
                    .Parallel()
                    .TweenProperty(mapTextureRect, "position", new Vector2(10, 10), 0.7f)
                    .SetTrans(Tween.TransitionType.Cubic);
            };

            // ! disable if not high enough level for map

            btn.AddThemeStyleboxOverride("normal", styleBoxNormal);
            btn.AddThemeStyleboxOverride("pressed", styleBoxPressed);
            btn.AddThemeStyleboxOverride("hover", styleBoxHovered);
            btn.AddThemeStyleboxOverride("disabled", styleBoxDisabled);
            mapTextureRect.AddChild(btn);
            buttons.Add(btn);
        }

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
            if (oldTween is null || !oldTween.IsRunning())
            {
                var btn = buttons[i];
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
