namespace Shooter.Game;

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
        var wishVel = input.Normalized() * 0.52f;

        if (IsWalking())
        {
            if (IsAiming())
            {
                if (wishVel.Z > 0f) wishVel.Z *= 0.60f;
                wishVel.X *= 0.78f;
            }
            wishVel *= 0.65f;
        }

        wishVel.X *= MathF.Abs(strafe);
        wishVel.Z *= MathF.Abs(forward);
        wishVel = wishVel.Rotated(Vector3.Up, ViewAngle.X);

        var result = TraceShape(Position + Vector3.Down * 0.06f);
        if (result.Hit && !grounded)
        {
            AddCameraPositionKick(new Vector3(0, -0.05f, 0), 0.9f);
            AddViewmodelPositionKick(new Vector3(0, -0.02f, 0), 0.9f);
        }

        grounded = result.Hit;
        if (grounded && Input.IsActionJustPressed("jump"))
        {
            AddCameraPositionKick(new Vector3(0, 0.02f, 0), 0.6f);
            AddViewmodelPositionKick(new Vector3(0, -0.05f, 0), 0.6f);
            wishVel.Y = 18f;
        }

        if (grounded)
        {
            lastVel *= new Vector3(0.86f, 0.95f, 0.86f);
            lastVel += wishVel;
        }
        else
        {
            lastVel *= new Vector3(0.992f, 0.95f, 0.992f);
        }

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

    private struct TraceInfoShape
    {
        public bool Hit { get; set; }
        public Godot.Collections.Dictionary Result { get; set; }
    }

    private TraceInfoShape TraceShape(Vector3 pos)
    {
        var query = new PhysicsShapeQueryParameters3D()
        {
            Shape = new CylinderShape3D() { Height = 1.8f, Radius = 0.44f },
            Transform = new Transform3D(Basis.Identity, pos + Vector3.Up * 0.9f),
            Exclude = [GetRid()],
        };
        var results = GetWorld3D().DirectSpaceState.IntersectShape(query, 1);
        if (results.Count == 0) return new TraceInfoShape() { Hit = false };

        return new TraceInfoShape() { Hit = true, Result = results[0] };
    }
}