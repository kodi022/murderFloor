namespace Shooter.Resource;

[GlobalClass]
public partial class Mob : GameResource
{
    [Export]
    public float MaxHealth { get; private set; } = 100f;
    [Export]
    public float Armor { get; private set; }

    [Export, ExportSubgroup("Visual")]
    public float Scale { get; private set; } = 1f;

    [Export, ExportSubgroup("Movement")]
    public float MovementSpeedScale { get; private set; } = 1f;

    [Export, ExportSubgroup("Attacking")]
    public ulong AttackRateMs { get; private set; } = 1000ul;
    [Export]
    public float AttackRange { get; private set; } = 1.2f;
    [Export]
    public float AttackDamage { get; private set; } = 10f;
    [Export]
    public Godot.Collections.Dictionary<string, float> HitboxDamageMultipliers { get; private set; } = new()
    {
        ["Head"] = 1.3f,
        ["Neck"] = 1.2f,
        ["Hand"] = 0.8f,
        ["Foot"] = 0.8f,
    };

    [Export, ExportSubgroup("Enragement")]
    public bool Enragement { get; private set; }
    [Export]
    public float EnragementDamageThreshold { get; private set; }
    [Export]
    public float EnragementLength { get; private set; }

    [Export, ExportSubgroup("Ragdoll")]
    public PackedScene Ragdoll { get; private set; }
    [Export]
    public Godot.Collections.Dictionary<string, string> RagdollColliderRemaps { get; private set; }

    public float GetHitDamageMultiplier(string colliderName)
    {
        foreach (var kvp in HitboxDamageMultipliers)
        {
            if (colliderName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        return 1f;
    }
}