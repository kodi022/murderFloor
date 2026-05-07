namespace MurderFloor;

[GlobalClass]
public partial class ToolFirearm : Tool
{
    [Export]
    public float RPM { get; set; }
    [Export]
    public float MagSize { get; set; }
    [Export]
    public float HoldingSpeed { get; set; } = 1f;

    [Export, ExportSubgroup("ManualFire (boltaction or pump)")]
    public bool ManualFire { get; set; }
    [Export]
    public string ManualFireAnimationName { get; set; }
    [Export(hintString: "Time before weapon can be fired again")]
    public int ManualFireTimeMs { get; set; }

    [Export, ExportSubgroup("Shotgun")]
    public bool Shotgun { get; set; }
    [Export]
    public int PelletCount { get; set; } = 8;
}