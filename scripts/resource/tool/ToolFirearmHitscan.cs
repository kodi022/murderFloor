namespace MurderFloor;

[GlobalClass]
public partial class ToolFirearmHitscan : ToolFirearm
{
    [Export]
    public Vector2 Damages { get; private set; } = new Vector2(10f, 4f);
    [Export]
    public Vector2 FalloffRanges { get; private set; } = new Vector2(40f, 50f);
    [Export]
    public float MaxRange { get; private set; } = 80f;
    [Export]
    public Vector2 DegreeSpread { get; private set; } = new Vector2(3f, 3f);

    public override void FirePrimary(FireInfo fi)
    {
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        float yawDeg = rng.RandfRange(-DegreeSpread.X, DegreeSpread.X);
        float pitchDeg = rng.RandfRange(-DegreeSpread.Y, DegreeSpread.Y);
        float yaw = Mathf.DegToRad(yawDeg);
        float pitch = Mathf.DegToRad(pitchDeg);

        Vector3 dir = fi.CameraForward.Normalized();
        dir = dir.Rotated(Vector3.Up, yaw);
        Vector3 right = dir.Cross(Vector3.Up).Normalized();
        dir = dir.Rotated(right, pitch);
        dir = dir.Normalized();

        var space = fi.Player.GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(fi.StartPosition, fi.StartPosition + dir * MaxRange);
        var ray = space.IntersectRay(query);

        if (ray.ContainsKey("collider"))
        {
            var debugDecal = (Node3D)GD.Load<PackedScene>("res://scenes/debug/DebugBulletDecal.tscn").Instantiate();
            debugDecal.GlobalPosition = (Vector3)ray["position"];
            fi.Player.GetTree().Root.AddChild(debugDecal);

            var di = new Godot.Collections.Dictionary<string, string>()
            {
                {"attacker", fi.Player.GetMultiplayerAuthority().ToString()},
                {"attackerName", NetworkManager.Current._players[fi.Player.GetMultiplayerAuthority()]["Name"]},
                {"weapon", "mind"},
                {"hitposition", ((Vector3)ray["position"]).ToString()},
                {"hitbox", "0"}
            };
            //pawn.Rpc("OnDamage", di);
        }
    }
}