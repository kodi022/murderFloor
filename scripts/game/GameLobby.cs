namespace Shooter.Game;

// - terminology help
// Game is multiple Waves
// Wave is multiple Groups until a wave spawn count is reached
// Group is a count of mobs spawned in one location simultaneously

public partial class GameLobby : Node
{
    public static GameLobby Current { get; private set; }

    [Export]
    public Area3D ExitArea { get; private set; }

    public override void _EnterTree()
    {
        Current = this;
    }

    public override void _Ready()
    {
        ExitArea.BodyEntered += OnBodyEntered;
        ExitArea.BodyExited += OnBodyExited;
    }

    public void OnBodyEntered(Node3D body)
    {
        if (body is not Player player) return;

        GD.Print(player);
        //if (selectedMap is null) return;

        // ! if everybody is ready
        //Global.NetworkManager.Singleton.Rpc("LoadGame", selectedMap.MeshScene.ResourcePath);
    }

    public void OnBodyExited(Node3D body)
    {
        if (body is not Player player) return;
    }
}