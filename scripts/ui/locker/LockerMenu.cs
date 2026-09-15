namespace Shooter.Ui;

public partial class LockerMenu : ScreenScaleLimiter
{
    [Export]
    private Panel controlsRoot;
    [Export]
    private Panel toolsPanel;

    private Control openMenu;

    public override async void _Ready()
    {
        base._Ready();
        toolsPanel.GetChild<Button>(1).Pressed += () => SwitchPage("res://scenes/ui/locker/LockerListMenu.tscn");

        var bref = toolsPanel.GetChild(0).GetChild(0);
        foreach (var equip in SaveManager.CurrentSave.GetEquippedLoot())
        {
            var lootRef = equip.GetLootRef();
            if (lootRef.FullId == "base:fists") continue;

            var bref2 = bref.Duplicate();
            bref2.GetChild(0).GetChild<TextureRect>(0).Texture = await lootRef.GenerateThumbnailImage(256, 128);
            toolsPanel.GetChild(0).AddChild(bref2);
        }
        bref.Free();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (IsInstanceValid(openMenu))
            GetChild<Control>(0).Modulate = new Color(0, 0, 0, 0);
        else
            GetChild<Control>(0).Modulate = new Color(1, 1, 1, 1);
    }

    private void SwitchPage(string path)
    {
        if (IsInstanceValid(openMenu)) return;

        var control = GD.Load<PackedScene>(path);
        openMenu = control.Instantiate<Control>();
        AddChild(openMenu);
    }
}
