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

    // ! set up
    protected int CurrentMag = 0;

    private ulong rpmAsMs = 0;
    private ulong msSinceFire = 0;

    public override void FirePrimary(FireInfo fi)
    {
        if (rpmAsMs == 0) rpmAsMs = (ulong)(60f / RPM * 1000f);

        var ticksMs = Time.GetTicksMsec();
        if (rpmAsMs < ticksMs - msSinceFire)
        {
            msSinceFire = ticksMs;
            FireBullet(fi);
            return;
        }
    }

    private void FireBullet(FireInfo fi)
    {
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        float yaw = Mathf.DegToRad(rng.RandfRange(-DegreeSpread.X, DegreeSpread.X));
        float pitch = Mathf.DegToRad(rng.RandfRange(-DegreeSpread.Y, DegreeSpread.Y));

        // normalize then scale back down to degrees to make circular
        Vector3 angle = new Vector3(Mathf.Abs(pitch), Mathf.Abs(yaw), 0).Normalized();
        Vector3 dir = fi.CameraForward;
        dir = dir.Rotated(Vector3.Up, angle.Y * yaw);
        Vector3 right = dir.Cross(Vector3.Up).Normalized();
        dir = dir.Rotated(right, angle.X * pitch);

        // raw degree math for debug
        // Vector3 dir = fi.CameraForward;
        // dir = dir.Rotated(Vector3.Up, yaw);
        // Vector3 right = dir.Cross(Vector3.Up).Normalized();
        // dir = dir.Rotated(right, pitch);

        var space = fi.Player.GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(fi.StartPosition, fi.StartPosition + dir * MaxRange);
        var ray = space.IntersectRay(query);
        if (ray.ContainsKey("collider"))
        {
            var debugDecal = (Node3D)GD.Load<PackedScene>("res://scenes/debug/DebugBulletDecal.tscn").Instantiate();
            debugDecal.Position = (Vector3)ray["position"];
            fi.Player.GetTree().Root.AddChild(debugDecal);

            var pos = (Vector3)ray["position"];
            var distanceSqr = pos.DistanceSquaredTo(fi.StartPosition);
            var nearSqr = FalloffRanges.X * FalloffRanges.X;
            var farSqr = FalloffRanges.Y * FalloffRanges.Y;
            var damage = Damages.X;
            if (distanceSqr > nearSqr)
            {
                if (distanceSqr > farSqr)
                {
                    damage = Damages.Y;
                }
                else
                {
                    var rangeFalloffNormalized = farSqr * (farSqr - distanceSqr);
                    damage = Damages.Y + (Damages.X - Damages.Y) * rangeFalloffNormalized;
                }
            }

            var godotObject = (GodotObject)ray["collider"];
            if (godotObject is Pawn pawn)
            {
                var di = new Godot.Collections.Dictionary<string, string>()
                {
                    {"damage", damage.ToString()},
                    {"attacker", fi.Player.GetMultiplayerAuthority().ToString()},
                    {"attackerName", NetworkManager.Current._players[fi.Player.GetMultiplayerAuthority()]["Name"]},
                    {"weapon", "mind"},
                    {"hitposition", ((Vector3)ray["position"]).ToString()},
                    {"hitbox", "0"}
                };
                pawn.Rpc("OnDamage", di);
            }
        }
    }
}