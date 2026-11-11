namespace Shooter.Debug;

public static class Rendering
{
    public static void Point(Node3D parentNode, Vector3 position, float scale = 1f, Color? color = null, ulong msToDelete = 10000ul)
    {
        var debugDot = GD.Load<PackedScene>("res://scenes/debug/DebugBulletDecal.tscn").Instantiate<Node3D>();
        debugDot.Position = position;
        debugDot.Scale = Vector3.One * scale;

        var debugBulletDecal = (PointMesh)debugDot;
        debugBulletDecal.MsToDelete = msToDelete;

        if (color is not null && debugBulletDecal.GetActiveMaterial(0) is StandardMaterial3D shared)
        {
            var inst = (StandardMaterial3D)shared.Duplicate(true);
            inst.AlbedoColor = (Color)color;
            debugBulletDecal.MaterialOverride = inst;
        }

        parentNode.AddChild(debugDot);
    }

    public static void Point(Vector3 position, float scale = 1f, Color? color = null, ulong msToDelete = 10000ul)
    {
        Point((Node3D)((SceneTree)Engine.GetMainLoop()).CurrentScene, position, scale, color, msToDelete);
    }
}