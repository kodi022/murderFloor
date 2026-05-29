namespace MurderFloor;

public partial class Pawn : CharacterBody3D
{
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
    public virtual void OnDamageRpc(Godot.Collections.Dictionary<string, string> damageInfo)
    {
        GD.Print($"OnDamageRpc {Name} ({Health})");
        var damage = damageInfo["damage"].ToFloat();

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
    }

    public virtual void OnDeath(Godot.Collections.Dictionary<string, string> damageInfo)
    {

    }
}