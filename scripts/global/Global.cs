global using Godot;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;

namespace MurderFloor;

public partial class Global : Node
{
    public static Global Singleton { get; private set; }

    public static Version GameVersion { get; private set; } = Version.FromString("0.1.0");
    public static readonly Texture2D MissingTexture = GD.Load<Texture2D>("res://images/missing.png");
    public static readonly ImageTexture MissingTextureImage = ImageTexture.CreateFromImage(GD.Load<Texture2D>("res://images/missing.png").GetImage());

    public static System.Text.Json.JsonSerializerOptions JsonOptions { get; private set; } = new()
    { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = false };

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

    public static string ButtonName(string actionName)
    {
        var inputText = InputMap.ActionGetEvents(actionName)[0].AsText();
        return inputText.Split(' ')[0]; // possible examples = "Escape" or "W - Physical"
    }
}
