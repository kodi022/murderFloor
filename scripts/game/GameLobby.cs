namespace Shooter.Game;

using Global;

// - terminology help
// Game is multiple Waves
// Wave is multiple Groups until a wave spawn count is reached
// Group is a count of mobs spawned in one location simultaneously

public partial class GameLobby : Node
{
    public static GameLobby Current { get; private set; }

    [Export]
    public Area3D ExitArea { get; private set; }

    public List<Player> ExitPlayers { get; private set; } = [];
    public double ExitTimer { get; private set; } = 999f;
    private bool startedLoading = false;

    public override void _EnterTree()
    {
        Current = this;
    }

    public override async void _Ready()
    {
        DecalManager.Ready();
        ExitArea.BodyEntered += OnBodyEntered;
        ExitArea.BodyExited += OnBodyExited;

        await Task.Delay(500);
        NetworkManager.Singleton.RpcId(1, NetworkManager.MethodName.PlayerLoadedRpc, Multiplayer.MultiplayerPeer.GetUniqueId());
    }

    public override void _Process(double delta)
    {
        if (ExitTimer == 999f) return;
        ExitTimer -= delta;

        if (ExitTimer <= 0f && !startedLoading)
        {
            startedLoading = true;
            if (NetworkManager.Singleton.IsMultiplayerAuthority())
                NetworkManager.Singleton.Rpc(NetworkManager.MethodName.LoadGameRpc, Ui.MapSelect.SelectedMapNetworked.MeshScene.ResourcePath);
        }
    }

    public void OnBodyEntered(Node3D body)
    {
        if (body is not Player player) return;

        if (Ui.MapSelect.SelectedMapNetworked is null) return;

        ExitPlayers.Add(player);

        if (ExitPlayers.Count == Player.AllPlayers.Count)
            ExitTimer = Mathf.Min(ExitTimer, 5f);
        else if (ExitPlayers.Count == 1)
            ExitTimer = Mathf.Min(ExitTimer, 60f);
        else if (ExitPlayers.Count == 2)
            ExitTimer = Mathf.Min(ExitTimer, 30f);
        else if (ExitPlayers.Count > 2)
            ExitTimer = Mathf.Min(ExitTimer, 20f);
    }

    public void OnBodyExited(Node3D body)
    {
        if (body is not Player player) return;

        ExitPlayers.Remove(player);

        if (ExitPlayers.Count == 0)
            ExitTimer = 999f;
    }
}