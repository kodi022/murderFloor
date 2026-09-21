namespace Shooter.Resource;

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
        ["Head"] = 1.4f,
        ["Neck"] = 1.25f,
        ["Hand"] = 0.8f,
        ["Foot"] = 0.8f,
    };

    public override void FireBullet(FireInfo fi)
    {
        Rng.Randomize();

        fi.Player.AddCameraRotationKick(new Vector3(CameraRotationKick.X, 0, CameraRotationKick.Y));

        var viewPosKick = new Vector3(0, ViewmodelPositionKick.X, ViewmodelPositionKick.Y);
        var viewRotKick = new Vector3(ViewmodelRotationKick.X, 0, ViewmodelRotationKick.Y);
        if (fi.Tool.Aiming)
        {
            fi.Player.AddViewmodelPositionKick(viewPosKick * 0.5f, 6f);
            fi.Player.AddViewmodelRotationKick(viewRotKick * 0.7f);
            fi.Player.AddCameraShake(ScreenShakeAmount * 0.7f);
        }
        else
        {
            fi.Player.AddViewmodelPositionKick(viewPosKick, 6f);
            fi.Player.AddViewmodelRotationKick(viewRotKick);
            fi.Player.AddCameraShake(ScreenShakeAmount);
        }

        fi.Player.ViewAngle += new Vector2(
            Rng.RandfRange(AimShiftRangeHorizontal.X, AimShiftRangeHorizontal.Y),
            Rng.RandfRange(AimShiftRangeVertical.X, AimShiftRangeVertical.Y)
        );

        for (int i = 0; i < PelletCount; i++)
        {
            float yaw = Mathf.DegToRad(Rng.RandfRange(-fi.Tool.CurrentSpread.X, fi.Tool.CurrentSpread.X));
            float pitch = Mathf.DegToRad(Rng.RandfRange(-fi.Tool.CurrentSpread.Y, fi.Tool.CurrentSpread.Y));

            // normalize then scale back down to make circular
            Vector3 angle = new Vector3(Mathf.Abs(pitch), Mathf.Abs(yaw), 0).Normalized();
            Vector3 dir = fi.ViewForward;
            dir = dir.Rotated(fi.ViewTransform.Basis.Y.Normalized(), angle.Y * yaw);
            dir = dir.Rotated(fi.ViewTransform.Basis.X.Normalized(), angle.X * pitch);

            var space = fi.Player.GetWorld3D().DirectSpaceState;
            var query = PhysicsRayQueryParameters3D.Create(fi.ViewTransform.Origin, fi.ViewTransform.Origin + dir * MaxRange, 5);
            var ray = space.IntersectRay(query);
            if (!ray.ContainsKey("collider")) continue;

            Debug.Rendering.Point((Vector3)ray["position"], color: new Color(0, 0, 0));

            Game.Pawn pawn = null;
            var currentNode = (Node)(GodotObject)ray["collider"];
            for (int j = 0; j < 5; j++)
            {
                currentNode = currentNode.GetParent();

                if (currentNode is null) break;
                if (currentNode is Game.Pawn p)
                {
                    pawn = p;
                    break;
                }
            }

            if (pawn is null) continue;

            var pos = (Vector3)ray["position"];
            var distanceSqr = pos.DistanceSquaredTo(fi.ViewTransform.Origin);
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

            var hitObjName = ((Node)(GodotObject)ray["collider"]).GetParent().Name;
            damage *= GetHitDamageMultiplier(hitObjName);

            var di = new DamageInfo()
            {
                Damage = damage,
                DamageType = DamageInfo.DamageTypeEnum.Physical,
                AttackerId = fi.Player.Id,
                AttackerName = Global.NetworkManager.Singleton._players[fi.Player.Id]["name"],
                WeaponId = HashId,
                HitboxName = hitObjName,
                HitPosition = pos,
                HitDirection = (pos - fi.ViewTransform.Origin).Normalized()
            };
            pawn.Rpc(Game.Pawn.MethodName.OnDamageRpc, di.ToVariant());
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