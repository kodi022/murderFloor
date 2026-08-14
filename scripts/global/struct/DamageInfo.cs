global using DamageInfoVariant = Godot.Collections.Dictionary<string, Godot.Variant>;

namespace MurderFloor;

public struct DamageInfo
{
    public enum DamageTypeEnum
    {
        Physical,
        Explosion,
        Fire,
        Acid,
    }

    public int AttackerId { get; set; }       // player id / 0 if mob
    public string AttackerName { get; set; }  // player name / mob name
    public int WeaponId { get; set; }         // weaponresourceid / empty if mob
    public string HitboxName { get; set; }
    public float Damage { get; set; }
    public DamageTypeEnum DamageType { get; set; }
    public float Force { get; set; }
    public Vector3 HitPosition { get; set; }
    public Vector3 HitDirection { get; set; }

    public readonly DamageInfoVariant ToVariant()
    {
        return new DamageInfoVariant
        {
            { nameof(AttackerId), AttackerId },
            { nameof(AttackerName), AttackerName },
            { nameof(WeaponId), WeaponId },
            { nameof(HitboxName), HitboxName },
            { nameof(Damage), Damage },
            { nameof(DamageType), (int)DamageType },
            { nameof(Force), Force },
            { nameof(HitPosition), HitPosition },
            { nameof(HitDirection), HitDirection },
        };
    }

    public static DamageInfo FromVariant(DamageInfoVariant variant)
    {
        return new DamageInfo()
        {
            AttackerId = variant[nameof(AttackerId)].AsInt32(),
            AttackerName = variant[nameof(AttackerName)].AsString(),
            WeaponId = variant[nameof(WeaponId)].AsInt32(),
            HitboxName = variant[nameof(HitboxName)].AsString(),
            Damage = variant[nameof(Damage)].AsSingle(),
            DamageType = (DamageTypeEnum)variant[nameof(DamageType)].AsInt32(),
            Force = variant[nameof(Force)].AsSingle(),
            HitPosition = variant[nameof(HitPosition)].AsVector3(),
            HitDirection = variant[nameof(HitDirection)].AsVector3()
        };
    }
}