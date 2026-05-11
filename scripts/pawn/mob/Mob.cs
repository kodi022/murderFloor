namespace MurderFloor;

public partial class Mob : CharacterBody3D, IPawn
{
    public float MaxHealth { get; set; }
    public float Health { get; set; }
    public float Armor { get; set; }
}