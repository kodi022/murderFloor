namespace Shooter.Game;

using Global;

public partial class Game : Node
{
    public static Game Current { get; private protected set; }

    public static DifficultyConfig DifficultyConfig => Ui.MapSelect.DifficultyConfigNetworked;

    [Signal]
    public delegate void GameStartEventHandler();
    [Signal]
    public delegate void GameWinEventHandler();

    public enum DifficultyEnum
    {
        Easy = 0, Medium = 1, Challenging = 2, Hard = 3, Extreme = 4, Ludicrous = 5,
    }

    [Export]
    public ulong GameSeed { get; private set; } = 12345678;

    [Export]
    public Node3D[] PlayerSpawns { get; private set; }

    [Export]
    public Area3D ExitArea { get; private protected set; }
    public List<Player> ExitPlayers { get; private protected set; } = [];
    public double ExitTimer { get; private protected set; } = 999f;

    /// <summary>Returns null if no Game</summary>
    public static bool IsValidAndOfType<T>(out T game) where T : Game
    {
        if (IsInstanceValid(Current) && Current is T t)
        {
            game = t;
            return true;
        }

        game = null;
        return false;
    }

    public override void _EnterTree()
    {
        Current = this;
    }

    public override async void _Ready()
    {
        await Task.Delay(500);
        NetworkManager.Singleton.RpcId(1, NetworkManager.MethodName.PlayerLoadedRpc, Multiplayer.MultiplayerPeer.GetUniqueId());
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public virtual void AllLoadedRpc()
    {
        GameSeed = Ui.MapSelect.SelectedSeedNetworked;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public virtual void StartGameRpc()
    {
        EmitSignal(SignalName.GameStart);
        DisplayServer.WindowRequestAttention();
    }

    public virtual bool BlockPlayerInput()
    {
        return false;
    }
}