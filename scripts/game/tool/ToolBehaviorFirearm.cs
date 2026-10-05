namespace Shooter.Game;

public class ToolBehaviorFirearm : ToolBehavior
{
    public bool Reloading { get; private set; } = false;
    public ulong RpmAsMs { get; set; } = 0;
    public ulong MsSinceFire { get; set; } = 0;
    public bool Bolting { get; set; } = false;
    public bool ShotSemi { get; set; } = false;
    public bool ShotBolt { get; set; } = false;
    // toolfirearm
    public Vector2 CurrentSpread { get; private set; }
    public Vector2 MinSpread { get; private set; }
    public Vector2 MaxSpread { get; private set; }

    private Resource.ToolFirearm Firearm => ParentTool.ToolResource as Resource.ToolFirearm;

    public override void Ready()
    {
        RpmAsMs = (ulong)(60f / (float)tc.GetStat(Resource.ToolFirearm.PropertyName.RPM) * 1000f);
        CurrentSpread = (Vector2)tc.GetStat(Resource.ToolFirearm.PropertyName.InitialDegreeSpread);
    }

    public override void Process(double delta)
    {
        var plrVel = ParentTool.Player.Velocity.LengthSquared();
        var movementPenalty = Vector2.One;
        if (plrVel > 8f)
            movementPenalty = (Vector2)tc.GetStat(Resource.ToolFirearm.PropertyName.FastWalkSpreadMult);
        else if (plrVel > 1f)
            movementPenalty = (Vector2)tc.GetStat(Resource.ToolFirearm.PropertyName.SlowWalkSpreadMult);

        var aimBuff = ParentTool.Aiming ? (Vector2)tc.GetStat(Resource.ToolFirearm.PropertyName.AimSpreadMult) : Vector2.One;

        MinSpread = (Vector2)tc.GetStat(Resource.ToolFirearm.PropertyName.InitialDegreeSpread) * aimBuff * movementPenalty;
        MaxSpread = Firearm.MaxDegreeSpread * movementPenalty;

        var recoveryRate = Vector2.One * Firearm.SpreadRecoveryRate * (float)delta;

        if (CurrentSpread < MinSpread)
            CurrentSpread += (Vector2.One * (float)delta * 50f).Min(MinSpread);
        else
            CurrentSpread = (CurrentSpread - recoveryRate).Max(MinSpread);
    }

    public override void FirePrimary(Resource.Tool.FireInfo fi)
    {
        if (Bolting) return;
        if (Reloading) return;
        if (Firearm.FireMode == Resource.ToolFirearm.FireModeEnum.Semi && ShotSemi) return;

        if (ParentTool.CurrentMag <= 0)
        {
            Reload(fi);
            return;
        }

        if (ShotBolt && Firearm.FireMode == Resource.ToolFirearm.FireModeEnum.Manual)
        {
            if (!ShotSemi) Bolt(fi);
            return;
        }

        var ticksMs = Time.GetTicksMsec();
        if (RpmAsMs < ticksMs - MsSinceFire)
        {
            MsSinceFire = ticksMs;
            Firearm.FireBullet(fi);

            ParentTool.AnimationPlayer.Stop();
            ParentTool.AnimationPlayer.Play("fire");
            var poly = (AudioStreamPlaybackPolyphonic)fi.Player.AudioStreamPlayer3D.GetStreamPlayback();
            poly.PlayStream(Firearm.FireSound, bus: "Effects");

            ParentTool.MuzzleFlashParticle.Restart();

            ShotSemi = true;
            ShotBolt = true;
            CurrentSpread = (CurrentSpread + Firearm.SpreadIncreasePerShot).Min(MaxSpread);
            ParentTool.CurrentMag--;
        }
    }

    public override void UnfirePrimary()
    {
        ShotSemi = false;
    }

    public override void FireSecondary()
    {
        ParentTool.Aiming = true;
    }

    public override void UnfireSecondary()
    {
        ParentTool.Aiming = false;
    }

    public override async void Bolt(Resource.Tool.FireInfo fi)
    {
        if (Bolting) return;
        if (Reloading) return;
        if (ParentTool.CurrentMag <= 0) return;
        Bolting = true;

        if (ParentTool.AnimationPlayer.HasAnimation("bolt"))
        {
            await ParentTool.TaskAnimation("bolt", (int)tc.GetStat(Resource.ToolFirearm.PropertyName.ManualFireDelayMs));
        }
        else
        {
            fi.Player.AddViewmodelPositionKick(new Vector3(0, 0, 0.1f), 2);
            await Task.Delay((int)tc.GetStat(Resource.ToolFirearm.PropertyName.ManualFireDelayMs) - 200);
            fi.Player.AddViewmodelPositionKick(new Vector3(0, 0, -0.05f), 2);
            await Task.Delay(200);
        }

        var poly = (AudioStreamPlaybackPolyphonic)fi.Player.AudioStreamPlayer3D.GetStreamPlayback();
        poly.PlayStream(Firearm.ManualFireSound, bus: "Effects");

        Bolting = false;
        ShotBolt = false;
    }

    public override async void Reload(Resource.Tool.FireInfo fi)
    {
        if (Bolting) return;
        if (Reloading) return;
        if (ParentTool.CurrentMag >= (int)tc.GetStat(Resource.ToolFirearm.PropertyName.MagSize)) return;
        if (ParentTool.CurrentReserve <= 0) return;
        Reloading = true;

        if (ParentTool.AnimationPlayer.HasAnimation("reload"))
        {
            await ParentTool.TaskAnimation("reload", Firearm.ReloadDelayMs);
        }
        else
        {
            fi.Player.AddViewmodelRotationKick(new Vector3(-1f, 0.5f, 0));
            await Task.Delay(Firearm.ReloadDelayMs - 200);
            fi.Player.AddViewmodelPositionKick(new Vector3(0, 0, 0.1f));
            fi.Player.AddViewmodelRotationKick(new Vector3(0.2f, 0, 0));
            await Task.Delay(200);
        }

        var poly = (AudioStreamPlaybackPolyphonic)fi.Player.AudioStreamPlayer3D.GetStreamPlayback();
        poly.PlayStream(Firearm.ReloadSound, bus: "Effects");

        //await reloadanimation

        if (Firearm.EndlessReserve)
        {
            ParentTool.CurrentMag = Firearm.MagSize;
            Reloading = false;
            return;
        }

        var diff = Firearm.MagSize - ParentTool.CurrentMag;
        if (diff >= ParentTool.CurrentReserve)
        {
            ParentTool.CurrentMag += ParentTool.CurrentReserve;
            ParentTool.CurrentReserve = 0;
        }
        else
        {
            ParentTool.CurrentMag = Firearm.MagSize;
            ParentTool.CurrentReserve -= diff;
        }

        Reloading = false;
        ShotBolt = false;
    }
}