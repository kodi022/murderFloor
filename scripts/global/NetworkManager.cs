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

    // servers ID is always 1
    // server is at Multiplayer.MultiplayerPeer

    // This will contain player info for every player,
    // with the keys being each player's unique IDs.
    public Godot.Collections.Dictionary<long, Godot.Collections.Dictionary<string, string>> _players = new Godot.Collections.Dictionary<long, Godot.Collections.Dictionary<string, string>>();

    // This is the local player info. This should be modified locally
    // before the connection is made. It will be passed to every other peer.
    // For example, the value of "name" can be set to something the player
    // entered in a UI scene.
    public Godot.Collections.Dictionary<string, string> _playerInfo = new()
    {
        { "name", "Convict" },
        { "coolness", "1" },
    };

    private int _playersLoaded = 0;

    public override void _EnterTree()
    {
        if (IsInstanceValid(Singleton)) return;
        Singleton = this;

        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        Multiplayer.ConnectedToServer += OnConnectedToServer;
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

    public Error CreateServer(bool lan = false)
    {
        var peer = new ENetMultiplayerPeer();
        Error error = peer.CreateServer(Port, lan ? 1 : MaxConnections);

        if (error != Error.Ok) return error;

        Multiplayer.MultiplayerPeer = peer;
        _players[1] = _playerInfo;
        OnConnectedToServer();

        return Error.Ok;
    }

    // used to shut down server or leave server depending on context
    public void CloseServer()
    {
        Multiplayer.MultiplayerPeer = null;
        _players.Clear();
    }

    // When the server decides to start the game from a UI scene,
    // do Rpc(Lobby.MethodName.LoadGameRpc, filePath);
    [Rpc(CallLocal = true)]
    public void LoadGameRpc(string gameScenePath)
    {
        _playersLoaded = 0;
        //GameManager.Clear_ClearOnLoad();
        GetTree().ChangeSceneToFile(gameScenePath);
    }

    // Emitted when this MultiplayerAPI's MultiplayerApi.MultiplayerPeer connects with a new peer. 
    // ID is the peer ID of the new peer. 
    // Clients get notified when other clients connect to the same server. 
    // Upon connecting to a server, a client also receives this signal for the server (with ID being 1).
    private void OnPeerConnected(long id)
    {
        GD.Print("OnPeerConnected " + id);

        RpcId(id, MethodName.SendInfoToPeerRpc, _playerInfo);

        _players[id] = _playerInfo;
        var player = GD.Load<PackedScene>("res://scenes/pawn/player/Player.tscn").Instantiate<Node3D>();
        player.Name = "plr_" + id.ToString();
        player.Position = new Vector3(0, 0.3f, 0);
        player.SetMultiplayerAuthority((int)id);
        GameManager.ClearOnLoad.AddChild(player);
    }

    // Emitted when this MultiplayerAPI's MultiplayerApi.MultiplayerPeer successfully connected to a server. 
    // Only emitted on clients.
    private void OnConnectedToServer()
    {
        GD.Print("OnConnectedToServer");

        int id = Multiplayer.GetUniqueId();
        _players[id] = _playerInfo;
        var player = GD.Load<PackedScene>("res://scenes/pawn/player/Player.tscn").Instantiate<Node3D>();
        player.Name = "plr_" + id.ToString();
        player.Position = new Vector3(0, 0.3f, 0);
        player.SetMultiplayerAuthority((int)id);
        GameManager.ClearOnLoad.AddChild(player);
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
        _players.Remove(id);
        EmitSignal(SignalName.PlayerDisconnected, id);
    }

    // Emitted when this MultiplayerAPI's MultiplayerApi.MultiplayerPeer fails to establish a connection to a server. 
    // Only emitted on clients.
    private void OnConnectionFailed()
    {
        GD.Print("OnConnectionFailed");
        Multiplayer.MultiplayerPeer = null;
        Rpc(MethodName.LoadGameRpc, "res://scenes/MainMenu.tscn");
    }

    // Emitted when this MultiplayerAPI's MultiplayerApi.MultiplayerPeer disconnects from server. 
    // Only emitted on clients.
    private void OnServerDisconnected()
    {
        GD.Print("OnServerDisconnected");
        Multiplayer.MultiplayerPeer = null;
        _players.Clear();
        Rpc(MethodName.LoadGameRpc, "res://scenes/MainMenu.tscn");
        EmitSignal(SignalName.ServerDisconnected);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void SendInfoToPeerRpc(Godot.Collections.Dictionary<string, string> playerInfo)
    {
        int playerId = Multiplayer.GetRemoteSenderId();
        _players[playerId] = playerInfo;
        EmitSignal(SignalName.PlayerConnected, playerId, playerInfo);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private async void PlayerLoadedRpc()
    {
        if (Multiplayer.IsServer())
        {
            _playersLoaded += 1;
            if (_playersLoaded == _players.Count)
            {
                await Task.Delay(2000);
                Game.Game.Current?.Rpc(Game.Game.MethodName.StartGameRpc);
                _playersLoaded = 0;
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void ClientPlayerReadyRpc()
    {
        Game.Player.Self.RpcId(Multiplayer.GetRemoteSenderId(), Game.Player.MethodName.ToolsSyncRpc, Game.Player.Self.GetAllTools(1));
    }
}