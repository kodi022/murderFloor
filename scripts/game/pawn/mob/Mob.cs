namespace Shooter.Game;

public partial class Mob : Pawn
{
    public const float MinimumDistanceToTarget = 1.2f;

    public int Level { get; set; }
    public string MobResourceFullId { get; set; }
    public Resource.Mob MobResource { get; private set; }

    [Export]
    public bool Active
    {
        get { return _active; }
        private set
        {
            _active = value;
            ProcessMode = value ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
            worldModels.Visible = value;
            collisionShape3D.SetDeferred("disabled", !value);
            Visible = value;
            ResetPhysicsInterpolation();
        }
    }

    public int MobProcessOffset { get; set; } = 0;
    public int MobPoolId { get; set; } = 0;

    private static readonly RandomNumberGenerator mobRng = new();

    [Export]
    private Node3D worldModels;
    [Export]
    private NavigationAgent3D navigationAgent3D;
    [Export]
    private CollisionShape3D collisionShape3D;
    [Export]
    private AnimationTree animationTree;

    private bool _active;
    private Pawn targetPawn;
    private int processTick;
    private ulong lastAttackTime;

    private float levelSpeedBuff;
    private float levelDamageBuff;
    private float levelVerticalActionBuff;

    private ulong ticksMs;
    private float distToTarget;

    public override void _Ready()
    {
        Active = false;
        navigationAgent3D.NavigationFinished += OnNavigationFinished;
        navigationAgent3D.WaypointReached += (a) => { lastWaypointTime = Time.GetTicksMsec(); };
        navigationAgent3D.LinkReached += OnLinkReached;
    }

    // 100 ticks per second
    public override void _PhysicsProcess(double delta)
    {
        if (!Active) return;
        if (!IsMultiplayerAuthority()) return;

        ticksMs = Time.GetTicksMsec();
        distToTarget = targetPawn?.Position.DistanceTo(Position) ?? 0f;
        processTick++;

        PhysicsProcessMovement();

        // attack, continue movement
        if (distToTarget < MobResource.AttackRange && MobResource.AttackRateMs < ticksMs - lastAttackTime)
        {
            lastAttackTime = ticksMs;

            animationTree.Set("parameters/oneshot_melee/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);

            var di = new DamageInfo()
            {
                Damage = MobResource.AttackDamage * levelDamageBuff,
                DamageType = DamageInfo.DamageTypeEnum.Physical,
                AttackerId = 0,
                AttackerName = "Mob",
                WeaponId = 0,
                HitboxName = "UpperSpine",
                HitPosition = Vector3.Zero,
                HitDirection = (Position - targetPawn.Position).Normalized()
            };
            targetPawn.Rpc(Pawn.MethodName.OnDamageRpc, di.ToVariant());
        }
    }

    public override void OnSpawn(Vector3 pos)
    {
        base.OnSpawn(pos);

        foreach (var body in FindChildren("StaticBody3D"))
        {
            body.SetMeta("decal", "res://images/decal/bodyblooddecal1.png");
        }

        ResetPhysicsInterpolation();
        GlobalPosition = pos;
        MobResource = ResourceManager.MobRegistry.GetResourceRef(MobResourceFullId);
        MaxHealth = MobResource.MaxHealth * (1 + (Level * 0.015f));
        Health = MaxHealth;
        Armor = MobResource.Armor;
        Scale = Vector3.One * MobResource.Scale;
        levelSpeedBuff = 1 + Level * 0.002f;
        levelDamageBuff = 1 + Level * 0.01f;
        levelVerticalActionBuff = 1 - Level * 0.005f;
        startPosHash = Utils.Hashing.StableHash(pos);
        Active = true;
        ChangeNavigationTarget();
    }

    public override void OnDeath(DamageInfo damageInfo)
    {
        if (!Active) return;
        Active = false;

        ResetPhysicsInterpolation();
        base.OnDeath(damageInfo);
        Game.Current.MobDeath(damageInfo, MobPoolId);

        var ragdoll = MobResource.Ragdoll.Instantiate<Node3D>();
        var hitCollider = RemapCollider(damageInfo.HitboxName);

        foreach (var decal in GetChild(0).GetChild(0).FindChildren("Decal_*"))
        {
            var decalPos = ((Decal)decal).Position;
            var parentName = decal.GetParent().GetParent().Name;
            var ragdollParentBone = ragdoll.FindChild($"Physical Bone {RemapCollider(parentName)}");
            decal.Owner = null;
            decal.Reparent(ragdollParentBone);
            decal.Owner = ragdollParentBone;
            ((Decal)decal).Position = decalPos;
        }

        ((Ragdoll)ragdoll).SetHit(hitCollider, damageInfo.HitDirection, damageInfo.Force * 100f);

        var liveSk = worldModels.GetNode<Skeleton3D>("KincheePlayerMob/Armature/Skeleton3D");
        var ragSk = ragdoll.GetNode<Skeleton3D>("KincheePlayerMob/Armature/Skeleton3D");
        var copyCount = Math.Min(liveSk.GetBoneCount(), ragSk.GetBoneCount());
        ragdoll.GlobalTransform = GlobalTransform;
        for (int i = 0; i < copyCount; i++)
        {
            var pos = liveSk.GetBonePosePosition(i);
            var rot = liveSk.GetBonePoseRotation(i);
            ragSk.SetBonePosePosition(i, pos);
            ragSk.SetBonePoseRotation(i, rot);
        }

        Global.GameManager.ClearOnLoad.AddChild(ragdoll);
    }

    private string RemapCollider(string collider)
    {
        if (MobResource.RagdollColliderRemaps.TryGetValue(collider, out string value))
            return value;
        else
            return collider;
    }
}