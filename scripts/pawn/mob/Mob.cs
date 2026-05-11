namespace MurderFloor;

public partial class Mob : CharacterBody3D, IPawn
{
    [Export]
    public float MaxHealth { get; set; } = 50f;
    [Export]
    public float Health { get; set; } = 50f;
    [Export]
    public float Armor { get; set; } = 0f;

    public int MobProcessOffset { get; set; } = 0;

    [Export]
    private Node3D worldModels;
    [Export]
    private NavigationAgent3D navigationAgent3D;

    private Player targetPlayer;

    public override void _Ready()
    {
        navigationAgent3D.TargetPosition = new Vector3(15, 0, 15);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsMultiplayerAuthority()) return;

        if (NavigationServer3D.MapGetIterationId(navigationAgent3D.GetNavigationMap()) == 0) return;

        var targetPos = navigationAgent3D.GetNextPathPosition(); // required every physics frame
        Vector3 newVelocity = GlobalPosition.DirectionTo(targetPos) * 10;
        Velocity = newVelocity + Vector3.Down;
        MoveAndSlide();

        // if target check update



        foreach (var target in Player.AllPlayers)
        {
            //target.pos
        }
    }
}