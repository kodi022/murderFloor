namespace MurderFloor;

public partial class HUD : ScreenScaleLimiter
{
    [Export]
    private Panel roundStartPanel;
    [Export]
    private Panel roundTimerPanel;

    [Export]
    private Panel EmptyCrosshair;
    [Export]
    private Panel GunCrosshair;
    [Export]
    private Panel ShotgunCrosshair;

    [Export]
    private Panel healthBarPanel;
    [Export]
    private Panel armorBarPanel;
    [Export]
    private Panel weightBarPanel;
    [Export]
    private Label useInfoLabel;
    [Export]
    private VBoxContainer weaponsContainer;

    private int updateBarsFuncCount = 0;
    private Vector2 lastHealthBarPos = Vector2.Zero;
    private Vector2 lastArmorBarPos = Vector2.Zero;
    private bool hookedGameEvents = false;

    private LiveTool selectedTool;

    private int activeCrosshairIndex = -1;
    private Panel activeCrosshair;

    public override void _Ready()
    {
        roundStartPanel.Visible = false;
        roundTimerPanel.Visible = false;
        EmptyCrosshair.Visible = false;
        GunCrosshair.Visible = false;
        ShotgunCrosshair.Visible = false;
        Player.Self.PlayerOnDamage += HurtAndUpdateHealth;
        Player.Self.PlayerOnHeal += HealAndUpdateHealth;
        Player.Self.PlayerToolChange += GenerateToolLists;

        static string ButtonName(string actionName)
        {
            var inputText = InputMap.ActionGetEvents(actionName)[0].AsText();
            return inputText.Split(' ')[0]; // "Escape" or "W - Physical"
        }

        weaponsContainer.GetChild(0).GetChild(0).GetChild<Label>(0).Text = ButtonName("selectprimary");
        weaponsContainer.GetChild(1).GetChild(0).GetChild<Label>(0).Text = ButtonName("selectsecondary");
        weaponsContainer.GetChild(2).GetChild(0).GetChild<Label>(0).Text = ButtonName("selectspecial");
        weaponsContainer.GetChild(3).GetChild(0).GetChild<Label>(0).Text = ButtonName("selectmelee");

        UpdateHealthAndArmor();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!hookedGameEvents && Game.Current is not null)
        {
            Game.Current.GameRoundStart += AnimateNewRound;
            Game.Current.GameRoundEnd += AnimateRoundTimer;
            hookedGameEvents = true;
        }

        ProcessCrosshairs();

        useInfoLabel.Text = Player.Self.UseInfoText;

        var weightMove = (float)Player.Self.ToolWeight / Player.Self.MaxWeight;
        var newWeightBarPos = new Vector2(2 + (weightBarPanel.Size.X * weightMove) - weightBarPanel.Size.X, 2);
        weightBarPanel.SetPosition(newWeightBarPos);

