namespace MurderFloor;

public partial class LiveMob : Pawn
{
    public const float MinimumDistanceToTarget = 1.2f;

    public Mob MobResource { get; set; }

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
    private ulong lastWaypointTime;
    private ulong lastTargetUpdateTime;
    private ulong lastAttackTime;

    private bool verticalAction;
    private ulong verticalActionStartTime;
    private Curve3D verticalActionMovementCurve;

    private ulong ticksMs;
    private float distToTarget;

    public override void _Ready()
    {
        Active = false;
        navigationAgent3D.NavigationFinished += OnNavigationFinished;
        navigationAgent3D.WaypointReached += (a) => { lastWaypointTime = Time.GetTicksMsec(); };
        navigationAgent3D.LinkReached += OnLinkReached;
    }

    public void OnSpawn(Vector3 location, string mobFullId)
    {
        MobResource = ResourceManager.MobRegistry.GetResourceRef(mobFullId);
        MaxHealth = MobResource.MaxHealth;
        Health = MaxHealth;
        Armor = MobResource.Armor;
        Scale = Vector3.One * MobResource.Scale;
        GlobalPosition = location;
        Active = true;
        ChangeNavigationTarget();
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

        // attack, allow movement
        if (distToTarget < MobResource.AttackRange && MobResource.AttackRateMs < ticksMs - lastAttackTime)
        {
            lastAttackTime = ticksMs;

            animationTree.Set("parameters/oneshot_melee/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);

            var di = new DamageInfo()
            {
                Damage = MobResource.AttackDamage,
                DamageType = DamageInfo.DamageTypeEnum.Physical,
                AttackerId = 0,
                AttackerName = "Mob",
                WeaponId = 0,
                HitboxName = "UpperSpine",
                HitPosition = Vector3.Zero,
                HitDirection = (Position - targetPawn.Position).Normalized()
            };
            targetPawn.Rpc("OnDamageRpc", di.ToVariant());
        }
    }

    public override void OnDeath(DamageInfo damageInfo)
    {
        if (!Active) return;

        Active = false;

        Game.Current.MobDeath(damageInfo, MobPoolId);

        var ragdoll = GD.Load<PackedScene>("res://scenes/pawn/mob/LiveMobRagdoll.tscn").Instantiate<Node3D>();
        var liveSk = worldModels.GetNode<Skeleton3D>("KincheePlayerMob/Armature/Skeleton3D");
        var ragSk = ragdoll.GetNode<Skeleton3D>("KincheePlayerMob/Armature/Skeleton3D");
        var copyCount = Math.Min(liveSk.GetBoneCount(), ragSk.GetBoneCount());
        ragdoll.GlobalTransform = GlobalTransform;
        for (int i = 0; i < copyCount; i++)
        {
            var pos = liveSk.GetBonePosePosition(i) * 6.12728f; // due to import scaling
            var rot = liveSk.GetBonePoseRotation(i);
            ragSk.SetBonePosePosition(i, pos);
            ragSk.SetBonePoseRotation(i, rot);
        }
        var hitCollider = damageInfo.HitboxName;
        // ragdoll has different colliders
        if (hitCollider == "Head") hitCollider = "Neck";
        if (hitCollider == "Foot_R") hitCollider = "LowerLeg_R";
        if (hitCollider == "Foot_L") hitCollider = "LowerLeg_L";
        ((Ragdoll)ragdoll).SetHit(hitCollider, damageInfo.HitDirection, 20f);
        Game.Current.AddChild(ragdoll);
    }
}