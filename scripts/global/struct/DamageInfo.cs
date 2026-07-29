global using DamageInfoVariant = Godot.Collections.Dictionary<string, Godot.Variant>;

namespace MurderFloor;

public struct DamageInfo
{
    public float Damage { get; set; }
    public int AttackerId { get; set; }       // player id / 0 if mob
    public string AttackerName { get; set; }  // player name / mob name
    public int WeaponId { get; set; }         // weaponresourceid / empty if mob
    public string HitboxName { get; set; }
    public Vector3 HitPosition { get; set; }
    public Vector3 HitDirection { get; set; }

    public readonly DamageInfoVariant ToVariant()
    {
        return new DamageInfoVariant
        {
            { nameof(Damage), Damage },
            { nameof(AttackerId), AttackerId },
            { nameof(AttackerName), AttackerName },
            { nameof(WeaponId), WeaponId },
            { nameof(HitboxName), HitboxName },
            { nameof(HitPosition), HitPosition },
            { nameof(HitDirection), HitDirection },
        };
    }

    public static DamageInfo FromVariant(DamageInfoVariant variant)
    {
        return new DamageInfo()
        {
            Damage = variant[nameof(Damage)].AsSingle(),
            AttackerId = variant[nameof(AttackerId)].AsInt32(),
            AttackerName = variant[nameof(AttackerName)].AsString(),
            WeaponId = variant[nameof(WeaponId)].AsInt32(),
            HitboxName = variant[nameof(HitboxName)].AsString(),
            HitPosition = variant[nameof(HitPosition)].AsVector3(),
            HitDirection = variant[nameof(HitDirection)].AsVector3()
        };
    }
}