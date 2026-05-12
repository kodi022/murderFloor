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

    public bool Active
    {
        get { return _active; }
        set
        {
            worldModels.Visible = value;
            SetProcess(value);
            SetPhysicsProcess(value);
            _active = value;
        }
    }

    private bool _active = false;

    private static readonly RandomNumberGenerator mobRng = new();

    [Export]
    private Node3D worldModels;
    [Export]
    private NavigationAgent3D navigationAgent3D;

    private Player targetPlayer;

    private ulong lastCheckpointTime;
    public override void _Ready()
    {
        Active = false;
        ChangeNavigationTarget();
        navigationAgent3D.NavigationFinished += ChangeNavigationTarget;
        navigationAgent3D.WaypointReached += (a) => { lastCheckpointTime = Time.GetTicksMsec(); };
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsMultiplayerAuthority()) return;

        if (NavigationServer3D.MapGetIterationId(navigationAgent3D.GetNavigationMap()) == 0) return;

        if (20000ul < Time.GetTicksMsec() - lastCheckpointTime)
        {
            Unstuck();
            return;
        }

        var targetPos = navigationAgent3D.GetNextPathPosition(); // required every physics frame
        Vector3 newVelocity = GlobalPosition.DirectionTo(targetPos) * 3.5f;
        Velocity = newVelocity + Vector3.Down;

        var lookingAtVel = newVelocity - new Vector3(0, newVelocity.Y, 0);
        Transform = Transform.LookingAt(GlobalPosition - lookingAtVel);

        MoveAndSlide();

        // if target check update


    }

    private void Unstuck()
    {
        var map = navigationAgent3D.GetNavigationMap();
        for (int i = 0; i < 1000; i++)
        {
            var grow = 1 + (i * 0.01f);
            var rand = GlobalPosition + new Vector3(mobRng.Randfn() * grow, mobRng.Randfn() * grow, mobRng.Randfn() * grow);
            var point = NavigationServer3D.MapGetClosestPoint(map, rand);
            if ((rand - point).LengthSquared() < 4)
            {
                GlobalPosition = point;
                lastCheckpointTime = Time.GetTicksMsec();
                break;
            }
        }
    }

    private void ChangeNavigationTarget()
    {
        navigationAgent3D.TargetPosition = new Vector3(mobRng.Randfn() * 10, mobRng.Randfn() * 10, mobRng.Randfn() * 10);

        // var nearestDist = 0f;
        // Player nearestPlr = null;
        // foreach (var plr in Player.AllPlayers)
        // {
        //     var dist = (GlobalPosition - plr.GlobalPosition).LengthSquared();

        //     if (dist < nearestDist)
        //     {
        //         nearestDist = dist;
        //         nearestPlr = plr;
        //     }
        // }

        // targetPlayer = nearestPlr;
    }
}