namespace MurderFloor;

public partial class HUD : ScreenScaleLimiter
{
    [Export]
    private Panel roundStartPanel;
    [Export]
    private Panel roundTimerPanel;
    [Export]
    private Panel waveInfoPanel;

    [Export]
    private Panel emptyCrosshair;
    [Export]
    private Panel gunCrosshair;
    [Export]
    private Panel shotgunCrosshair;

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

    private RichTextLabel waveInfoWave, waveInfoLeft;

    private Panel healthBar, healthBarChange;
    private float lastHealthMove;

    private Panel armorBar, armorBarChange;
    private float lastArmorMove;

    private Panel weightBar, weightBarChange;

    private bool hookedGameEvents = false;

    private LiveTool selectedTool;

    private int activeCrosshairIndex = -1;
    private Panel activeCrosshair;

    public override void _Ready()
    {
        roundStartPanel.Visible = false;
        roundTimerPanel.Visible = false;
        waveInfoPanel.Visible = false;
        emptyCrosshair.Visible = false;
        gunCrosshair.Visible = false;
        shotgunCrosshair.Visible = false;
        Player.Self.PlayerOnDamage += HurtAndUpdateHealth;
        Player.Self.PlayerOnHeal += HealAndUpdateHealth;
        Player.Self.PlayerToolChange += GenerateToolLists;

        waveInfoWave = waveInfoPanel.GetChild<RichTextLabel>(1);
        waveInfoLeft = waveInfoPanel.GetChild<RichTextLabel>(2);

        healthBar = healthBarPanel.GetChild<Panel>(2);
        healthBarChange = healthBarPanel.GetChild<Panel>(1);
        armorBar = armorBarPanel.GetChild<Panel>(2);
        armorBarChange = armorBarPanel.GetChild<Panel>(1);
        weightBar = weightBarPanel.GetChild<Panel>(2);
        weightBarChange = weightBarPanel.GetChild<Panel>(1);

        weaponsContainer.GetChild(0).GetChild(0).GetChild<Label>(0).Text = Global.ButtonName("selectprimary");
        weaponsContainer.GetChild(1).GetChild(0).GetChild<Label>(0).Text = Global.ButtonName("selectsecondary");
        weaponsContainer.GetChild(2).GetChild(0).GetChild<Label>(0).Text = Global.ButtonName("selectspecial");
        weaponsContainer.GetChild(3).GetChild(0).GetChild<Label>(0).Text = Global.ButtonName("selectmelee");

        UpdateHealthAndArmor();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!hookedGameEvents && Game.Current is not null)
        {
            Game.Current.GameWaveStart += AnimateNewRound;
            Game.Current.GameWaveEnd += AnimateRoundTimer;
            hookedGameEvents = true;
        }

        ProcessCrosshairs();

        if (Game.Current is not null && Game.Current.WaveMobsLeft > 0)
        {
            waveInfoPanel.Visible = true;
            waveInfoWave.Text = $"Wave {Game.Current.Wave}/{Game.Current.MaxWave}";
            waveInfoLeft.Text = $"{Game.Current.WaveMobsLeft} Left";
        }
        else
        {
            waveInfoPanel.Visible = false;
        }

        useInfoLabel.Text = Player.Self.UseInfoText;

        if (selectedTool != Player.Self.SelectedTool)
        {
            selectedTool = Player.Self.SelectedTool;
            GenerateToolLists();
        }
    }

    private async void GenerateToolLists()
    {
        var weightMove = (float)Player.Self.ToolWeight / Player.Self.MaxWeight;
        var newWeightBarPos = new Vector2(2 + (weightBar.Size.X * weightMove) - weightBar.Size.X, 2);
        weightBar.SetPosition(newWeightBarPos);
        weightBarChange.SetPosition(newWeightBarPos);

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
            0 => emptyCrosshair,
            1 => gunCrosshair,
            2 => shotgunCrosshair,
            _ => emptyCrosshair,
        };
        lastCrosshair.Visible = false;
        lastCrosshair.Modulate = new Color(1, 1, 1);

        activeCrosshairIndex = select;
        activeCrosshair = activeCrosshairIndex switch
        {
            0 => emptyCrosshair,
            1 => gunCrosshair,
            2 => shotgunCrosshair,
            _ => emptyCrosshair,
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
        tween.TweenProperty(this, "modulate", new Color(1.5f, 0.5f, 0.5f), 0.06d).SetTrans(Tween.TransitionType.Linear);
        tween.Parallel().TweenProperty(this, "offset_transform_position", uiDir, 0.06d).SetTrans(Tween.TransitionType.Linear);
        tween.TweenProperty(this, "modulate", new Color(1f, 1f, 1f), 0.06d).SetTrans(Tween.TransitionType.Linear);
        tween.Parallel().TweenProperty(this, "offset_transform_position", Vector2.Zero, 0.06d).SetTrans(Tween.TransitionType.Linear);

        UpdateHealthAndArmor();
    }

    private async void HealAndUpdateHealth(float amount)
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate", new Color(0.5f, 1.5f, 0.5f), 0.1d).SetTrans(Tween.TransitionType.Sine);
        tween.TweenProperty(this, "modulate", new Color(1f, 1f, 1f), 0.1d).SetTrans(Tween.TransitionType.Sine);

        UpdateHealthAndArmor();
    }

    private async void UpdateHealthAndArmor()
    {
        var healthMove = Player.Self.Health / Player.Self.MaxHealth;
        if (healthMove != lastHealthMove)
        {
            lastHealthMove = healthMove;
            var newHealthBarPos = new Vector2(2 + (healthBar.Size.X * healthMove) - healthBar.Size.X, 2);
            var healthTween = healthBarPanel.CreateTween();
            healthTween.TweenProperty(healthBar, "position", newHealthBarPos, 0.1d);
            healthTween.TweenInterval(1d);
            healthTween.TweenProperty(healthBarChange, "position", newHealthBarPos, 0.3d);
        }

        var armorMove = Player.Self.Armor / Player.Self.MaxArmor;
        if (armorMove != lastArmorMove)
        {
            lastArmorMove = armorMove;
            var newArmorBarPos = new Vector2(2 + (armorBar.Size.X * armorMove) - armorBar.Size.X, 2);
            var armorTween = armorBarPanel.CreateTween();
            armorTween.TweenProperty(armorBar, "position", newArmorBarPos, 0.1d);
            armorTween.TweenInterval(1d);
            armorTween.TweenProperty(armorBarChange, "position", newArmorBarPos, 0.3d);
        }
    }

    private async void AnimateRoundTimer(int round)
    {
        var numberLabel = (Label)roundTimerPanel.GetChild(1);

        roundTimerPanel.Visible = true;

        var time = Game.Current.TimeMsBetweenWaves / 1000;
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