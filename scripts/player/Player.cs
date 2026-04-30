namespace MurderFloor;

public partial class Player : CharacterBody3D
{
    [Export]
    private Camera3D camera;

    [Export(PropertyHint.Range, "1,100,0.1")]
    private float sensitivity = 50f;

    private Vector3 lastVel;
    private float viewYaw = 0f, viewPitch = 0f;

    private Input.MouseModeEnum mouseMode = Input.MouseModeEnum.Captured;

    public override void _Ready()
    {
        Input.UseAccumulatedInput = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (mouseMode != Input.MouseModeEnum.Captured) return;

        if (@event is InputEventMouseMotion eventMouseMotion)
        {
            var viewportTransform = GetTree().Root.GetFinalTransform();
            var relative = ((InputEventMouseMotion)eventMouseMotion.XformedBy(viewportTransform)).Relative;
            viewPitch -= relative.Y * 0.0002f * sensitivity;
            viewPitch = float.Clamp(viewPitch, -1.3f, 1.3f);

            viewYaw -= relative.X * 0.0002f * sensitivity;
        }
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("exit"))
        {
            if (mouseMode == Input.MouseModeEnum.Captured) mouseMode = Input.MouseModeEnum.Visible;
            else mouseMode = Input.MouseModeEnum.Captured;
        }
        Input.MouseMode = mouseMode;

        camera.Rotation = new Vector3(viewPitch, 0, 0);
        Rotation = new Vector3(0, viewYaw, 0);

        var forward = Input.GetAxis("backward", "forward");
        var strafe = Input.GetAxis("left", "right");
        var wishMove = new Vector3(forward, -1f, strafe);
        wishMove = wishMove.Normalized();
        wishMove = wishMove.Rotated(Vector3.Up, viewYaw + 1.570796326794896f);
        lastVel *= new Vector3(0.90f, 0.95f, 0.90f);
        lastVel += wishMove;

        Velocity = lastVel;
        MoveAndSlide();
        lastVel = Velocity;
    }
}
