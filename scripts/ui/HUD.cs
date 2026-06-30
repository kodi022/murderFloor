namespace MurderFloor;

public partial class HUD : Control
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
    private Label playersLabel;
    [Export]
    private Panel healthBarPanel;
    [Export]
    private Label useInfoLabel;

    private int activeCrosshair = -1;

    private int healthBarFunctionCount = 0;
    private Vector2 lastHealthBarPos = Vector2.Zero;
    private bool hookedGameEvents = false;

    public override void _Ready()
    {
        roundStartPanel.Visible = false;
        roundTimerPanel.Visible = false;
        Player.Self.PlayerOnDamage += UpdateHealth;
    }

    public override void _Process(double delta)
    {
        if (!hookedGameEvents && Game.Current is not null)
        {
            Game.Current.GameRoundStart += AnimateNewRound;
            Game.Current.GameRoundEnd += AnimateRoundTimer;
            hookedGameEvents = true;
        }

        ProcessCrosshairs();

        useInfoLabel.Text = Player.Self.UseInfoText;

        string players = "";
        players += $"-> {Player.Self.Id}-{NetworkManager.Current._playerInfo["Name"]}\n";
        foreach (var player in NetworkManager.Current._players)
        {
            if (player.Key == Player.Self.Id) continue;

            players += $"{player.Key}-{player.Value["Name"]}";
            var p = Player.AllPlayers.First(p => p.Id == player.Key);
            if (p is not null && p.SelectedTool is not null)
            {
                players += $" t{p.ToolsPrimary.Count + p.ToolsSecondary.Count + p.ToolsSpecial.Count + p.ToolsMelee.Count}";
                players += $" ({p.SelectedTool.ToolResource.ResourceId} {p.SelectedTool.CurrentMag})";
            }
            players += "\n";
        }
        playersLabel.Text = players + "\n\n\n";

        void ListWeapons(List<LiveTool> tools)
        {
            foreach (var tool in tools)
            {
                if (tool == Player.Self.SelectedTool) playersLabel.Text += "-> ";
                playersLabel.Text += tool.ToolResource.ResourceId + $" {tool.CurrentMag} / {tool.CurrentReserve}" + "\n";
            }
        }
        void CropAndNewline()
        {
            playersLabel.Text = playersLabel.Text[..^1] + "\n\n";
        }

        ListWeapons(Player.Self.ToolsPrimary);
        CropAndNewline();
        ListWeapons(Player.Self.ToolsSecondary);
        CropAndNewline();
        ListWeapons(Player.Self.ToolsSpecial);
        CropAndNewline();
        ListWeapons(Player.Self.ToolsMelee);
    }

    private void ProcessCrosshairs()
    {
        var selectedTool = Player.Self.SelectedTool;
        if (selectedTool is null || selectedTool.ToolResource is ToolMelee melee)
        {
            ChangeCrosshair(0);
            return;
        }

        if (selectedTool.ToolResource is ToolFirearm firearm)
        {
            float yaw = Mathf.DegToRad(selectedTool.CurrentSpread.X);
            float pitch = Mathf.DegToRad(selectedTool.CurrentSpread.Y);

            // this does not need normalized to a circle, its just for crosshair movement
            Vector3 dir = Vector3.Forward.Rotated(Vector3.Up, Mathf.Abs(yaw));
            dir = dir.Rotated(Vector3.Right, Mathf.Abs(pitch));

            // do a Camera.UnprojectPosition manually with localized values
            // this is because Camera has interpolation or something
            // which creates incorrect values
            var screenCenter = DisplayServer.WindowGetSize() / 2;
            var fovRad = Mathf.DegToRad(Player.Self.Camera.Fov);
            var focal = screenCenter.Y / Mathf.Tan(fovRad * 0.5f);
            var projected = new Vector2(
                screenCenter.X + dir.X * focal / -dir.Z,
                screenCenter.Y - dir.Y * focal / -dir.Z
            );

            if (firearm.FirearmType == ToolFirearm.FirearmTypeEnum.Shotgun)
            {
                ChangeCrosshair(2);
                ShotgunCrosshair.Position = -(screenCenter - projected);
                ShotgunCrosshair.Size = (screenCenter - projected) * 2;
            }
            else
            {
                ChangeCrosshair(1);
                GunCrosshair.Position = -(screenCenter - projected);
                GunCrosshair.Size = (screenCenter - projected) * 2;
            }
        }
    }

    private void ChangeCrosshair(int select)
    {
        if (activeCrosshair == select) return;

        activeCrosshair = select;
        EmptyCrosshair.Visible = select == 0;
        GunCrosshair.Visible = select == 1;
        ShotgunCrosshair.Visible = select == 2;
    }

    private async void UpdateHealth(DamageInfo damageInfo)
    {
        healthBarFunctionCount++;
        var move = Player.Self.Health / Player.Self.MaxHealth;
        var newHealthBarPos = new Vector2((healthBarPanel.Size.X * move) - healthBarPanel.Size.X, 0);

        var deltas = 0d;
        // smooth over 250 ms
        while (deltas < 0.25d)
        {
            var delta = GetProcessDeltaTime();
            await Task.Delay((int)(delta * 1000d));

            deltas += delta;
            var currentHealthBarPos = lastHealthBarPos.Lerp(newHealthBarPos, (float)deltas * 4f);
            if (healthBarFunctionCount > 1)
            {
                lastHealthBarPos = currentHealthBarPos;
                healthBarFunctionCount--;
                return;
            }

            healthBarPanel.SetPosition(currentHealthBarPos);
        }

        healthBarFunctionCount--;
        lastHealthBarPos = newHealthBarPos;
    }

    private async void AnimateRoundTimer(int round)
    {
        var numberLabel = (Label)roundTimerPanel.GetChild(0);

        roundTimerPanel.Visible = true;

        var time = Game.Current.TimeMsBetweenRounds / 1000;
        while (time > 0)
        {
            numberLabel.Text = $"{time}";
            time--;
            await Task.Delay(1000);
        }

        roundTimerPanel.Visible = false;
    }

    private async void AnimateNewRound(int round)
    {
        roundTimerPanel.Visible = false;
        var numberLabel = (Label)roundStartPanel.GetChild(1);
        numberLabel.Text = $"{round}";
        roundStartPanel.Visible = true;

        await Task.Delay(6000);
        roundStartPanel.Visible = false;
    }
}