        if (selectedTool != Player.Self.SelectedTool)
        {
            selectedTool = Player.Self.SelectedTool;
            GenerateToolLists();
        }
    }

    private async void GenerateToolLists()
    {
        async Task ListWeapons(int containerIndex, List<LiveTool> tools)
        {
            var container = weaponsContainer.GetChild(containerIndex);

            bool skippedFirst = false;
            foreach (var child in container.GetChildren())
            {
                if (!skippedFirst)
                {
                    skippedFirst = true;
                    continue;
                }

                child.Free();
            }

            foreach (var tool in tools)
            {
                var scene = GD.Load<PackedScene>("res://scenes/ui/hud/HUDToolBox.tscn");
                var hudToolBox = scene.Instantiate<HUDToolBox>();
                hudToolBox.LiveTool = tool;
                hudToolBox.Equipped = tool == selectedTool;
                container.AddChild(hudToolBox);
            }
        }

        await ListWeapons(0, Player.Self.ToolsPrimary);
        await ListWeapons(1, Player.Self.ToolsSecondary);
        await ListWeapons(2, Player.Self.ToolsSpecial);
        await ListWeapons(3, Player.Self.ToolsMelee);
    }

    private void ProcessCrosshairs()
    {
        void DefaultSize()
        {
            activeCrosshair.Position = -Vector2.One * 4;
            activeCrosshair.Size = Vector2.One * 8;
        }

        var selectedTool = Player.Self.SelectedTool;
        if (selectedTool is null || selectedTool.ToolResource is ToolMelee)
        {
            ChangeCrosshair(0);
            DefaultSize();
            return;
        }

        if (selectedTool.ToolResource is ToolFirearm firearm)
        {
            if (OptionsManager.CurrentOptions.ScalingCrosshair)
            {
                float yaw = Mathf.DegToRad(selectedTool.CurrentSpread.X);
                float pitch = Mathf.DegToRad(selectedTool.CurrentSpread.Y);

                // this does not need normalized to a circle, its just for crosshair movement
                Vector3 dir = Vector3.Forward.Rotated(Vector3.Up, Mathf.Abs(yaw));
                dir = dir.Rotated(Vector3.Right, Mathf.Abs(pitch));

                // do a Camera.UnprojectPosition manually with localized values
                // this is because Camera has something which creates incorrect values
                var viewportSize = GetViewportRect().Size;
                var scale = 1080f / viewportSize.Y;
                var screenCenter = new Vector2I(
                    (int)(viewportSize.X / 2f),
                    (int)(viewportSize.Y / 2f)
                );
                var fovRad = Mathf.DegToRad(Player.Self.Camera.Fov) * scale;
                var focal = screenCenter.Y / Mathf.Tan(fovRad * 0.5f);
                var projected = new Vector2I(
                    (int)(screenCenter.X + dir.X * focal / -dir.Z),
                    (int)(screenCenter.Y - dir.Y * focal / -dir.Z)
                );

                activeCrosshair.Position = -(screenCenter - projected);
                activeCrosshair.Size = (screenCenter - projected) * 2;
            }
            else
            {
                DefaultSize();
            }

            if (selectedTool.Aiming)
                activeCrosshair.Modulate = new Color(1, 1, 1, OptionsManager.CurrentOptions.AimCrosshairOpacity);
            else
                activeCrosshair.Modulate = new Color(1, 1, 1, OptionsManager.CurrentOptions.CrosshairOpacity);

            if (firearm.FirearmType == ToolFirearm.FirearmTypeEnum.Shotgun)
                ChangeCrosshair(2);
            else
                ChangeCrosshair(1);
        }
    }

    private void ChangeCrosshair(int select)
    {
        if (activeCrosshairIndex == select) return;

        var lastCrosshair = activeCrosshairIndex switch
        {
            0 => EmptyCrosshair,
            1 => GunCrosshair,
            2 => ShotgunCrosshair,
            _ => EmptyCrosshair,
        };
        lastCrosshair.Visible = false;
        lastCrosshair.Modulate = new Color(1, 1, 1);

        activeCrosshairIndex = select;
        activeCrosshair = activeCrosshairIndex switch
        {
            0 => EmptyCrosshair,
            1 => GunCrosshair,
            2 => ShotgunCrosshair,
            _ => EmptyCrosshair,
        };
        activeCrosshair.Visible = true;
    }

    private async void HurtAndUpdateHealth(DamageInfoVariant damageInfoVariant)
    {
        var di = DamageInfo.FromVariant(damageInfoVariant);
        var hitFrom = -new Vector3(di.HitDirection.X, 0, di.HitDirection.Z);
        var localDirection = Player.Self.GlobalTransform.Basis.Inverse() * hitFrom;
        var uiDir = new Vector2(localDirection.X, localDirection.Z) * (di.Damage * 0.75f);
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate", new Color(1.5f, 0.5f, 0.5f), 0.1f).SetTrans(Tween.TransitionType.Linear);
        tween.Parallel().TweenProperty(this, "offset_transform_position", uiDir, 0.1f).SetTrans(Tween.TransitionType.Expo);
        tween.TweenProperty(this, "modulate", new Color(1f, 1f, 1f), 0.1f).SetTrans(Tween.TransitionType.Back);
        tween.Parallel().TweenProperty(this, "offset_transform_position", Vector2.Zero, 0.1f).SetTrans(Tween.TransitionType.Back);

        UpdateHealthAndArmor();
    }

    private async void HealAndUpdateHealth(float amount)
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate", new Color(0.5f, 1.5f, 0.5f), 0.12f).SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(this, "modulate", new Color(1f, 1f, 1f), 0.12f).SetTrans(Tween.TransitionType.Sine);

        UpdateHealthAndArmor();
    }

    private async void UpdateHealthAndArmor()
    {
        updateBarsFuncCount++;
        var healthMove = Player.Self.Health / Player.Self.MaxHealth;
        var newHealthBarPos = new Vector2(2 + (healthBarPanel.Size.X * healthMove) - healthBarPanel.Size.X, 2);

        var armorMove = Player.Self.Armor / Player.Self.MaxArmor;
        var newArmorBarPos = new Vector2(2 + (armorBarPanel.Size.X * armorMove) - armorBarPanel.Size.X, 2);

        // ! use tween like in HUDToolBox or above
        var deltas = 0d;
        // smooth over 250 ms
        while (deltas < 0.25d)
        {
            var delta = GetProcessDeltaTime();
            await Task.Delay((int)(delta * 1000d));

            deltas += delta;

            var currentHealthBarPos = lastHealthBarPos.Lerp(newHealthBarPos, (float)deltas * 4f);
            var currentArmorBarPos = lastArmorBarPos.Lerp(newArmorBarPos, (float)deltas * 4f);
            if (updateBarsFuncCount > 1)
            {
                lastHealthBarPos = currentHealthBarPos;
                lastArmorBarPos = currentArmorBarPos;
                updateBarsFuncCount--;
                return;
            }

            healthBarPanel.SetPosition(currentHealthBarPos);
            armorBarPanel.SetPosition(currentArmorBarPos);
        }

        updateBarsFuncCount--;
        lastHealthBarPos = newHealthBarPos;
        lastArmorBarPos = newArmorBarPos;
    }

    private async void AnimateRoundTimer(int round)
    {
        var numberLabel = (Label)roundTimerPanel.GetChild(1);

        roundTimerPanel.Visible = true;

        var time = Game.Current.TimeMsBetweenRounds / 1000;
        while (time > 0)
        {
            numberLabel.Text = time.ToString();
            time--;
            await Task.Delay(1000);
        }

        roundTimerPanel.Visible = false;
    }

    private async void AnimateNewRound(int round)
    {
        roundTimerPanel.Visible = false;
        var numberLabel = (Label)roundStartPanel.GetChild(2);
        numberLabel.Text = round.ToString();
        roundStartPanel.Visible = true;

        await Task.Delay(6000);
        roundStartPanel.Visible = false;
    }
}