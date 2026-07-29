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
            if (wishMove.X < 0f) wishMove.X *= 0.2f;
            wishMove *= 0.42f;
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
        if (SelectedTool is not null && SelectedTool.Aiming) return true;

        return false;
    }
}