namespace MurderFloor;

[GlobalClass]
public partial class Usable : Node3D
{
    [Export]
    public string UseInfoText { get; private set; }
    [Export]
    public string UiSceneToOpen { get; private set; }

    public void UsableHit()
    {

    }

    public void UsableInvoke()
    {
        Player.Self.OpenUI(UiSceneToOpen);
    }
}