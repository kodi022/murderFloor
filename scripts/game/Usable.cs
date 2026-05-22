namespace MurderFloor;

[GlobalClass]
public partial class Usable : Node3D
{
    [Export]
    public Control UiToOpen { get; private set; }

    public void UsableHit()
    {
        GD.Print("hit me!!");
    }

    public void UsableInvoke()
    {
        GD.Print("invoke me!!");
    }
}