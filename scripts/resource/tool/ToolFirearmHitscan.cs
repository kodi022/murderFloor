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

    private static readonly Dictionary<string, float> HitboxDamageMultipliers = new()
    {
        ["Head"] = 1.5f,
        ["Neck"] = 1.35f,
        ["Hand"] = 0.8f,
        ["LowerLeg"] = 0.9f,
        ["Foot"] = 0.8f,
    };

    public override void FireBullet(FireInfo fi)
    {
        Rng.Randomize();

        fi.Player.CameraRotationKick += new Vector3(CameraRotationKick.X, 0, CameraRotationKick.Y);
        fi.Player.ViewModelPositionKick += new Vector3(0, ViewmodelPositionKick.X, ViewmodelPositionKick.Y);
        fi.Player.ViewModelRotationKick += new Vector3(ViewmodelRotationKick.X, 0, ViewmodelRotationKick.Y);
        fi.Player.ViewAngle += new Vector2(
            Rng.RandfRange(AimShiftRangeHorizontal.X, AimShiftRangeHorizontal.Y),
            Rng.RandfRange(AimShiftRangeVertical.X, AimShiftRangeVertical.Y)
        );
        fi.Player.CameraShakeScale = ScreenShakeAmount;

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
            var query = PhysicsRayQueryParameters3D.Create(fi.StartPosition, fi.StartPosition + dir * MaxRange, 5);
            var ray = space.IntersectRay(query);
            if (ray.ContainsKey("collider"))
            {
                Debug.DebugDot(fi.Player, (Vector3)ray["position"], color: new Color(0, 0, 0));

                Pawn pawn = null;
                var currentNode = (Node)(GodotObject)ray["collider"];
                for (int j = 0; j < 5; j++)
                {
                    currentNode = currentNode.GetParent();

                    if (currentNode is null) break;
                    if (currentNode is Pawn p)
                    {
                        pawn = p;
                        break;
                    }
                }

                if (pawn is not null)
                {
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

                    var hitObjName = ((Node)(GodotObject)ray["collider"]).GetParent().Name.ToString();
                    damage *= GetHitDamageMultiplier(hitObjName);

                    // ! collect damages and send single Rpc, maybe comma separated values
                    var di = new DamageInfo()
                    {
                        {"damage", damage},
                        {"attacker", fi.Player.Id},
                        {"attackerName", NetworkManager.Current._players[fi.Player.Id]["Name"]},
                        {"weapon", "mind"},
                        {"hitposition", (Vector3)ray["position"]},
                        {"hitbox", "0"}
                    };
                    pawn.Rpc("OnDamageRpc", di);
                }
            }
        }
    }

    private static float GetHitDamageMultiplier(string colliderName)
    {
        foreach (var kvp in HitboxDamageMultipliers)
        {
            if (colliderName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        return 1f;
    }
}