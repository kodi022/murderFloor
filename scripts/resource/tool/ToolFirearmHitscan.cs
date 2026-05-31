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

    public override void FireBullet(FireInfo fi)
    {
        fi.Player.CameraRotationKick += new Vector3(CameraRotationKick.X, 0, CameraRotationKick.Y);
        fi.Player.ViewModelPositionKick += new Vector3(0, ViewmodelPositionKick.X, ViewmodelPositionKick.Y);
        fi.Player.ViewModelRotationKick += new Vector3(ViewmodelRotationKick.X, 0, ViewmodelRotationKick.Y);
        fi.Player.ViewPitch += AimShift.X;
        fi.Player.ViewYaw += AimShift.Y;

        Rng.Randomize();
        for (int i = 0; i < PelletCount; i++)
        {
            float yaw = Mathf.DegToRad(Rng.RandfRange(-fi.LiveTool.CurrentSpread.X, fi.LiveTool.CurrentSpread.X));
            float pitch = Mathf.DegToRad(Rng.RandfRange(-fi.LiveTool.CurrentSpread.Y, fi.LiveTool.CurrentSpread.Y));

            // normalize then scale back down to make circular
            Vector3 angle = new Vector3(Mathf.Abs(pitch), Mathf.Abs(yaw), 0).Normalized();
            Vector3 dir = fi.ViewForward;
            dir = dir.Rotated(fi.ViewTransform.Basis.Y.Normalized(), angle.Y * yaw);
            dir = dir.Rotated(fi.ViewTransform.Basis.X.Normalized(), angle.X * pitch);

            var space = fi.Player.GetWorld3D().DirectSpaceState;
            var query = PhysicsRayQueryParameters3D.Create(fi.StartPosition, fi.StartPosition + dir * MaxRange);
            var ray = space.IntersectRay(query);
            if (ray.ContainsKey("collider"))
            {
                Global.DebugDot(fi.Player, (Vector3)ray["position"]);

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
                        var rangeFalloffNormalized = 1 - ((nearSqr - distanceSqr) / (nearSqr - farSqr));
                        damage = Damages.Y + (Damages.X - Damages.Y) * rangeFalloffNormalized;
                    }
                }

                var godotObject = (GodotObject)ray["collider"];
                if (godotObject is Pawn pawn)
                {
                    // ! collect damages and send single Rpc, maybe comma separated values
                    var di = new Godot.Collections.Dictionary<string, string>()
                    {
                        {"damage", damage.ToString("0.00")},
                        {"attacker", fi.Player.GetMultiplayerAuthority().ToString()},
                        {"attackerName", NetworkManager.Current._players[fi.Player.GetMultiplayerAuthority()]["Name"]},
                        {"weapon", "mind"},
                        {"hitposition", ((Vector3)ray["position"]).ToString()},
                        {"hitbox", "0"}
                    };
                    pawn.Rpc("OnDamageRpc", di);
                }
            }
        }
    }
}