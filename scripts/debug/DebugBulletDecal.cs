namespace MurderFloor;

public partial class DebugBulletDecal : Node3D
{
    ulong readyTime = 0;
    public override void _Ready()
    {
        readyTime = Time.GetTicksMsec();
    }

    public override void _Process(double delta)
    {
        if (3000ul < Time.GetTicksMsec() - readyTime)
        {
            Free();
        }
    }
}
