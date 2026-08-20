global using Godot;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading.Tasks;

namespace MurderFloor;

public static class Global
{
    public static Version GameVersion { get; private set; } = Version.FromString("0.1.0");
    public static readonly Texture2D MissingTexture = GD.Load<Texture2D>("res://images/missing.png");
    public static readonly ImageTexture MissingTextureImage = ImageTexture.CreateFromImage(GD.Load<Texture2D>("res://images/missing.png").GetImage());
}
