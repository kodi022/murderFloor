namespace Shooter.Game;

public class ToolBehavior
{
    public Tool ParentTool { get; set; }
    private protected ToolConfig Tc => ParentTool.ToolConfig;

    public virtual void Ready()
    {

    }

    public virtual void Process(double delta)
    {

    }

    public virtual void FirePrimary(Resource.Tool.FireInfo fi)
    {

    }

    public virtual void UnfirePrimary()
    {

    }

    public virtual void FireSecondary()
    {

    }

    public virtual void UnfireSecondary()
    {

    }

    public virtual void Bolt(Resource.Tool.FireInfo fi)
    {

    }

    public virtual void Reload(Resource.Tool.FireInfo fi)
    {

    }
}