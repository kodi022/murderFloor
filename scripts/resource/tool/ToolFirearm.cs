namespace MurderFloor;

[GlobalClass]
public partial class ToolFirearm : Tool
{
    [Export]
    public float RPM { get; private set; }
    [Export]
    public float MagSize { get; private set; }
    [Export]
    public float HoldingSpeed { get; private set; } = 1f;

    [Export, ExportSubgroup("ManualFire (boltaction or pump)")]
    public bool ManualFire { get; private set; }
    [Export]
    public string ManualFireAnimationName { get; private set; }
    [Export(hintString: "Time before weapon can be fired again")]
    public int ManualFireTimeMs { get; private set; }

    [Export, ExportSubgroup("Shotgun")]
    public bool Shotgun { get; private set; }
    [Export]
    public int PelletCount { get; private set; } = 8;
}