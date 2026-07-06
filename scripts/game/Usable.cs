namespace MurderFloor;

public partial class Usable : Node3D
{
    [Export]
    public string UseInfoText { get; private set; }
    [Export]
    public string UiSceneToOpen { get; private set; }

    public Action UseAction { get; set; }

    public void UsableHit()
    {

    }

    public void UsableInvoke()
    {
        if (!string.IsNullOrEmpty(UiSceneToOpen))
        {
            Player.Self.OpenUI(UiSceneToOpen);
            return;
        }

        UseAction?.Invoke();
    }
}