namespace MurderFloor;

public partial class Player : Pawn
{
    [Export]
    public Vector3 NetworkedVelocity { get; set; } = Vector3.Zero;

    private Vector3 lastVel;
    private bool grounded;

    public void AddVelocity(Vector3 vel)
    {
        lastVel += vel;
    }

    private void PhysicsProcessMovement()
    {
        var forward = Input.GetAxis("forward", "backward");
        var strafe = Input.GetAxis("left", "right");
        var input = new Vector3(strafe, 0f, forward);
        var wishVel = input.Normalized();

        if (IsWalking())
        {
            if (IsAiming())
            {
                if (wishVel.Z > 0f) wishVel.Z *= 0.75f;
                wishVel.X *= 0.75f;
            }
            wishVel *= 0.33f;
        }
        else
        {
            wishVel *= 0.52f;
        }

        var result = Trace(Position, Position + Vector3.Down * 0.1f);

        if (result.Hit && !grounded)
        {
            AddViewmodelPositionKick(new Vector3(0, 0.02f, 0));
        }

        grounded = result.Hit;

        // ! does not reduce from joystick
        // abs input
        // wishMove.X *= input.X
        // wishMove.Z *= input.Z

        wishVel = wishVel.Rotated(Vector3.Up, ViewAngle.X);
        if (grounded && Input.IsActionJustPressed("jump"))
        {
            AddViewmodelPositionKick(new Vector3(0, -0.065f, 0), 0.7f);
            wishVel.Y = 14f;
        }
        lastVel *= new Vector3(0.86f, 0.95f, 0.86f);
        lastVel += wishVel;

        Gravity();

        Velocity = lastVel;

        MoveAndSlide();
        NetworkedVelocity = Velocity;
        lastVel = Velocity;
    }

    private void Gravity()
    {
        lastVel.Y -= 0.25f;
        if (lastVel.Y < -0.3f) lastVel.Y *= 1.04f;
        lastVel.Y = Mathf.Clamp(lastVel.Y, -25, 10);
    }

    private bool IsWalking()
    {
        if (Input.IsActionPressed("walk")) return true;
        if (IsAiming()) return true;

        return false;
    }

    private bool IsAiming()
    {
        if (SelectedTool is not null && SelectedTool.Aiming) return true;

        return false;
    }

    private struct TraceInfo
    {
        public bool Hit { get; set; }
        public Godot.Collections.Dictionary Result { get; set; }
        public float Fraction { get; set; } // i dont remember what this is
        public bool StartedSolid { get; set; }
    }

    private TraceInfo Trace(Vector3 start, Vector3 end)
    {
        var query = PhysicsRayQueryParameters3D.Create(start, end);
        var results = GetWorld3D().DirectSpaceState.IntersectRay(query);
        if (results.Count == 0) return new TraceInfo() { Hit = false };

        return new TraceInfo() { Hit = true, Result = results, Fraction = 1f, StartedSolid = results["position"].AsVector3() == start };
    }
}