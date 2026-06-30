global using Godot;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;

/// "damage": float</para>
/// "attacker": "player id OR 0 if mob"</para>
/// "attackerName": "player name OR mob name"</para>
/// "weapon": "weaponresourceid OR empty if mob"</para>
/// "hitposition": vector3</para>
/// "hitbox": id</para>
global using DamageInfo = Godot.Collections.Dictionary<string, Godot.Variant>;

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

    public static int StableHash(float x, float y, float z)
    {
        unchecked
        {
            float hash = x * 13466917;
            hash = hash * 2119412839 + y;
            hash = hash * 135040691 + z;
            return (int)hash;
        }
    }

    public static int StableHash(Vector3 vec)
    {
        return StableHash(vec.X, vec.Y, vec.Z);
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
}