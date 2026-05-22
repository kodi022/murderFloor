namespace MurderFloor;

[Tool]
public partial class MobSpawnArea : Node3D
{
    [Export] public Vector3 NodePrimaryPoint { get; internal set; }
    [Export] public Vector3 NodeSecondaryPoint { get; internal set; }
}
