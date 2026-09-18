namespace Shooter.Resource;

[GlobalClass]
public partial class ToolMelee : Tool
{
    [Export]
    public float RPM { get; private set; }
    [Export]
    public float Damage { get; private set; } = 20f;
    [Export]
    public float MaxRange { get; private set; } = 1f;

    [Export]
    public string IdleAnimationName { get; private set; } = "idle";

    private static readonly Dictionary<string, float> HitboxDamageMultipliers = new()
    {
        ["Head"] = 1.25f,
        ["Neck"] = 1.15f,
    };

    public override SlotEnum GetSlot() => SlotEnum.Melee;

    public virtual void FireMelee(FireInfo fi)
    {
        var space = fi.Player.GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(fi.ViewTransform.Origin, fi.ViewTransform.Origin + fi.ViewForward * MaxRange, 5);
        var ray = space.IntersectRay(query);
        if (ray.ContainsKey("collider"))
        {
            Debug.Rendering.Point((Vector3)ray["position"], color: new Color(0, 0, 0));

            Game.Pawn pawn = null;
            var currentNode = (Node)(GodotObject)ray["collider"];
            for (int j = 0; j < 5; j++)
            {
                currentNode = currentNode.GetParent();

                if (currentNode is null) break;
                if (currentNode is Game.Pawn p)
                {
                    pawn = p;
                    break;
                }
            }

            if (pawn is not null)
            {
                var pos = (Vector3)ray["position"];
                var damage = Damage;

                var hitObjName = ((Node)(GodotObject)ray["collider"]).GetParent().Name.ToString();
                damage *= GetHitDamageMultiplier(hitObjName);

                var di = new DamageInfo()
                {
                    Damage = damage,
                    DamageType = DamageInfo.DamageTypeEnum.Physical,
                    AttackerId = fi.Player.Id,
                    AttackerName = Global.NetworkManager.Singleton._players[fi.Player.Id]["Name"],
                    WeaponId = HashId,
                    HitboxName = hitObjName,
                    HitPosition = (Vector3)ray["position"],
                    HitDirection = (pos - fi.ViewTransform.Origin).Normalized()
                };
                pawn.Rpc(Game.Pawn.MethodName.OnDamageRpc, di.ToVariant());
            }
        }
    }

    private static float GetHitDamageMultiplier(string colliderName)
    {
        foreach (var kvp in HitboxDamageMultipliers)
        {
            if (colliderName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        return 1f;
    }

    public override BuiltToolData BuildToolScene(ToolConfig toolConfig)
    {
        var builtToolData = new BuiltToolData() { ToolHashId = HashId };

        var toolResource = ResourceManager.ToolRegistry.GetResourceRef(HashId);
        builtToolData.Tool = toolResource.MeshScene.Instantiate<Node3D>();

        Node3D FindNode(string name)
        {
            var thing = (Node3D)builtToolData.Tool.FindChildren(name).FirstOrDefault(new Node3D());
            if (!thing.IsInsideTree())
                GD.PrintErr($"Warning: {toolResource.FullId} has no Node3D named \"{name}\"");

            return thing;
        }

        var gadgetNode = FindNode("Point-Gadget");

        // foreach (var hashId in toolConfig.AttachmentLootStates)
        // {
        //     var attachment = ResourceManager.AttachmentRegistry.GetResourceRef(hashId);
        //     switch (attachment.AttachmentType)
        //     {
        //         case Attachment.AttachmentTypeEnum.Gadget:

        //             break;
        //     }
        // }

        return builtToolData;
    }
}