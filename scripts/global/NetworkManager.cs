namespace Shooter.Global;

// https://docs.godotengine.org/en/stable/tutorials/networking/high_level_multiplayer.html
public partial class NetworkManager : Node
{
    private const int DefaultPort = 7000;
    private const string DefaultServerIP = "127.0.0.1"; // IPv4 localhost
    private const int MaxConnections = 4;

    public static NetworkManager Singleton { get; private set; }

    public string ServerIP { get; set; } = default;
    public int Port { get; set; } = default;

    // These signals can be connected to by a UI lobby scene or the game scene.
    [Signal]
    public delegate void PlayerConnectedEventHandler(int peerId, Godot.Collections.Dictionary<string, string> playerInfo);
    [Signal]
    public delegate void PlayerDisconnectedEventHandler(int peerId);
    [Signal]
    public delegate void ServerDisconnectedEventHandler();

    public static bool AllPlayersLoaded => Singleton._playersLoaded == Singleton.Players.Count;

    // servers ID is always 1
    // server is at Multiplayer.MultiplayerPeer

    // This will contain player info for every player,
    // with the keys being each player's unique IDs.
    public Godot.Collections.Dictionary<long, Godot.Collections.Dictionary<string, string>> Players = [];

    // This is the local player info. This should be modified locally
    // before the connection is made. It will be passed to every other peer.
    // For example, the value of "name" can be set to something the player
    // entered in a UI scene.
    public Godot.Collections.Dictionary<string, string> PlayerInfo = new()
    {
        { "name", "Convict" },
        { "coolness", "1" },
    };

    private int _playersLoaded = 0;

    public override void _EnterTree()
    {
        if (IsInstanceValid(Singleton)) return;
        Singleton = this;

        Multiplayer.ConnectedToServer += OnConnectedToServer;
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        Multiplayer.ConnectionFailed += OnConnectionFailed;
        Multiplayer.ServerDisconnected += OnServerDisconnected;
    }

    public Error JoinServer()
    {
        string IP = ServerIP;
        if (IP == default)
        {
            IP = DefaultServerIP;
        }

        int port = Port;
        if (port == default)
        {
            port = DefaultPort;
        }

        var peer = new ENetMultiplayerPeer();
        Error error = peer.CreateClient(IP, port);

        if (error != Error.Ok) return error;

        Multiplayer.MultiplayerPeer = peer;
        return Error.Ok;
    }

    public Error CreateServer(bool lan = true)
    {
        var peer = new ENetMultiplayerPeer();
        Error error = peer.CreateServer(Port, lan ? 1 : MaxConnections);

        if (error != Error.Ok) return error;

        Multiplayer.MultiplayerPeer = peer;
        Players[1] = PlayerInfo;
        OnConnectedToServer();

        return Error.Ok;
    }

    // used to shut down server or leave server depending on context
    public void CloseServer()
    {
        Multiplayer.MultiplayerPeer = null;
        Players.Clear();
    }

    // When the server decides to start the game from a UI scene,
    // do Rpc(Lobby.MethodName.LoadGameRpc, filePath, false);
    [Rpc(CallLocal = true)]
    public async void LoadGameRpc(string gameScenePath, bool joiningServer)
    {
        _playersLoaded = 0;
        Game.Player.Self?.CloseUI(); // needed or cursor may stay unlocked
        GameManager.Clear_ClearOnLoad();
        Ui.HudFloatingText.ClearFloatingTexts();
        GetTree().ChangeSceneToFile(gameScenePath);

        if (!joiningServer)
        {
            await Task.Delay(200);
            foreach (var player in Players)
            {
                SpawnPlayer(player.Key);
            }
        }
    }

    // Emitted when this MultiplayerAPI's MultiplayerApi.MultiplayerPeer successfully connected to a server. 
    // Only emitted on clients.
    private void OnConnectedToServer()
    {
        GD.Print("OnConnectedToServer");

        int id = Multiplayer.GetUniqueId();
        Players[id] = PlayerInfo;
        SpawnPlayer(id);
    }

