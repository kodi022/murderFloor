namespace MurderFloor;

public partial class Player : CharacterBody3D
{
    [Export]
    private Camera3D camera;
    [Export]
    private Node3D body;

    [Export(PropertyHint.Range, "1,500,0.01")]
    private float sensitivity = 50f;

    private Vector3 lastVel;

    [Export]
    private float viewYaw = 0f;
    [Export]
    private float viewPitch = 0f;

    private Input.MouseModeEnum mouseMode = Input.MouseModeEnum.Captured;

    public override void _Ready()
    {
        if (!IsMultiplayerAuthority()) return;

        body.Free();
        Input.UseAccumulatedInput = false;
        camera.Current = true;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!IsMultiplayerAuthority()) return;

        if (mouseMode != Input.MouseModeEnum.Captured) return;

        if (@event is InputEventMouseMotion eventMouseMotion)
        {
            var viewportTransform = GetTree().Root.GetFinalTransform();
            var relative = ((InputEventMouseMotion)eventMouseMotion.XformedBy(viewportTransform)).Relative;
            viewPitch -= relative.Y * 0.0001f * sensitivity;
            viewPitch = float.Clamp(viewPitch, -1.3f, 1.3f);

            viewYaw -= relative.X * 0.0001f * sensitivity;
        }
    }

    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority()) return;

        Input.MouseMode = mouseMode;

        if (Input.IsActionJustPressed("exit"))
        {
            if (mouseMode == Input.MouseModeEnum.Captured) mouseMode = Input.MouseModeEnum.Visible;
            else mouseMode = Input.MouseModeEnum.Captured;
        }

        camera.Rotation = new Vector3(viewPitch, 0, 0);
        Rotation = new Vector3(0, viewYaw, 0);
    }

    public override void _PhysicsProcess(double delta)
    {
        //GD.Print(Name + IsMultiplayerAuthority());
        if (!IsMultiplayerAuthority()) return;

        var forward = Input.GetAxis("backward", "forward");
        var strafe = Input.GetAxis("left", "right");
        var wishMove = new Vector3(forward, -1f, strafe);
        wishMove = wishMove.Normalized();
        wishMove = wishMove.Rotated(Vector3.Up, viewYaw + 1.570796326794896f);
        lastVel *= new Vector3(0.86f, 0.95f, 0.86f);
        lastVel += wishMove;

        Velocity = lastVel;
        MoveAndSlide();
        lastVel = Velocity;
    }
}
