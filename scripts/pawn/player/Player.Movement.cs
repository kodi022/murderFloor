namespace MurderFloor;

public partial class Player : Pawn
{
    [Export]
    public Vector3 NetworkedVelocity { get; set; } = Vector3.Zero;

    private Vector3 lastVel;

    private void PhysicsProcessMovement()
    {
        var forward = Input.GetAxis("backward", "forward");
        var strafe = Input.GetAxis("left", "right");
        var moveScale = Input.IsActionPressed("walk") ? 0.42f : 0.7f;
        var wishMove = new Vector3(forward, 0f, strafe).Normalized() * moveScale;
        wishMove = wishMove.Rotated(Vector3.Up, ViewAngle.X + 1.570796326794896f);
        if (Input.IsActionJustPressed("jump")) wishMove.Y = 14f;
        lastVel *= new Vector3(0.80f, 0.95f, 0.80f);
        lastVel += wishMove;
        lastVel.Y -= 0.25f;
        if (lastVel.Y < -1f) lastVel.Y *= 1.04f;
        lastVel.Y = Mathf.Clamp(lastVel.Y, -25, 10);
        Velocity = lastVel;

        MoveAndSlide();
        NetworkedVelocity = Velocity;
        lastVel = Velocity;
    }
}