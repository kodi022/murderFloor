namespace MurderFloor;

public partial class Pawn : CharacterBody3D
{
    [Signal]
    public delegate void PlayerOnHealEventHandler(float amount);
    [Signal]
    public delegate void PlayerOnDamageEventHandler(DamageInfoVariant damageInfoVariant);
    [Signal]
    public delegate void PlayerOnDeathEventHandler(DamageInfoVariant damageInfoVariant);

    [Signal]
    public delegate void MobOnDamageEventHandler(DamageInfoVariant damageInfoVariant);
    [Signal]
    public delegate void MobOnDeathEventHandler(DamageInfoVariant damageInfoVariant);

    [Export]
    public float MaxHealth { get; set; } = 100;
    [Export]
    public float Health { get; set; } = 100;
    [Export]
    public float MaxArmor { get; set; } = 100;
    [Export]
    public float Armor { get; set; } = 0;

    /// <summary>this should only be called using Rpc</summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public virtual void OnDamageRpc(DamageInfoVariant damageInfoVariant)
    {
        var damageInfo = DamageInfo.FromVariant(damageInfoVariant);
        var damage = damageInfo.Damage;

        if (Armor > 0)
        {
            if (damage * 2 > Armor)
            {
                damage -= Armor * 0.5f;
                Armor = 0;
            }
            else
            {
                damage *= 0.5f;
                Armor -= damage;
            }
        }

        if (damage > Health)
        {
            Health = 0;
            OnDeath(damageInfo);
            return;
        }

        Health -= damage;

        // gore
        // sounds

        bool attackerIsSelf = damageInfo.AttackerId == Player.Self.Id;
        if (attackerIsSelf && this is LiveMob)
        {
            EmitSignal(SignalName.MobOnDamage, damageInfo.ToVariant());
        }

        if (Player.Self == this)
        {
            EmitSignal(SignalName.PlayerOnDamage, damageInfo.ToVariant());
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public virtual void OnHealRpc(DamageInfo damageInfo)
    {
    }

    public virtual void OnDeath(DamageInfo damageInfo)
    {
        if (this is LiveMob)
        {
            EmitSignal(SignalName.MobOnDeath, damageInfo.ToVariant());
        }

        if (Player.Self == this)
        {
            EmitSignal(SignalName.PlayerOnDeath, damageInfo.ToVariant());
        }
    }
}