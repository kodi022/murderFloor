namespace MurderFloor;

public partial class LiveMob : Pawn
{
    public const float MinimumDistanceToTarget = 0.8f;

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

    public override void _Ready()
    {
        MobResource = ResourceManager.MobRegistry.GetAllResource().First().Value;
        Active = false;
        navigationAgent3D.NavigationFinished += OnNavigationFinished;
        navigationAgent3D.WaypointReached += (a) => { lastWaypointTime = Time.GetTicksMsec(); };
        navigationAgent3D.LinkReached += OnLinkReached;
    }

    public void OnSpawn(Vector3 location, int mobResourceId)
    {
        MaxHealth = 100;
        Health = MaxHealth;
        GlobalPosition = location;
        Active = true;
        ChangeNavigationTarget();
    }

    public override void OnDeath(Godot.Collections.Dictionary<string, Variant> damageInfo)
    {
        Game.Current.MobDeath(MobPoolId);
        Active = false;
    }

    // 100 ticks per second
    public override void _PhysicsProcess(double delta)
    {
        if (!Active) return;
        if (!IsMultiplayerAuthority()) return;

        var ticksMs = Time.GetTicksMsec();
        processTick++;
        if ((processTick + MobProcessOffset) % 20 == 0) ProcessLogic(ticksMs);

        var newVelocity = ProcessPathfinding();
        var lookingAtVel = new Vector3(newVelocity.X, 0f, newVelocity.Z).Normalized();
        if (lookingAtVel.LengthSquared() > 0.0001f)
        {
            var yaw = Mathf.Atan2(lookingAtVel.X, lookingAtVel.Z);
            Rotation = new Vector3(0f, yaw, 0f);
        }

        var distToTarget = targetPawn?.Position.DistanceTo(Position) ?? 0f;

        animationTree.Set("parameters/timescale_walk/scale", 1.5f);

        // attack, allow movement
        if (distToTarget < MobResource.AttackRange && MobResource.AttackRateMs < ticksMs - lastAttackTime)
        {
            lastAttackTime = ticksMs;

            animationTree.Set("parameters/oneshot_melee/request", (int)AnimationNodeOneShot.OneShotRequest.Fire);

            var di = new Godot.Collections.Dictionary<string, string>()
            {
                {"damage", "5"},
                {"attacker", "0"},
                {"attackerName", "mobname"},
                {"weapon", "claw"},
                {"hitposition", Vector3.Zero.ToString()},
                {"hitbox", "0"}
            };
            targetPawn.Rpc("OnDamageRpc", di);
        }

        if (verticalAction)
        {
            var pointCount = verticalActionMovementCurve.PointCount;
            var timeOffset = Time.GetTicksMsec() - verticalActionStartTime;
            Position = verticalActionMovementCurve.Samplef(timeOffset / 2000f * (pointCount - 1));
            if (timeOffset >= 2000ul)
            {
                verticalAction = false;
                Position = verticalActionMovementCurve.Samplef(1f * pointCount);
            }
        }
        else
        {
            if (distToTarget > MinimumDistanceToTarget)
            {
                MoveAndSlide();
            }
        }
    }

    private void ProcessLogic(ulong ticksMs)
    {
        if (targetPawn is null || targetPawn.Health <= 0)
        {
            ChangeNavigationTarget();
        }

        if (10000ul < ticksMs - lastTargetUpdateTime)
        {
            lastTargetUpdateTime = ticksMs;
            ChangeNavigationTarget();
            return;
        }

        if (20000ul < ticksMs - lastWaypointTime)
        {
            lastWaypointTime = ticksMs;
            Unstuck();
            return;
        }
    }

    private Vector3 ProcessPathfinding()
    {
        navigationAgent3D.TargetPosition = targetPawn?.GlobalPosition ?? GlobalPosition;
        if (navigationAgent3D.TargetPosition == GlobalPosition) return Vector3.Zero;

        var targetPos = navigationAgent3D.GetNextPathPosition(); // required every physics frame

        var velocityTarget = new Vector3(targetPos.X, targetPos.Y, targetPos.Z);
        Vector3 newVelocity = GlobalPosition.DirectionTo(velocityTarget) * 3.4f; // ! speed
        Velocity = newVelocity + Vector3.Down * 0.5f;
        return newVelocity;
    }

    private void OnNavigationFinished()
    {
        var distToTarget = targetPawn?.Position.DistanceTo(Position) ?? 0f;

        if (distToTarget > MinimumDistanceToTarget)
        {
            ChangeNavigationTarget();
        }
    }

