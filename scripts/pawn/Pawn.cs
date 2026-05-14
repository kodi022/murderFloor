namespace MurderFloor;

public partial class Pawn : CharacterBody3D
{
    [Export]
    public float MaxHealth { get; set; }
    [Export]
    public float Health { get; set; }
    [Export]
    public float Armor { get; set; }

    /// <summary>
    /// <para>"attacker": "player id OR 0 if mob"</para>
    /// <para>"attackerName": "player name OR mob name"</para>
    /// <para>"weapon": "weaponresourceid OR empty if mob"</para>
    /// <para>"hitposition": "vector3"</para>
    /// <para>"hitbox": "int id"</para>
    /// </summary>
    public virtual void OnDamage(Godot.Collections.Dictionary<string, string> damageInfo)
    {
        var damage = 0.5f;

        GD.Print(Name + " " + Health);
        // if (damageInfo["attacker"] == "0")
        // {
        // }

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
    }

    public virtual void OnDeath(Godot.Collections.Dictionary<string, string> damageInfo)
    {

    }
}