    // Emitted when this MultiplayerAPI's MultiplayerApi.MultiplayerPeer connects with a new peer. 
    // ID is the peer ID of the new peer. 
    // Clients get notified when other clients connect to the same server. 
    // Upon connecting to a server, a client also receives this signal for the server (with ID being 1).
    private void OnPeerConnected(long id)
    {
        GD.Print("OnPeerConnected " + id);

        RpcId(id, MethodName.SendInfoToPeerRpc, PlayerInfo);

        Players[id] = PlayerInfo;
        SpawnPlayer(id);
    }

    // Emitted when this MultiplayerAPI's MultiplayerApi.MultiplayerPeer disconnects from a peer. 
    // Clients get notified when other clients disconnect from the same server.
    private void OnPeerDisconnected(long id)
    {
        GD.Print("OnPeerDisconnected " + id);
        foreach (var p in Game.Player.AllPlayers)
        {
            if (p.Id == id)
            {
                p.QueueFree();
            }
        }
        Players.Remove(id);
        EmitSignal(SignalName.PlayerDisconnected, id);
    }

    // Emitted when this MultiplayerAPI's MultiplayerApi.MultiplayerPeer fails to establish a connection to a server. 
    // Only emitted on clients.
    private void OnConnectionFailed()
    {
        GD.Print("OnConnectionFailed");
        Multiplayer.MultiplayerPeer = null;
        Rpc(MethodName.LoadGameRpc, "res://scenes/MainMenu.tscn", false);
    }

    // Emitted when this MultiplayerAPI's MultiplayerApi.MultiplayerPeer disconnects from server. 
    // Only emitted on clients.
    private void OnServerDisconnected()
    {
        GD.Print("OnServerDisconnected");
        Multiplayer.MultiplayerPeer = null;
        Players.Clear();
        Rpc(MethodName.LoadGameRpc, "res://scenes/MainMenu.tscn", false);
        EmitSignal(SignalName.ServerDisconnected);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void SendInfoToPeerRpc(Godot.Collections.Dictionary<string, string> playerInfo)
    {
        int playerId = Multiplayer.GetRemoteSenderId();
        Players[playerId] = playerInfo;
        EmitSignal(SignalName.PlayerConnected, playerId, playerInfo);
    }

    // CallLocal required, because host is usually a player
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private async void PlayerLoadedRpc(int id)
    {
        if (Multiplayer.IsServer())
        {
            if (Game.Game.IsValidAndOfType(out Game.GameLobby _))
            {
                if (Ui.MapSelect.SelectedMapNetworked is not null)
                {
                    Ui.MapSelect.WorldMapSelect.RpcId(
                        id,
                        Ui.MapSelect.MethodName.MapInfoRpc,
                        Ui.MapSelect.SelectedMapNetworked.FullId,
                        Ui.MapSelect.DifficultyConfigNetworked.Serialize(),
                        Ui.MapSelect.SelectedSeedNetworked,
                        Ui.MapSelect.SelectedButtonLocation
                    );
                }
            }

            _playersLoaded += 1;
            if (AllPlayersLoaded)
            {
                if (IsInstanceValid(Game.Game.Current))
                {
                    Game.Game.Current.Rpc(Game.Game.MethodName.AllLoadedRpc);
                    await Task.Delay(5000);
                    Game.Game.Current.Rpc(Game.Game.MethodName.StartGameRpc);
                }
                _playersLoaded = 0;
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void ClientPlayerReadyRpc()
    {
        Game.Player.Self.RpcId(Multiplayer.GetRemoteSenderId(), Game.Player.MethodName.ToolsSyncRpc, Game.Player.Self.GetAllTools(1));
    }

    private static void SpawnPlayer(long id)
    {
        var player = GD.Load<PackedScene>("res://scenes/pawn/player/Player.tscn").Instantiate<Node3D>();
        player.Name = "plr_" + id.ToString();
        player.Position = new Vector3(0, 0.3f, 0);
        player.SetMultiplayerAuthority((int)id);
        GameManager.ClearOnLoad.CallDeferred("add_child", player); // requires calldeferred, no idea why
    }
}