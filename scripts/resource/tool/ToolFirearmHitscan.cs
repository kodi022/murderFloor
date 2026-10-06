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

        var firearmBehavior = (Game.ToolBehaviorFirearm)fi.Tool.ToolBehavior;
        for (int i = 0; i < PelletCount; i++)
        {
            float yaw = Mathf.DegToRad(Rng.RandfRange(-firearmBehavior.CurrentSpread.X, firearmBehavior.CurrentSpread.X));
            float pitch = Mathf.DegToRad(Rng.RandfRange(-firearmBehavior.CurrentSpread.Y, firearmBehavior.CurrentSpread.Y));

            // normalize then scale back down to make circular
            Vector3 angle = new Vector3(Mathf.Abs(pitch), Mathf.Abs(yaw), 0).Normalized();
            Vector3 dir = fi.ViewForward;
            dir = dir.Rotated(fi.ViewTransform.Basis.Y.Normalized(), angle.Y * yaw);
            dir = dir.Rotated(fi.ViewTransform.Basis.X.Normalized(), angle.X * pitch);

            var space = fi.Player.GetWorld3D().DirectSpaceState;
            var query = PhysicsRayQueryParameters3D.Create(fi.ViewTransform.Origin, fi.ViewTransform.Origin + dir * MaxRange, 5);
            var ray = space.IntersectRay(query);

            var bulletPitch = Mathf.Atan2(dir.Y, Mathf.Sqrt(dir.X * dir.X + dir.Z * dir.Z));
            var bulletYaw = Mathf.Atan2(dir.X, dir.Z);
            fi.Tool.BulletParticles[i].GlobalPosition = fi.Tool.MuzzleFlashParticle.GlobalPosition;
            fi.Tool.BulletParticles[i].GlobalRotation = new Vector3(0, bulletYaw - Mathf.DegToRad(90f), bulletPitch);
            fi.Tool.BulletParticles[i].Lifetime = 10d;
            fi.Tool.BulletParticles[i].Restart();

            if (!ray.ContainsKey("collider")) continue;

            var pos = (Vector3)ray["position"];
            var distanceSqr = pos.DistanceSquaredTo(fi.ViewTransform.Origin);
            fi.Tool.BulletParticles[i].Lifetime = Mathf.Sqrt(distanceSqr) / 150f - 0.0065f;

            Game.Pawn pawn = null;
            var currentNode = (Node)(GodotObject)ray["collider"];

            var normal = (Vector3)ray["normal"];
            var decalRotPitch = Mathf.Acos(normal.Y);
            var decalRotYaw = Mathf.Atan2(normal.X, normal.Z);
            var decalRot = new Vector3(decalRotPitch, decalRotYaw, 0f);
            // roll Random.Shared.NextSingle() * Mathf.Pi
            Global.DecalManager.UpdateNext(
                currentNode,
                (Vector3)ray["position"],
                decalRot,
                (string)currentNode.GetMeta("decal", "res://images/decal/bulletdecal.png"),
                new Vector3(0.25f, 0.25f, 0.25f)
            );

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

    // sign determines buff direction
    public enum AllStatsEnum
    {
        Damages = 1,
        FalloffRanges = 2,
        RPM = 3,
        HoldingSpeed = 5,
        FloatDamage = 9,
        PelletCount = 4,
        ReloadDelayMs = -1,
        MagSize = 6,
        MagsReserve = 7,
        InitialDegreeSpread = -2,
        MaxDegreeSpread = -3,
        SpreadRecoveryRate = 8,
        SpreadIncreasePerShot = -4,
        SlowWalkSpreadMult = -5,
        FastWalkSpreadMult = -6,
        AimSpreadMult = -7,
        AimShiftRangeVertical = -8,
        AimShiftRangeHorizontal = -9,
    }

    /// <summary>Gets stat or -1.0f if not found</summary>
    public override Variant GetStat(string statName, float mult)
    {
        return (AllStatsEnum)Enum.Parse(typeof(AllStatsEnum), statName) switch
        {
            AllStatsEnum.Damages => Damages * mult,
            AllStatsEnum.FalloffRanges => FalloffRanges * mult,
            AllStatsEnum.RPM => RPM * mult,
            AllStatsEnum.PelletCount => PelletCount * mult,
            AllStatsEnum.HoldingSpeed => HoldingSpeed * mult,
            AllStatsEnum.ReloadDelayMs => ReloadDelayMs * mult,
            AllStatsEnum.MagSize => MagSize * mult,
            AllStatsEnum.MagsReserve => MagsReserve * mult,
            AllStatsEnum.InitialDegreeSpread => InitialDegreeSpread * mult,
            AllStatsEnum.MaxDegreeSpread => MaxDegreeSpread * mult,
            AllStatsEnum.SpreadRecoveryRate => SpreadRecoveryRate * mult,
            AllStatsEnum.SpreadIncreasePerShot => SpreadIncreasePerShot * mult,
            AllStatsEnum.SlowWalkSpreadMult => SlowWalkSpreadMult * mult,
            AllStatsEnum.FastWalkSpreadMult => FastWalkSpreadMult * mult,
            AllStatsEnum.AimSpreadMult => AimSpreadMult * mult,
            AllStatsEnum.AimShiftRangeVertical => AimShiftRangeVertical * mult,
            AllStatsEnum.AimShiftRangeHorizontal => AimShiftRangeHorizontal * mult,
            _ => -1f,
        };
    }

    /// <summary>Gets names of all stats or empty array if none</summary>
    public override string[] GetStatsEnum()
    {
        return Enum.GetNames(typeof(AllStatsEnum));
    }

    /// <summary>Gets sign of stat based on its enum</summary>
    public override int GetStatSign(string statName)
    {
        return Math.Sign((int)Enum.Parse(typeof(AllStatsEnum), statName));
    }
}