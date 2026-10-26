namespace Shooter.Game;

public partial class Mob : Pawn
{
    private Vector3 velocityNoGravity;

    private ulong lastWaypointTime;
    private ulong lastTargetUpdateTime;

    private bool verticalAction;
    private ulong verticalActionStartTime;
    private ulong verticalActionLengthMs;
    private Curve3D verticalActionMovementCurve;

    private int startPosHash;

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
            Position = verticalActionMovementCurve.Samplef(timeOffset / (float)verticalActionLengthMs * (pointCount - 1));
            if (timeOffset >= verticalActionLengthMs)
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
        if (targetPawn is null || targetPawn.IsDead)
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

        var hash = Utils.Hashing.StableHash(targetPos) + startPosHash;
        var rand = new Vector3(hash % 17 / 17f, hash % 13 / 13f, hash % 11 / 11f);
        var distSqr = targetPos.DistanceSquaredTo(targetPawn.Position);
        targetPos += rand * distSqr * 0.05f;

        velocityNoGravity = GlobalPosition.DirectionTo(targetPos) * MobResource.MovementSpeedScale * 2.8f * levelSpeedBuff;

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
            verticalActionLengthMs = (ulong)(-exitHeightDiff * 450f * levelVerticalActionBuff);
            verticalActionMovementCurve = new();

            var climbHeight = -exitHeightDiff + 0.1f;
            var wallTopPos = entryPos + Vector3.Up * climbHeight;
            var travelDirection = entryPos.DirectionTo(exitPos);
            var horizontalDirection = new Vector3(travelDirection.X, 0f, travelDirection.Z).Normalized();
            var climbHandleLength = climbHeight * 0.5f;
            var travelHandleLength = entryPos.DistanceTo(exitPos) * 0.35f;
            var wallTopOut = horizontalDirection * travelHandleLength;

            verticalActionMovementCurve.AddPoint(entryPos, @out: Vector3.Up * climbHandleLength);
            verticalActionMovementCurve.AddPoint(
                wallTopPos,
                @in: Vector3.Down * climbHandleLength,
                @out: wallTopOut);
            verticalActionMovementCurve.AddPoint(exitPos, @in: -wallTopOut);

            int pointDotSize = 6, handleDotSize = 4;
            Color pointColor = new(1, 1, 0), handleColor = new(0, 1, 0);
            Debug.Rendering.Point(entryPos, pointDotSize, pointColor);
            Debug.Rendering.Point(entryPos + Vector3.Up * climbHandleLength, handleDotSize, handleColor);
            Debug.Rendering.Point(wallTopPos + Vector3.Down * climbHandleLength, handleDotSize, handleColor);
            Debug.Rendering.Point(wallTopPos, pointDotSize, pointColor);
            Debug.Rendering.Point(wallTopPos + wallTopOut, handleDotSize, handleColor);
            Debug.Rendering.Point(exitPos - wallTopOut, handleDotSize, handleColor);
            Debug.Rendering.Point(exitPos, pointDotSize, pointColor);
        }

        // drop
        if (exitHeightDiff > 0f)
        {
            verticalAction = true;
            verticalActionStartTime = Time.GetTicksMsec();
            verticalActionLengthMs = (ulong)(exitHeightDiff * 250f * levelVerticalActionBuff);
            verticalActionMovementCurve = new();
            var dropHeight = exitHeightDiff;
            var travelDirection = entryPos.DirectionTo(exitPos);
            var horizontalDirection = new Vector3(travelDirection.X, 0f, travelDirection.Z).Normalized();
            var launchHandle = horizontalDirection * entryPos.DistanceTo(exitPos) * 0.35f;
            verticalActionMovementCurve.AddPoint(entryPos, @out: launchHandle);
            verticalActionMovementCurve.AddPoint(exitPos, @in: Vector3.Up * dropHeight * 0.65f);

            for (int i = 0; i < 10; i++)
            {
                Debug.Rendering.Point(verticalActionMovementCurve.Samplef(i / 10f), msToDelete: 1000);
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