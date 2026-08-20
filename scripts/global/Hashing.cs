namespace MurderFloor;

public static class Hashing
{
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

    public static int StableHash(Vector2 vec)
    {
        return StableHash(vec.X, vec.Y, 0f);
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

    public static int StableHash(Vector3I vec)
    {
        return StableHash(vec.X, vec.Y, vec.Z);
    }

    public static int StableHash(Vector2I vec)
    {
        return StableHash(vec.X, vec.Y, 0);
    }
}