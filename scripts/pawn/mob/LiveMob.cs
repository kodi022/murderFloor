using System.Runtime.Intrinsics;

namespace MurderFloor;

public partial class LiveMob : Pawn
{
    //public Mob MobResource { get; set; }

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

    public int MobProcessOffset { get; set; } = 0;

    private static readonly RandomNumberGenerator mobRng = new();

    [Export]
    private Node3D worldModels;
    [Export]
    private NavigationAgent3D navigationAgent3D;

    private bool _active;
    private Pawn targetPawn;
    private ulong lastCheckpointTime;
    private ulong lastTargetUpdateTime;

    public override void _Ready()
    {
        Active = false;
        ChangeNavigationTarget();
        navigationAgent3D.NavigationFinished += ChangeNavigationTarget;
        navigationAgent3D.WaypointReached += (a) => { lastCheckpointTime = Time.GetTicksMsec(); };
    }

    public void ActivateAsType(Vector3 location, int mobResourceId)
    {

    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsMultiplayerAuthority()) return;

        if (NavigationServer3D.MapGetIterationId(navigationAgent3D.GetNavigationMap()) == 0) return;

        navigationAgent3D.TargetPosition = targetPawn?.GlobalPosition ?? GlobalPosition;

        //if (navigationAgent3D.IsTargetReached()) return;

        if (10000ul < Time.GetTicksMsec() - lastTargetUpdateTime)
        {
            ChangeNavigationTarget();
            return;
        }

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
        lastCheckpointTime = Time.GetTicksMsec();
        for (int i = 0; i < 1000; i++)
        {
            var grow = 1 + (i * 0.01f);
            var rand = GlobalPosition + new Vector3(mobRng.Randfn() * grow, mobRng.Randfn() * grow, mobRng.Randfn() * grow);
            var point = NavigationServer3D.MapGetClosestPoint(map, rand);
            if ((rand - point).LengthSquared() < 4)
            {
                GlobalPosition = point;
                break;
            }
        }
    }

    private void ChangeNavigationTarget()
    {
        lastTargetUpdateTime = Time.GetTicksMsec();
        // weight on highest damage dealers?

        //navigationAgent3D.TargetPosition = new Vector3(mobRng.Randfn() * 10, mobRng.Randfn() * 10, mobRng.Randfn() * 10);

        // if not team attack

        // else find teammate pawns
        GD.Print(Player.AllPlayers.Count);
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
}