    // position: The start position of the link that was reached.
    // type: Always NavigationPathQueryResult3D.PathSegmentType.Link.
    // rid: The Rid of the link.
    // owner: The object which manages the link (usually NavigationLink3D).
    // link_entry_position: If owner is available and the owner is a NavigationLink3D, it will contain the global position of the link's point the agent is entering.
    // link_exit_position: If owner is available and the owner is a NavigationLink3D, it will contain the global position of the link's point which the agent is exiting.
    private void OnLinkReached(Godot.Collections.Dictionary values)
    {
        if (verticalAction) return;

        var entryPos = (Vector3)values["link_entry_position"];
        var exitPos = (Vector3)values["link_exit_position"];
        var exitHeightDiff = entryPos.Y - exitPos.Y;

        // jump
        if (exitHeightDiff < 0f)
        {
            verticalAction = true;
            verticalActionStartTime = Time.GetTicksMsec();
            verticalActionMovementCurve = new();

            var centerPos = (exitPos + entryPos) * 0.5f;
            var jumpApexPos = new Vector3(centerPos.X, exitPos.Y + 0.5f, centerPos.Z);

            var startHandleLength = (entryPos - jumpApexPos).Length() * 0.25f;
            var entryOut = (jumpApexPos - entryPos).Normalized() * startHandleLength + Vector3.Up * 0.2f;
            var centerIn = -entryOut + Vector3.Up * 0.4f;

            var endHandleLength = (exitPos - jumpApexPos).Length() * 0.25f;
            var centerOut = (exitPos - jumpApexPos).Normalized() * endHandleLength + Vector3.Up * 0.2f;
            var exitIn = -centerOut + Vector3.Up * 0.4f;

            verticalActionMovementCurve.AddPoint(entryPos, @out: entryOut);
            verticalActionMovementCurve.AddPoint(jumpApexPos, centerIn, centerOut);
            verticalActionMovementCurve.AddPoint(exitPos, @in: exitIn);

            int pointDotSize = 6, handleDotSize = 4;
            Color pointColor = new Color(1, 1, 0), handleColor = new Color(0, 1, 0);
            Global.DebugDot(this, entryPos, pointDotSize, pointColor);
            Global.DebugDot(this, entryPos + entryOut, handleDotSize, handleColor);
            Global.DebugDot(this, jumpApexPos + centerIn, handleDotSize, handleColor);
            Global.DebugDot(this, jumpApexPos, pointDotSize, pointColor);
            Global.DebugDot(this, jumpApexPos + centerOut, handleDotSize, handleColor);
            Global.DebugDot(this, exitPos + exitIn, handleDotSize, handleColor);
            Global.DebugDot(this, exitPos, pointDotSize, pointColor);
        }

        // drop
        if (exitHeightDiff > 0f)
        {
            verticalAction = true;
            verticalActionStartTime = Time.GetTicksMsec();
            verticalActionMovementCurve = new();
            verticalActionMovementCurve.AddPoint(entryPos);
            verticalActionMovementCurve.AddPoint(exitPos);

            for (int i = 0; i < 10; i++)
            {
                Global.DebugDot(this, verticalActionMovementCurve.Samplef(i / 10f), 1000);
            }
        }
    }

    private void ChangeNavigationTarget()
    {
        // weight on highest damage dealers?

        //navigationAgent3D.TargetPosition = new Vector3(mobRng.Randfn() * 10, mobRng.Randfn() * 10, mobRng.Randfn() * 10);

        // smart weighting to have a higher overriding range for players who do more damage
        // if not team attack

        // else find teammate pawns

        var nearestDist = 999999f;
        Player nearestPlr = null;
        foreach (var plr in Player.AllPlayers)
        {
            var dist = (GlobalPosition - plr.GlobalPosition).LengthSquared();

            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestPlr = plr;
            }
        }

        targetPawn = nearestPlr;
    }

    private void Unstuck()
    {
        var map = navigationAgent3D.GetNavigationMap();
        for (int i = 0; i < 1000; i++)
        {
            var grow = 1 + (i * 0.01f);
            var rand = GlobalPosition + new Vector3(mobRng.Randfn() * grow, mobRng.Randfn() * grow, mobRng.Randfn() * grow);
            var point = NavigationServer3D.MapGetClosestPoint(map, rand);
            if ((rand - point).LengthSquared() < 5f)
            {
                GlobalPosition = point;
                break;
            }
        }
    }
}