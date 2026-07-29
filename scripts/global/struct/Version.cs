namespace MurderFloor;

public struct Version
{
    public int Major { get; private set; } = 0;
    public int Minor { get; private set; } = 0;
    public int Patch { get; private set; } = 0;

    public Version(int major, int minor, int patch)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    public override string ToString()
    {
        return $"{Major}.{Minor}.{Patch}";
    }

    public static Version FromString(string versionStr)
    {
        var vals = versionStr.Split('.');
        if (vals.Length != 3)
        {
            GD.PushWarning("Version.FromString got invalid string");
            return new Version(0, 1, 0);
        }
        return new Version(vals[0].ToInt(), vals[1].ToInt(), vals[2].ToInt());
    }

    /// <summary> Returns false if given version is equal or less than </summary>
    public bool IsGreaterThan(Version ver)
    {
        if (Major == ver.Major)
        {
            if (Minor == ver.Minor) return Patch > ver.Patch;
            else return Minor > ver.Minor;
        }
        else return Major > ver.Major;
    }
}