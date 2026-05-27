namespace MurderFloor;

public partial class HUD : Control
{
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

    private int activeCrosshair = 0;

    public override void _Process(double delta)
    {
        ProcessCrosshairs();

        string players = "";
        foreach (var player in NetworkManager.Current._players)
        {
            players += $"{player.Key} {player.Value["Name"]}\n";
        }
        playersLabel.Text = players + "\n\n";


        void ListWeapons(List<LiveTool> tools)
        {
            foreach (var tool in tools)
            {
                if (tool == Player.Self.SelectedTool) playersLabel.Text += "-> ";
                playersLabel.Text += tool.ToolResource.NameLocalizationKey + $" {tool.CurrentMag}" + "\n";
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

        var move = Player.Self.MaxHealth - Player.Self.Health;
        healthBarPanel.SetPosition(new Vector2(-move * healthBarPanel.Size.X, 0));
        useInfoLabel.Text = Player.Self.UseInfoText;
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
        activeCrosshair = select;
        EmptyCrosshair.Visible = select == 0;
        GunCrosshair.Visible = select == 1;
        ShotgunCrosshair.Visible = select == 2;
    }
}