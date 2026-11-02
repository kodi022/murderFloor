namespace Shooter.Game;

public partial class ParticleDespawner : Node3D
{
    private GpuParticles3D particles;

    public override void _Ready()
    {
        particles = (GpuParticles3D)GetChild(0);
        particles.Finished += QueueFree;
        particles.Restart();
    }
}