namespace Shooter.Global;

public static class DecalManager
{
    public static List<Decal> Decals { get; private set; } = [];
    private static int decalIndex = 0;

    private static Node parent;

    public static void Ready()
    {
        if (!GodotObject.IsInstanceValid(parent))
        {
            parent = new Node() { Name = "NodePool" };
            GameManager.ClearOnLoad.AddChild(parent);
        }

        for (int i = 0; i < OptionsManager.CurrentOptions.DecalMaximum; i++)
        {
            var decal = new Decal();
            Decals.Add(decal);
            parent.AddChild(decal);
            decal.Name = "Decal_" + Decals.Count;
            decal.Visible = false;
        }
    }

    public static void Clear()
    {
        foreach (var decal in Decals)
            if (GodotObject.IsInstanceValid(decal)) decal.Free();
        Decals = [];
        decalIndex = 0;
    }

    public static void UpdateNext(Vector3 position, Vector3 rotation, string texture, Vector3 size)
    {
        if (Decals.Count == 0) return;

        var decal = Decals[decalIndex];
        decal.GlobalPosition = position;
        decal.GlobalRotation = rotation;
        decal.Size = size;
        decal.Visible = true;
        decal.TextureAlbedo = GD.Load<Texture2D>(texture);

        decalIndex = (decalIndex + 1) % Decals.Count;
    }

    public static void UpdateNext(Node newParent, Vector3 position, Vector3 rotation, string texture, Vector3 size)
    {
        if (Decals.Count == 0) return;

        var decal = Decals[decalIndex];
        decal.Owner = null;
        decal.Reparent(newParent);
        decal.Owner = newParent;
        decal.ResetPhysicsInterpolation();
        decal.GlobalPosition = position;
        decal.GlobalRotation = rotation;
        decal.Size = size;
        decal.Visible = true;
        decal.TextureAlbedo = GD.Load<Texture2D>(texture);

        decalIndex = (decalIndex + 1) % Decals.Count;
    }
}