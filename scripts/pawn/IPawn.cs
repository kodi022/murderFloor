namespace MurderFloor;

public interface IPawn
{
    public float MaxHealth { get; set; }
    public float Health { get; set; }
    public float Armor { get; set; }

    // "attacker": "player id OR 0 if mob"
    // "attackerName": "player name OR mob name"
    // "weapon": "weaponresourceid OR empty if mob"
    // "hitposition": "vector3"
    public void OnDamage(Godot.Collections.Dictionary<string, string> damageInfo)
    {
        var damage = 10f;

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
            // DIE IDIOT
            return;
        }

        Health -= damage;
    }
}