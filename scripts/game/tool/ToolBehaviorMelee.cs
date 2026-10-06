namespace Shooter.Game;

public class ToolBehaviorMelee : ToolBehavior
{
    public ulong RpmAsMs { get; set; } = 0;
    public ulong MsSinceFire { get; set; } = 0;

    private Resource.ToolMelee Melee => ParentTool.ToolResource as Resource.ToolMelee;

    public override void Ready()
    {
        RpmAsMs = (ulong)(60f / (float)Tc.GetStat(Resource.ToolMelee.PropertyName.RPM) * 1000f);
    }

    public override void Process(double delta)
    {

    }

    public override void FirePrimary(Resource.Tool.FireInfo fi)
    {
        var ticksMs = Time.GetTicksMsec();
        if (RpmAsMs < ticksMs - MsSinceFire)
        {
            MsSinceFire = ticksMs;

            // ParentTool.AnimationPlayer.Stop();
            // ParentTool.AnimationPlayer.Play("fire");
            // var poly = (AudioStreamPlaybackPolyphonic)fi.Player.AudioStreamPlayer3D.GetStreamPlayback();
            // poly.PlayStream(Melee.soun, bus: "Effects");

            Melee.FireMelee(fi);
        }
    }

    public override void UnfirePrimary()
    {

    }

    public override void FireSecondary()
    {

    }

    public override void UnfireSecondary()
    {

    }

    public override async void Bolt(Resource.Tool.FireInfo fi)
    {

    }

    public override async void Reload(Resource.Tool.FireInfo fi)
    {

    }
}