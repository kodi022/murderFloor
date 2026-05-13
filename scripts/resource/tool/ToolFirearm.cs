namespace MurderFloor;

[GlobalClass]
public partial class ToolFirearm : Tool
{
    [Export]
    public FirearmTypeEnum FirearmType { get; private set; }
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

    // order by size/power of weapon
    public enum FirearmTypeEnum
    {
        Pistol,
        Revolver,
        SMG,
        PDW,
        Carbine,
        AR,
        Shotgun,
        DMR,
        Special,
        Melee
    }

    public enum SlotEnum
    {
        Secondary,
        Primary,
        Special,
        Melee
    }

    public SlotEnum GetSlot()
    {
        if ((int)FirearmType <= (int)FirearmTypeEnum.Revolver)
        {
            return SlotEnum.Secondary;
        }

        if (FirearmType == FirearmTypeEnum.Special) return SlotEnum.Special;
        if (FirearmType == FirearmTypeEnum.Melee) return SlotEnum.Melee;

        return SlotEnum.Primary;
    }
}