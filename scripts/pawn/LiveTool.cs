using System.Threading.Tasks;

namespace MurderFloor;

public partial class LiveTool : Node
{
    [Export]
    public int PlayerId { get; set; }
    [Export]
    public string ToolId { get; set; }

    // reference to player
    public Player Player { get; private set; }

    // reference to tool
    public Tool ToolResource { get; private set; }

    // public Godot.Collections.Dictionary<string, string> AttachmentConfig { get; set; }
    // public Godot.Collections.Dictionary<string, string> ModifierConfig { get; set; }

    private MeshInstance3D meshInstance3D;

    public override void _Ready()
    {
        Player = Player.FindPlayer(PlayerId);
        ToolResource = ResourceManager.ToolRegistry.GetResourceReference(ToolId);
    }

    public async Task Equip()
    {
        foreach (var child in Player.ViewToolPosition.GetChildren())
        {
            child.Free();
        }

        Player.ViewToolPosition.AddChild(ToolResource.MeshScene.Instantiate());

        // await equip animation
    }

    public async Task Unequip()
    {
        // await unequip animation
    }

    public void FirePrimary(Vector3 cameraPos, Vector3 cameraForward)
    {
        var fi = new Tool.FireInfo()
        {
            Player = Player.Self,
            StartPosition = cameraPos,
            CameraForward = cameraForward
        };

        ToolResource.FirePrimary(fi);
    }

    public void FireSecondary() { }//=> ToolResource.FireSecondary();
    public void FireReload() { }//=> ToolResource.FireReload();
}