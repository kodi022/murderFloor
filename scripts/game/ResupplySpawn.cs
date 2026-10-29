namespace Shooter.Game;

public partial class ResupplySpawn : Node3D
{
    [Export]
    public Node3D MapRoofClosedNode { get; private set; }
    [Export]
    public Node3D MapRoofOpenNode { get; private set; }
    [Export]
    public AudioStreamMP3 RoofOpenSound { get; private set; }

    public void SpawnResupply()
    {
        var resupply = GD.Load<PackedScene>("res://scenes/pickup/Resupply.tscn");
        var inst = (Node3D)resupply.Instantiate();
        Global.GameManager.ClearOnLoad.AddChild(inst);
        inst.GlobalPosition = GlobalPosition;
    }
}