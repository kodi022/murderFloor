namespace MurderFloor;

public partial class LiveTool : Node3D
{
    public Tool ToolResource { get; set; }

    public Godot.Collections.Dictionary<string, string> AttachmentConfig { get; set; }
    public Godot.Collections.Dictionary<string, string> ModifierConfig { get; set; }

    public void FirePrimary() => ToolResource.FirePrimary();
    public void FireSecondary() => ToolResource.FireSecondary();
    public void FireReload() => ToolResource.FireReload();
}