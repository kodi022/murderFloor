
namespace MurderFloor;

public partial class LiveMob : Pawn
{
    public Mob MobResource { get; set; }

    [Export]
    public bool Active
    {
        get { return _active; }
        private set
        {
            worldModels.Visible = value;
            collisionShape3D.SetDeferred("disabled", !value);
            SetProcess(value);
            SetPhysicsProcess(value);
            _active = value;
        }
    }

    public int MobProcessOffset { get; set; } = 0;
    public int MobPoolId { get; set; } = 0;

    private static readonly RandomNumberGenerator mobRng = new();

    [Export]
    private Node3D worldModels;
    [Export]
    private NavigationAgent3D navigationAgent3D;
    [Export]
    private CollisionShape3D collisionShape3D;

    private bool _active;
    private Pawn targetPawn;
    private ulong lastCheckpointTime;
    private ulong lastTargetUpdateTime;

    public override void _Ready()
    {
        Active = false;
        navigationAgent3D.NavigationFinished += ChangeNavigationTarget;
        navigationAgent3D.WaypointReached += (a) => { lastCheckpointTime = Time.GetTicksMsec(); };
    }

    public void OnSpawn(Vector3 location, int mobResourceId)
    {
        MaxHealth = 100;
        Health = MaxHealth;
        GlobalPosition = location;
        Active = true;
        ChangeNavigationTarget();
    }

    public override void OnDeath(Godot.Collections.Dictionary<string, string> damageInfo)
    {
        if (!Active) return;
        Active = false;
        Game.Current.MobDeath(MobPoolId);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!Active) return;
        if (!IsMultiplayerAuthority()) return;
        if (NavigationServer3D.MapGetIterationId(navigationAgent3D.GetNavigationMap()) == 0) return;

        navigationAgent3D.TargetPosition = targetPawn?.GlobalPosition ?? GlobalPosition;

        if (navigationAgent3D.DistanceToTarget() < 1)
        {
            var di = new Godot.Collections.Dictionary<string, string>()
            {
                {"damage", 0.ToString()},
                {"attacker", 0.ToString()},
                {"attackerName", "mobname"},
                {"weapon", "claw"},
                {"hitposition", Vector3.Zero.ToString()},
                {"hitbox", "0"}
            };
            targetPawn.Rpc("OnDamage", di);
        }

        var ticksMs = Time.GetTicksMsec();
        if (10000ul < ticksMs - lastTargetUpdateTime)
        {
            lastTargetUpdateTime = ticksMs;
            ChangeNavigationTarget();
            return;
        }

        if (20000ul < ticksMs - lastCheckpointTime)
        {
            lastCheckpointTime = ticksMs;
            Unstuck();
            return;
        }

        var targetPos = navigationAgent3D.GetNextPathPosition(); // required every physics frame
        Vector3 newVelocity = GlobalPosition.DirectionTo(targetPos) * 3.2f;
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
                break;
            }
        }
    }

    private void ChangeNavigationTarget()
    {
        // weight on highest damage dealers?

        //navigationAgent3D.TargetPosition = new Vector3(mobRng.Randfn() * 10, mobRng.Randfn() * 10, mobRng.Randfn() * 10);

        // smart weighting to have a higher overriding range for players who do more damage
        // if not team attack

        // else find teammate pawns

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