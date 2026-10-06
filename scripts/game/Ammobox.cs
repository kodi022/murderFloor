namespace Shooter.Game;

public partial class Ammobox : Node3D
{
    public override void _Ready()
    {
        GetChild<Usable>(0).UseAction = () =>
        {
            foreach (var tool in Player.Self.GetAllTools())
            {
                if (tool.ToolBehavior is ToolBehaviorFirearm tbf)
                    tbf.FillReserve();
            }

            Free();
        };
    }
}
