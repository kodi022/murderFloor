namespace MurderFloor;

[Tool]
public partial class MobSpawnArea : Node3D
{
    [Export] public Vector3 NodePrimaryPoint { get; internal set; }
    [Export] public Vector3 NodeSecondaryPoint { get; internal set; }

    public List<Vector3> GetSpawnVectorList(int spawnCount)
    {
        List<Vector3> vectors = [];

        var minPoint = NodePrimaryPoint.Min(NodeSecondaryPoint);
        var maxPoint = NodePrimaryPoint.Max(NodeSecondaryPoint);
        var aabb = new Aabb(minPoint, maxPoint - minPoint);
        for (int x = 0; x < 20; x++) for (int z = 0; z < 20; z++)
        {
            var pos = new Vector3(
                minPoint.X + x * 0.6f + 0.01f,
                minPoint.Y + 0.01f,
                minPoint.Z + z * 0.6f + 0.01f
            );

            Global.DebugDot(this, pos, 60000);

            if (aabb.HasPoint(pos)) vectors.Add(GlobalPosition + pos);

            if (vectors.Count >= spawnCount) break;
        }

        return vectors;
    }
}
