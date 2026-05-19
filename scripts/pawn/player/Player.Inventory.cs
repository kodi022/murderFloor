using System.Threading.Tasks;

namespace MurderFloor;

public partial class Player : Pawn
{
    public List<LiveTool> ToolsPrimary { get; private set; } = [];
    public List<LiveTool> ToolsSecondary { get; private set; } = [];
    public List<LiveTool> ToolsGadget { get; private set; } = [];
    public List<LiveTool> ToolsMelee { get; private set; } = [];

    public int SelectedToolIndex { get; private set; } = 0;
    // reference from tool list
    public LiveTool SelectedTool = null;

    public bool SwappingWeapon { get; private set; }

    // camera.ProjectPosition aims at pixel, which probably isn't perfectly center
    // var endPos = camera.ProjectPosition(Vector2.Zero, 100f);
    // this uses camera rotation, perfectly center
    // var endPos = camera.GlobalPosition - camera.GlobalTransform.Basis.Z * 100f;

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void AddTool(string toolId)
    {
        var tool = ResourceManager.ToolRegistry.GetResourceReference(toolId);
        switch (tool.GetSlot())
        {
            case Tool.SlotEnum.Primary:
                break;
        }
        // ! finish this later
    }

    public async void SelectToolByIndex(int index)
    {
        if (SwappingWeapon) return;

        SwappingWeapon = true;
        await SelectedTool?.Unequip();

        async Task FinishSelection(int index)
        {
            if (IsMultiplayerAuthority()) Rpc("SwitchToolRpc", index);
            SelectedToolIndex = index;
            await SelectedTool.Equip();
            SwappingWeapon = false;
        }

        if (index < 0)
        {
            SelectedTool = ToolsPrimary[0];
            await FinishSelection(0);
            return;
        }

        var count = ToolsPrimary.Count;
        if (index < count)
        {
            SelectedTool = ToolsPrimary[index];
            await FinishSelection(index);
            return;
        }

        count += ToolsSecondary.Count;
        if (index < count)
        {
            SelectedTool = ToolsSecondary[index - ToolsPrimary.Count];
            await FinishSelection(index);
            return;
        }

        count += ToolsGadget.Count;
        if (index >= count)
        {
            SelectedTool = ToolsPrimary[0];
            await FinishSelection(0);
            return;
        }

        SelectedTool = ToolsGadget[index - ToolsPrimary.Count - ToolsSecondary.Count];
        SelectedToolIndex = index;
        await FinishSelection(index);
    }

    // useful for scrolling
    public void SelectToolByDelta(int delta)
    {
        SelectToolByIndex(SelectedToolIndex + delta);
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void SwitchToolRpc(int index)
    {
        SelectToolByIndex(index);
    }
}