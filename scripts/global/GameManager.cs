global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;
global using Godot;

namespace Shooter.Global;

public partial class GameManager : Node
{
    public static GameManager Singleton { get; private set; }

    public static Version GameVersion { get; private set; } = Version.FromString("0.1.0");

    public static bool ChangingScene { get; set; } = false;
    public static Node ClearOnLoad { get; private set; }

    // static signals do not exist, must be on a singleton class instead
    [Signal]
    public delegate void MobOnDamageEventHandler(Godot.Collections.Dictionary<string, Variant> damageInfoVariant, int mobPoolId);
    [Signal]
    public delegate void MobOnDeathEventHandler(Godot.Collections.Dictionary<string, Variant> damageInfoVariant, int mobPoolId);

    public override void _EnterTree()
    {
        if (IsInstanceValid(Singleton)) return;
        Singleton = this;

        ResourceManager.Ready();

        ClearOnLoad = new Node() { Name = "ClearOnLoad" };
        GetTree().Root.CallDeferred("add_child", ClearOnLoad);
    }

    public static void Clear_ClearOnLoad()
    {
        foreach (var child in ClearOnLoad.GetChildren()) child.QueueFree();
        DecalManager.Clear();
    }
}
