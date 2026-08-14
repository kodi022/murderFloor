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
        Player.Self.PlayerOnDamage += UpdateHealthAndArmor;
        Player.Self.PlayerOnHeal += HealAndUpdateHealth;
        Player.Self.PlayerToolChange += GenerateToolLists;
        UpdateHealthAndArmor(null);
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

        // string players = "";
        // players += $"-> {Player.Self.Id}-{NetworkManager.Current._playerInfo["Name"]}\n";
        // foreach (var player in NetworkManager.Current._players)
        // {
        //     if (player.Key == Player.Self.Id) continue;

        //     players += $"{player.Key}-{player.Value["Name"]}";
        //     var p = Player.AllPlayers.First(p => p.Id == player.Key);
        //     if (p is not null && p.SelectedTool is not null)
        //     {
        //         players += $" t{p.ToolsPrimary.Count + p.ToolsSecondary.Count + p.ToolsSpecial.Count + p.ToolsMelee.Count}";
        //         players += $" ({p.SelectedTool.ToolResource.ResourceId} {p.SelectedTool.CurrentMag})";
        //     }
        //     players += "\n";
        // }
        // playersLabel.Text = players + "\n\n\n";

        if (selectedTool != Player.Self.SelectedTool)
        {
            selectedTool = Player.Self.SelectedTool;
            GenerateToolLists();
        }
    }

    private async void GenerateToolLists()
    {
        foreach (var child in weaponsContainer.GetChildren()) child.QueueFree();

        async Task ListWeapons(List<LiveTool> tools)
        {
            var container = new HBoxContainer();
            container.Alignment = BoxContainer.AlignmentMode.End;
            container.AddThemeConstantOverride("separation", 0);
            weaponsContainer.AddChild(container);

            foreach (var tool in tools)
            {
                var scene = GD.Load<PackedScene>("res://scenes/ui/hud/HUDToolBox.tscn");
                var hudToolBox = scene.Instantiate<HUDToolBox>();
                hudToolBox.LiveTool = tool;
                hudToolBox.Equipped = tool == selectedTool;
                container.AddChild(hudToolBox);
            }
        }

        await ListWeapons(Player.Self.ToolsPrimary);
        await ListWeapons(Player.Self.ToolsSecondary);
        await ListWeapons(Player.Self.ToolsSpecial);
        await ListWeapons(Player.Self.ToolsMelee);
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

    private async void HealAndUpdateHealth(float amount)
    {
        // heal effect

        UpdateHealthAndArmor(null);
    }

    private async void UpdateHealthAndArmor(DamageInfoVariant damageInfoVariant)
    {
        updateBarsFuncCount++;
        var healthMove = Player.Self.Health / Player.Self.MaxHealth;
        var newHealthBarPos = new Vector2(2 + (healthBarPanel.Size.X * healthMove) - healthBarPanel.Size.X, 2);

        var armorMove = Player.Self.Armor / Player.Self.MaxArmor;
        var newArmorBarPos = new Vector2(2 + (armorBarPanel.Size.X * armorMove) - armorBarPanel.Size.X, 2);

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
        var numberLabel = (Label)roundTimerPanel.GetChild(0);

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
        var numberLabel = (Label)roundStartPanel.GetChild(1);
        numberLabel.Text = round.ToString();
        roundStartPanel.Visible = true;

        await Task.Delay(6000);
        roundStartPanel.Visible = false;
    }
}