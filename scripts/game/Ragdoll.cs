namespace Shooter.Game;

public partial class Ragdoll : Node3D
{
    [Export]
    PhysicalBoneSimulator3D boneSimulator;

    public ulong MsToDelete { get; set; } = 20000ul;

    private ulong readyTime = 0;

    private string hitBone = null;
    private Vector3 hitDir;
    private float hitForce;

    public override void _Ready()
    {
        readyTime = Time.GetTicksMsec();
        boneSimulator.PhysicalBonesStartSimulation();

        if (hitBone is not null)
        {
            var hit = (PhysicalBone3D)boneSimulator.FindChild("Physical Bone " + hitBone);
            if (hit is null)
            {
                GD.PushWarning($"Ragdoll._Ready: Could not find Bone of name \"Physical Bone {hitBone}\"");
                return;
            }

            hit.LinearVelocity = hitDir * (hitForce / hit.Mass);
        }
    }

    public override void _Process(double delta)
    {
        if (MsToDelete < Time.GetTicksMsec() - readyTime)
        {
            Free();
        }
    }

    public void SetHit(string hitBone, Vector3 hitDir, float hitForce)
    {
        this.hitBone = hitBone;
        this.hitDir = hitDir;
        this.hitForce = hitForce;
    }
}
