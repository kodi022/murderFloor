global using Godot;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;

namespace Shooter.Global;

public partial class GameManager : Node
{
    public static GameManager Singleton { get; private set; }

    public static Version GameVersion { get; private set; } = Version.FromString("0.1.0");

    public static Node KeepOnLoad { get; private set; }
    public static Node ClearOnLoad { get; private set; }

    public override void _EnterTree()
    {
        if (IsInstanceValid(Singleton)) return;
        Singleton = this;

        ResourceManager.Ready();

        KeepOnLoad = new Node() { Name = "KeepOnLoad" };
        GetTree().Root.CallDeferred("add_child", KeepOnLoad);

        ClearOnLoad = new Node() { Name = "ClearOnLoad" };
        GetTree().Root.CallDeferred("add_child", ClearOnLoad);
    }

    public static void Clear_ClearOnLoad()
    {
        foreach (var child in ClearOnLoad.GetChildren()) child.QueueFree();
    }
}
