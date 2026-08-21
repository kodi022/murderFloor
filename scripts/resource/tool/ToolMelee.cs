namespace MurderFloor;

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

    public virtual void FireMelee(FireInfo fi)
    {
        var space = fi.Player.GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(fi.StartPosition, fi.StartPosition + fi.ViewForward * MaxRange, 5);
        var ray = space.IntersectRay(query);
        if (ray.ContainsKey("collider"))
        {
            Debug.DebugDot((Vector3)ray["position"], color: new Color(0, 0, 0));

            Pawn pawn = null;
            var currentNode = (Node)(GodotObject)ray["collider"];
            for (int j = 0; j < 5; j++)
            {
                currentNode = currentNode.GetParent();

                if (currentNode is null) break;
                if (currentNode is Pawn p)
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
                    AttackerName = NetworkManager.Current._players[fi.Player.Id]["Name"],
                    WeaponId = HashId,
                    HitboxName = hitObjName,
                    HitPosition = (Vector3)ray["position"],
                    HitDirection = (pos - fi.StartPosition).Normalized()
                };
                pawn.Rpc("OnDamageRpc", di.ToVariant());
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

    public override BuiltToolData BuildToolScene(BuildToolData buildToolData)
    {
        var builtToolData = new BuiltToolData() { ToolHashId = HashId };

        var toolResource = ResourceManager.ToolRegistry.GetResourceRef(HashId);
        builtToolData.Node3D = toolResource.MeshScene.Instantiate<Node3D>();

        Node3D FindNode(string name)
        {
            var thing = (Node3D)builtToolData.Node3D.FindChildren(name).FirstOrDefault(new Node3D());
            if (!thing.IsInsideTree())
                GD.PrintErr($"Warning: {toolResource.FullId} has no Node3D named \"{name}\"");

            return thing;
        }

        var gadgetNode = FindNode("Point-Gadget");

        foreach (var hashId in buildToolData.AttachmentHashIds)
        {
            var attachment = ResourceManager.AttachmentRegistry.GetResourceRef(hashId);
            switch (attachment.AttachmentType)
            {
                case Attachment.AttachmentTypeEnum.Gadget:

                    break;
            }
        }

        return builtToolData;
    }
}