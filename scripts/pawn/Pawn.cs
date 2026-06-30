namespace MurderFloor;

public partial class Pawn : CharacterBody3D
{
    [Signal]
    public delegate void PlayerOnDamageEventHandler(DamageInfo damageInfo);
    [Signal]
    public delegate void PlayerOnDeathEventHandler(DamageInfo damageInfo);

    [Signal]
    public delegate void MobOnDamageEventHandler(DamageInfo damageInfo);
    [Signal]
    public delegate void MobOnDeathEventHandler(DamageInfo damageInfo);

    [Export]
    public float MaxHealth { get; set; } = 100;
    [Export]
    public float Health { get; set; } = 100;
    [Export]
    public float Armor { get; set; } = 0;

    /// <summary>
    /// this should only be called using Rpc
    /// <para>"damage": "float"</para>
    /// <para>"attacker": "player id OR 0 if mob"</para>
    /// <para>"attackerName": "player name OR mob name"</para>
    /// <para>"weapon": "weaponresourceid OR empty if mob"</para>
    /// <para>"hitposition": "vector3"</para>
    /// <para>"hitbox": "int id"</para>
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public virtual void OnDamageRpc(DamageInfo damageInfo)
    {
        var damage = damageInfo["damage"].AsSingle();

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
        // sound

        // signals
        bool attackerIsSelf = damageInfo["attacker"].AsInt64() == Player.Self.Id;
        if (attackerIsSelf && this is LiveMob)
        {
            EmitSignal(SignalName.MobOnDamage, damageInfo);
        }

        if (Player.Self == this)
        {
            EmitSignal(SignalName.PlayerOnDamage, damageInfo);
        }
    }

    public virtual void OnDeath(DamageInfo damageInfo)
    {

    }
}