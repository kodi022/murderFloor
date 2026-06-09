#if TOOLS

using Godot;
using MurderFloor;
using System;

public partial class MobSpawnAreaGizmo : EditorNode3DGizmoPlugin
{
    private const int primaryHandleId = 0;
    private const int secondaryHandleId = 1;

    public MobSpawnAreaGizmo()
    {
        CreateMaterial("NodePrimaryPointMaterial", new Color(1, 0, 0));
        CreateMaterial("NodeSecondaryPointMaterial", new Color(0, 1, 0));
        CreateHandleMaterial("HandlePrimaryPointMaterial");
        CreateHandleMaterial("HandleSecondaryPointMaterial");
        var handlePrimaryPointMaterial = GetMaterial("HandlePrimaryPointMaterial");
        var handleSecondaryPointMaterial = GetMaterial("HandleSecondaryPointMaterial");
        handlePrimaryPointMaterial.AlbedoColor = new Color(1, 0, 0);
        handleSecondaryPointMaterial.AlbedoColor = new Color(0, 1, 0);
    }

    public MobSpawnAreaGizmo(EditorUndoRedoManager undoRedo)
    {
        CreateMaterial("NodePrimaryPointMaterial", new Color(1, 0, 0));
        CreateMaterial("NodeSecondaryPointMaterial", new Color(0, 1, 0));
        CreateHandleMaterial("HandlePrimaryPointMaterial");
        CreateHandleMaterial("HandleSecondaryPointMaterial");
        var handlePrimaryPointMaterial = GetMaterial("HandlePrimaryPointMaterial");
        var handleSecondaryPointMaterial = GetMaterial("HandleSecondaryPointMaterial");
        handlePrimaryPointMaterial.AlbedoColor = new Color(1, 0, 0);
        handleSecondaryPointMaterial.AlbedoColor = new Color(0, 1, 0);
    }

    public override void _Redraw(EditorNode3DGizmo gizmo)
    {
        gizmo.Clear();
        base._Redraw(gizmo);

        var spawnArea = (MobSpawnArea)gizmo.GetNode3D();
        var a = spawnArea.NodePrimaryPoint;
        var b = spawnArea.NodeSecondaryPoint;
        var lines = new[] {
            // vertical
            new Vector3(a.X, a.Y, a.Z), new Vector3(a.X, b.Y, a.Z),
            new Vector3(a.X, b.Y, b.Z), new Vector3(a.X, a.Y, b.Z),
            new Vector3(b.X, a.Y, a.Z), new Vector3(b.X, b.Y, a.Z),
            new Vector3(b.X, b.Y, b.Z), new Vector3(b.X, a.Y, b.Z),
            // primary X horizontal
            new Vector3(a.X, a.Y, a.Z), new Vector3(b.X, a.Y, a.Z),
            new Vector3(a.X, a.Y, b.Z), new Vector3(b.X, a.Y, b.Z),
            // primary Z horizontal
            new Vector3(a.X, a.Y, a.Z), new Vector3(a.X, a.Y, b.Z),
            new Vector3(b.X, a.Y, a.Z), new Vector3(b.X, a.Y, b.Z),
            // secondary X horizontal
            new Vector3(a.X, b.Y, a.Z), new Vector3(b.X, b.Y, a.Z),
            new Vector3(a.X, b.Y, b.Z), new Vector3(b.X, b.Y, b.Z),
            // secondary Z horizontal
            new Vector3(a.X, b.Y, a.Z), new Vector3(a.X, b.Y, b.Z),
            new Vector3(b.X, b.Y, a.Z), new Vector3(b.X, b.Y, b.Z),

        };

        var handles = new[] { spawnArea.NodePrimaryPoint, spawnArea.NodeSecondaryPoint };
        gizmo.AddLines(lines, GetMaterial("NodePrimaryPointMaterial", gizmo));
        gizmo.AddHandles(handles, GetMaterial("HandlePrimaryPointMaterial", gizmo), [primaryHandleId, secondaryHandleId]);
    }

    public override string _GetHandleName(EditorNode3DGizmo gizmo, int handleId, bool secondary)
    {
        // vsc says "Unnecessary assignment of a value to 'mobSpawnArea'" its lying
        var spawnArea = (MobSpawnArea)gizmo.GetNode3D();
        return handleId switch
        {
            0 => nameof(spawnArea.NodePrimaryPoint),
            1 => nameof(spawnArea.NodeSecondaryPoint),
            _ => "Unknown Handle",
        };
    }

    public override Variant _GetHandleValue(EditorNode3DGizmo gizmo, int handleId, bool secondary)
    {
        var spawnArea = (MobSpawnArea)gizmo.GetNode3D();
        return handleId switch
        {
            0 => spawnArea.NodePrimaryPoint,
            1 => spawnArea.NodeSecondaryPoint,
            _ => Vector3.Zero,
        };
    }

    public override void _SetHandle(EditorNode3DGizmo gizmo, int handleId, bool secondary, Camera3D camera, Vector2 screenPos)
    {
        var mobSpawnArea = (MobSpawnArea)gizmo.GetNode3D();
        switch (handleId)
        {
            case 0:
                var depth = GetZDepth(camera, mobSpawnArea.GlobalPosition + mobSpawnArea.NodePrimaryPoint);
                mobSpawnArea.NodePrimaryPoint = camera.ProjectPosition(screenPos, depth) - mobSpawnArea.GlobalPosition;
                break;
            case 1:
                var depth2 = GetZDepth(camera, mobSpawnArea.GlobalPosition + mobSpawnArea.NodeSecondaryPoint);
                mobSpawnArea.NodeSecondaryPoint = camera.ProjectPosition(screenPos, depth2) - mobSpawnArea.GlobalPosition;
                break;
        }
    }

    public override string _GetGizmoName()
    {
        return nameof(MobSpawnAreaGizmo);
    }

    public override bool _HasGizmo(Node3D forNode3D)
    {
        return forNode3D is MobSpawnArea;
    }

    private static float GetZDepth(Camera3D camera, Vector3 position)
    {
        Vector3 cameraPosition = camera.GlobalPosition;
        Vector3 cameraForward = -camera.GlobalTransform.Basis.Z;
        Vector3 vectorToPosition = position - cameraPosition;
        float zDepth = vectorToPosition.Dot(cameraForward.Normalized());
        return zDepth;
    }
}
#endif