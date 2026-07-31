namespace MurderFloor;

public partial class Player : Pawn
{
    [Export]
    public Vector3 NetworkedVelocity { get; set; } = Vector3.Zero;

    private Vector3 lastVel;

    private void PhysicsProcessMovement()
    {
        var forward = Input.GetAxis("forward", "backward");
        var strafe = Input.GetAxis("left", "right");
        var wishMove = new Vector3(strafe, 0f, forward).Normalized(); // ! does not reduce from joystick

        if (IsWalking())
        {
            if (IsAiming())
            {
                if (wishMove.Z > 0f) wishMove.Z *= 0.75f;
                wishMove.X *= 0.75f;
            }
            wishMove *= 0.45f;
        }
        else
        {
            wishMove *= 0.72f;
        }

        wishMove = wishMove.Rotated(Vector3.Up, ViewAngle.X);
        if (Input.IsActionJustPressed("jump")) wishMove.Y = 14f;
        lastVel *= new Vector3(0.80f, 0.95f, 0.80f);
        lastVel += wishMove;

        Gravity();

        Velocity = lastVel;

        MoveAndSlide();
        NetworkedVelocity = Velocity;
        lastVel = Velocity;
    }

    private void Gravity()
    {
        lastVel.Y -= 0.25f;
        if (lastVel.Y < -1f) lastVel.Y *= 1.04f;
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
}