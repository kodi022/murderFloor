namespace MurderFloor;

public partial class NetworkManager : Node
{
    public static NetworkManager Current;
    // https://docs.godotengine.org/en/stable/tutorials/networking/high_level_multiplayer.html

    private const int DefaultPort = 7000;
    private const string DefaultServerIP = "127.0.0.1"; // IPv4 localhost
    private const int MaxConnections = 4;

    public string ServerIP { get; set; } = default;
    public int Port { get; set; } = default;

    public bool Playing { get; set; } = false;

    // servers ID is always 1
    // server is at Multiplayer.MultiplayerPeer

    // These signals can be connected to by a UI lobby scene or the game scene.
    [Signal]
    public delegate void PlayerConnectedEventHandler(int peerId, Godot.Collections.Dictionary<string, string> playerInfo);
    [Signal]
    public delegate void PlayerDisconnectedEventHandler(int peerId);
    [Signal]
    public delegate void ServerDisconnectedEventHandler();

    private int _playersLoaded = 0;

    // This will contain player info for every player,
    // with the keys being each player's unique IDs.
    public Godot.Collections.Dictionary<long, Godot.Collections.Dictionary<string, string>> _players = new Godot.Collections.Dictionary<long, Godot.Collections.Dictionary<string, string>>();

    // This is the local player info. This should be modified locally
    // before the connection is made. It will be passed to every other peer.
    // For example, the value of "name" can be set to something the player
    // entered in a UI scene.
    public Godot.Collections.Dictionary<string, string> _playerInfo = new()
    {
        { "Name", "PlayerName" },
        { "Coolness", "3" },
    };

    public override void _EnterTree()
    {
        Current = this;
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
    // do Rpc(Lobby.MethodName.LoadGame, filePath);
    [Rpc(CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void LoadGame(string gameScenePath)
    {
        GetTree().ChangeSceneToFile(gameScenePath);
    }

    // ! WHERE DO I SPAWN PLAYERS FOR CLIENTS

    // Emitted when this MultiplayerAPI's MultiplayerApi.MultiplayerPeer connects with a new peer. 
    // ID is the peer ID of the new peer. 
    // Clients get notified when other clients connect to the same server. 
    // Upon connecting to a server, a client also receives this signal for the server (with ID being 1).
    private void OnPeerConnected(long id)
    {
        GD.Print("OnPeerConnected " + id);

        RpcId(id, "SendInfoToPeer", _playerInfo);
        _players[id] = _playerInfo;
        var player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate();
        player.Name = id.ToString();
        player.SetMultiplayerAuthority((int)id);
        AddChild(player);
    }

    // Emitted when this MultiplayerAPI's MultiplayerApi.MultiplayerPeer disconnects from a peer. 
    // Clients get notified when other clients disconnect from the same server.
    private void OnPeerDisconnected(long id)
    {
        GD.Print("OnPeerDisconnected " + id);
        _players.Remove(id);
    }

    // Emitted when this MultiplayerAPI's MultiplayerApi.MultiplayerPeer successfully connected to a server. 
    // Only emitted on clients.
    private void OnConnectedToServer()
    {
        GD.Print("OnConnectedToServer");

        int id = Multiplayer.GetUniqueId();
        _players[id] = _playerInfo;
        var player = GD.Load<PackedScene>("res://scenes/Player.tscn").Instantiate();
        player.Name = id.ToString();
        player.SetMultiplayerAuthority((int)id);
        AddChild(player);
    }

    // Emitted when this MultiplayerAPI's MultiplayerApi.MultiplayerPeer fails to establish a connection to a server. 
    // Only emitted on clients.
    private void OnConnectionFailed()
    {
        // ! return to menu if not in
        GD.Print("OnConnectionFailed");
        Multiplayer.MultiplayerPeer = null;
    }

    // Emitted when this MultiplayerAPI's MultiplayerApi.MultiplayerPeer disconnects from server. 
    // Only emitted on clients.
    private void OnServerDisconnected()
    {
        GD.Print("OnServerDisconnected");
        // ! return to menu if not in
        Multiplayer.MultiplayerPeer = null;
        _players.Clear();
    }

    [Rpc(TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SpawnPeersSelf(Godot.Collections.Dictionary<string, string> newPlayerInfo)
    {
        int newPlayerId = Multiplayer.GetRemoteSenderId();
        _players[newPlayerId] = newPlayerInfo;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SendInfoToPeer(Godot.Collections.Dictionary<string, string> newPlayerInfo)
    {
        int newPlayerId = Multiplayer.GetRemoteSenderId();
        _players[newPlayerId] = newPlayerInfo;
    }
}