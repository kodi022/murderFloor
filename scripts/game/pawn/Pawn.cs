namespace Shooter.Game;

public partial class Pawn : CharacterBody3D
{
    [Signal]
    public delegate void PlayerOnHealEventHandler(float amount);
    [Signal]
    public delegate void PlayerOnDamageEventHandler(Godot.Collections.Dictionary<string, Variant> damageInfoVariant);
    [Signal]
    public delegate void PlayerOnDeathEventHandler(Godot.Collections.Dictionary<string, Variant> damageInfoVariant);
    [Signal]
    public delegate void PlayerOnSpawnEventHandler(Vector3 position);

    [Signal]
    public delegate void MobOnDamageEventHandler(Godot.Collections.Dictionary<string, Variant> damageInfoVariant);
    [Signal]
    public delegate void MobOnDeathEventHandler(Godot.Collections.Dictionary<string, Variant> damageInfoVariant);

    [Export]
    public float MaxHealth { get; set; } = 100;
    [Export]
    public float Health { get; set; } = 100;
    [Export]
    public float MaxArmor { get; set; } = 100;
    [Export]
    public float Armor { get; set; } = 0;

    public bool IsDead => Health <= 0;

    /// <summary>this should only be called using Rpc</summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public virtual void OnDamageRpc(Godot.Collections.Dictionary<string, Variant> damageInfoVariant)
    {
        if (IsDead) return;

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

        if (damage >= Health)
        {
            Health = 0;
            OnDeath(damageInfo);
            return;
        }

        Health -= damage;

        // gore
        // sounds

        bool attackerIsSelf = damageInfo.AttackerId == Player.Self.Id;
        if (attackerIsSelf && this is Mob)
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

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void SpawnRpc(Vector3 pos)
    {
        OnSpawn(pos);
    }

    public virtual void OnSpawn(Vector3 pos)
    {
        if (Player.Self == this)
        {
            EmitSignal(SignalName.PlayerOnSpawn, pos);
        }
    }

    public virtual void OnDeath(DamageInfo damageInfo)
    {
        if (this is Mob)
        {
            EmitSignal(SignalName.MobOnDeath, damageInfo.ToVariant());
        }

        if (Player.Self == this)
        {
            EmitSignal(SignalName.PlayerOnDeath, damageInfo.ToVariant());
        }
    }
}