namespace Shooter.Utils;

public static class Defaults
{
    public static readonly Texture2D MissingTexture = GD.Load<Texture2D>("res://images/missing.png");
    public static readonly ImageTexture MissingTextureImage = ImageTexture.CreateFromImage(GD.Load<Texture2D>("res://images/missing.png").GetImage());

    public static System.Text.Json.JsonSerializerOptions JsonOptions { get; private set; } = new()
    { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = false };

    // i cant find a better location for this
    public static string ButtonName(string actionName)
    {
        var inputText = InputMap.ActionGetEvents(actionName)[0].AsText();
        return inputText.Split(' ')[0]; // possible examples = "Escape" or "W - Physical"
    }
}