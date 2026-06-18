namespace MurderFloor;

public partial class DebugBulletDecal : MeshInstance3D
{
    public ulong MsToDelete { get; set; } = 5000ul;

    private ulong readyTime = 0;

    public override void _Ready()
    {
        readyTime = Time.GetTicksMsec();
    }

    public override void _Process(double delta)
    {
        if (MsToDelete < Time.GetTicksMsec() - readyTime)
        {
            Free();
        }
    }
}
