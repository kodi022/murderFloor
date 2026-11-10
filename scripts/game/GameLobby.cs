namespace Shooter.Game;

using Global;

public partial class GameLobby : Game
{
    public override async void _Ready()
    {
        base._Ready();
        DecalManager.Ready();
        ExitArea.BodyEntered += OnBodyEntered;
        ExitArea.BodyExited += OnBodyExited;
    }

    public override void _Process(double delta)
    {
        if (ExitTimer == 999f) return;
        ExitTimer -= delta;

        if (ExitTimer <= 0f && !GameManager.ChangingScene)
        {
            GameManager.ChangingScene = true;
            if (NetworkManager.Singleton.IsMultiplayerAuthority())
                NetworkManager.Singleton.Rpc(NetworkManager.MethodName.LoadGameRpc, Ui.MapSelect.SelectedMapNetworked.MeshScene.ResourcePath, false);
        }
    }

    public void OnBodyEntered(Node3D body)
    {
        if (body is not Player player) return;

        if (Ui.MapSelect.SelectedMapNetworked is null) return;

        ExitPlayers.Add(player);

        if (ExitPlayers.Count == Player.AllPlayers.Count)
            ExitTimer = Mathf.Min(ExitTimer, 5f);
        else if (ExitPlayers.Count == Player.AllPlayers.Count - 1)
            ExitTimer = Mathf.Min(ExitTimer, 20f);
        else if (ExitPlayers.Count == Player.AllPlayers.Count - 2)
            ExitTimer = Mathf.Min(ExitTimer, 30f);
        else if (ExitPlayers.Count > 0)
            ExitTimer = Mathf.Min(ExitTimer, 60f);
    }

    public void OnBodyExited(Node3D body)
    {
        if (body is not Player player) return;

        ExitPlayers.Remove(player);

        if (ExitPlayers.Count == 0)
            ExitTimer = 999f;
    }
}