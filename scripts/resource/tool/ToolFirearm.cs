namespace MurderFloor;

[GlobalClass]
public partial class ToolFirearm : Tool
{
    public enum FireModeEnum
    {
        Auto,
        Semi,
        Manual
    }

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

    [Export]
    public FirearmTypeEnum FirearmType { get; private set; }
    [Export]
    public float RPM { get; private set; } = 400f;

    [Export]
    public int PelletCount { get; private set; } = 1;
    [Export]
    public float HoldingSpeed { get; private set; } = 1f;
    [Export]
    public string ReloadAnimationName { get; private set; }
    [Export]
    public int ReloadDelayMs { get; private set; } = 800;

    [Export, ExportSubgroup("Ammo")]
    public int MagSize { get; private set; } = 8;
    [Export]
    public int MagsReserve { get; private set; } = 6;
    [Export]
    public bool EndlessReserve { get; private set; } = false;

    [Export, ExportSubgroup("Spread")]
    public Vector2 InitialDegreeSpread { get; private set; } = new Vector2(1f, 1f);
    [Export]
    public Vector2 MaxDegreeSpread { get; private set; } = new Vector2(3f, 3f);
    [Export]
    public float SpreadRecoveryRate { get; private set; } = 2f;
    [Export]
    public Vector2 SpreadIncreasePerShot { get; private set; } = new Vector2(0.5f, 0.5f);

    // ! add movement spread penalty

    [Export, ExportSubgroup("Kick")]
    public Vector2 CameraRotationKick { get; private set; } = new Vector2(0.02f, 0f);
    [Export]
    public Vector2 ViewmodelPositionKick { get; private set; } = new Vector2(0f, 0.075f);
    [Export]
    public Vector2 ViewmodelRotationKick { get; private set; } = new Vector2(0.05f, 0f);
    [Export]
    public Vector2 AimShift { get; private set; } = new Vector2(0.001f, 0f);
    [Export]
    public float ScreenShakeAmount { get; private set; } = 0.1f;

    [Export, ExportSubgroup("FireMode")]
    public FireModeEnum FireMode { get; private set; }
    [Export, ExportSubgroup("FireMode/FireModeManual")]
    public string ManualFireAnimationName { get; private set; }
    [Export]
    public int ManualFireDelayMs { get; private set; } = 400;

    protected RandomNumberGenerator Rng { get; private set; } = new();

    public virtual void FireBullet(FireInfo fi) { }

    public override SlotEnum GetSlot()
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