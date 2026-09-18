namespace Shooter.Ui;

using Game;

public partial class HudPlayerNames : Control
{
    [Export]
    private Label refLabel;

    private Label copiedLabel;
    private Dictionary<Player, Label> playerList = [];

    public override void _Ready()
    {
        Global.NetworkManager.Singleton.PlayerConnected += OnPlayerConnected;
        Global.NetworkManager.Singleton.PlayerDisconnected += OnPlayerDisconnected;
        copiedLabel = (Label)refLabel.Duplicate();
        refLabel.Free();
    }

    public override void _Process(double delta)
    {
        foreach (var kvp in playerList)
        {
            var camera = Player.Self.Camera;
            var playerPos = kvp.Key.Position;
            if (camera.IsPositionBehind(playerPos) || kvp.Key.IsDead)
            {
                kvp.Value.Visible = false;
                continue;
            }

            var unproject = Player.Self.Camera.UnprojectPosition(kvp.Key.Position + Vector3.Up * 2f);
            kvp.Value.Position = unproject + new Vector2(-250f, 0f);
            kvp.Value.Visible = true;
        }
    }

    private async void OnPlayerConnected(int peerId, Godot.Collections.Dictionary<string, string> info)
    {
        if (peerId == Player.Self.Id) return;
        // OnPlayerConnected gets called for self before HudPlayerNames _Ready is called
        if (!IsInstanceValid(this)) await Task.Delay(500);

        var label = (Label)copiedLabel.Duplicate();
        label.Text = info["name"];
        AddChild(label);
        playerList.Add(Player.FindPlayer(peerId), label);
    }

    private void OnPlayerDisconnected(int peerId)
    {
        var found = playerList.First(c => c.Key.Id == peerId);
        found.Value.QueueFree();
        playerList.Remove(found.Key);
    }
}
