namespace MurderFloor;

public partial class LiveTool : Node3D
{
    // reference to tool
    public Tool ToolResource { get; set; }

    public Godot.Collections.Dictionary<string, string> AttachmentConfig { get; set; }
    public Godot.Collections.Dictionary<string, string> ModifierConfig { get; set; }

    public override void _Ready()
    {
        base._Ready();
    }

    public void FirePrimary()
    {
        ToolResource.FirePrimary();
    }

    public void FireSecondary() => ToolResource.FireSecondary();
    public void FireReload() => ToolResource.FireReload();
}