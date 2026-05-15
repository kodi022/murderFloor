namespace MurderFloor;

public partial class Player : Pawn
{
    public List<LiveTool> ToolsPrimary { get; private set; } = [];
    public List<LiveTool> ToolsSecondary { get; private set; } = [];
    public List<LiveTool> ToolsGadget { get; private set; } = [];

    public int SelectedToolIndex { get; private set; } = 0;
    // reference from tool list
    public LiveTool SelectedTool = null;

    // camera.ProjectPosition aims at pixel, which probably isn't perfectly center
    // var endPos = camera.ProjectPosition(Vector2.Zero, 100f);
    // this uses camera rotation, perfectly center
    // var endPos = camera.GlobalPosition - camera.GlobalTransform.Basis.Z * 100f;

    public void SelectToolByIndex(int index)
    {
        if (index < 0)
        {
            SelectedTool = ToolsPrimary[0];
            SelectedToolIndex = 0;
            return;
        }

        var count = ToolsPrimary.Count;
        if (index < count)
        {
            SelectedTool = ToolsPrimary[index];
            SelectedToolIndex = index;
            return;
        }

        count += ToolsSecondary.Count;
        if (index < count)
        {
            SelectedTool = ToolsSecondary[index - ToolsPrimary.Count];
            SelectedToolIndex = index;
            return;
        }

        count += ToolsGadget.Count;
        if (index >= count)
        {
            SelectedTool = ToolsPrimary[0];
            SelectedToolIndex = 0;
            return;
        }

        SelectedTool = ToolsGadget[index - ToolsPrimary.Count - ToolsSecondary.Count];
        SelectedToolIndex = index;
    }

    // useful for scrolling
    public void SelectToolByDelta(int delta)
    {
        SelectToolByIndex(SelectedToolIndex + delta);
    }

    public void AttachWeaponToHand()
    {
        foreach (var child in viewToolPosition.GetChildren())
        {
            child.Free();
        }

        var tool = ResourceManager.ToolRegistry.GetResourceReference("base:testpistol");
        viewToolPosition.AddChild(tool.MeshScene.Instantiate());
    }
}