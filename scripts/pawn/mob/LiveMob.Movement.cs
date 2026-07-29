namespace MurderFloor;

public partial class LiveMob : Pawn
{
    private Vector3 velocityNoGravity;

    public void PhysicsProcessMovement()
    {
        if ((processTick + MobProcessOffset) % 20 == 0) CheckNavigationTarget(ticksMs);

        ProcessPathfinding();

        // looking
        if (velocityNoGravity == Vector3.Zero)
        {
            var lookingAt = new Vector3(mobRng.Randf(), 0f, mobRng.Randf()) - Vector3.One * 0.5f;
            if (lookingAt.LengthSquared() > 0.0001f)
            {
                var yaw = Mathf.Atan2(lookingAt.X, lookingAt.Z);
                Rotation = new Vector3(0f, yaw, 0f);
            }
        }
        else
        {
            var lookingAt = velocityNoGravity.Normalized();
            if (lookingAt.LengthSquared() > 0.0001f)
            {
                var yaw = Mathf.Atan2(lookingAt.X, lookingAt.Z);
                Rotation = new Vector3(0f, yaw, 0f);
            }
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
                animationTree.Set("parameters/timescale_walk/scale", MobResource.MovementSpeedScale * 2f);
            }
            else
            {
                animationTree.Set("parameters/timescale_walk/scale", 0f);
            }
        }
    }

    private void CheckNavigationTarget(ulong ticksMs)
    {
        if (targetPawn is null || targetPawn.Health <= 0)
        {
            ChangeNavigationTarget();
        }

        if (1000ul < ticksMs - lastTargetUpdateTime)
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

    private void ProcessPathfinding()
    {
        navigationAgent3D.TargetPosition = targetPawn?.GlobalPosition ?? GlobalPosition;
        if (navigationAgent3D.TargetPosition == GlobalPosition)
        {
            velocityNoGravity = Vector3.Zero;
            Velocity = velocityNoGravity + Vector3.Down * 1f;
            return;
        }

        var targetPos = navigationAgent3D.GetNextPathPosition(); // required every physics frame

        // ! randomize path better, this is bad
        var distSqr = targetPos.DistanceSquaredTo(targetPawn.Position);
        targetPos += new Vector3(mobRng.Randf() - 0.5f, -0.1f, mobRng.Randf() - 0.5f) * distSqr * 0.05f;

        velocityNoGravity = GlobalPosition.DirectionTo(targetPos) * MobResource.MovementSpeedScale * 3f;

        if (velocityNoGravity.Y > 0.05f)
            Velocity = velocityNoGravity + Vector3.Down * 0.2f;
        else
            Velocity = velocityNoGravity + Vector3.Down * 1f;
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
            Debug.DebugDot(entryPos, pointDotSize, pointColor);
            Debug.DebugDot(entryPos + entryOut, handleDotSize, handleColor);
            Debug.DebugDot(jumpApexPos + centerIn, handleDotSize, handleColor);
            Debug.DebugDot(jumpApexPos, pointDotSize, pointColor);
            Debug.DebugDot(jumpApexPos + centerOut, handleDotSize, handleColor);
            Debug.DebugDot(exitPos + exitIn, handleDotSize, handleColor);
            Debug.DebugDot(exitPos, pointDotSize, pointColor);
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
                Debug.DebugDot(verticalActionMovementCurve.Samplef(i / 10f), msToDelete: 1000);
            }
        }
    }

    private void ChangeNavigationTarget()
    {
        // weight on highest damage dealers?

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