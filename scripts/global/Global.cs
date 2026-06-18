global using Godot;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;

namespace MurderFloor;

public static class Global
{
    public static readonly Texture2D MissingTexture = GD.Load<Texture2D>("res://images/missing.png");
    public static readonly ImageTexture MissingTextureImage = ImageTexture.CreateFromImage(GD.Load<Texture2D>("res://images/missing.png").GetImage());

    public static int StableHash(string s)
    {
        if (string.IsNullOrEmpty(s))
            return 0;

        unchecked
        {
            int hash = 2116037303;
            foreach (var ch in s)
            {
                hash = (hash ^ ch) * 971296439;
            }
            return hash;
        }
    }

    public static int StableHash(int x, int y, int z)
    {
        unchecked
        {
            int hash = x * 13466917;
            hash = hash * 2119412839 + y;
            hash = hash * 135040691 + z;
            return hash;
        }
    }

    public static void DebugDot(Node anyNode, Vector3 position, float scale = 1f, Color? color = null, ulong msToDelete = 10000ul)
    {
        var debugDot = (Node3D)GD.Load<PackedScene>("res://scenes/debug/DebugBulletDecal.tscn").Instantiate();
        debugDot.Position = position;
        debugDot.Scale = Vector3.One * scale;

        var debugBulletDecal = (DebugBulletDecal)debugDot;
        debugBulletDecal.MsToDelete = msToDelete;

        if (color is not null && debugBulletDecal.GetActiveMaterial(0) is StandardMaterial3D shared)
        {
            var inst = (StandardMaterial3D)shared.Duplicate(true);
            inst.AlbedoColor = (Color)color;
            debugBulletDecal.MaterialOverride = inst;
        }

        anyNode.GetTree().Root.AddChild(debugDot);
    }
}