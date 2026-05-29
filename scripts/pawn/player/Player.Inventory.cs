namespace MurderFloor;

public partial class Player : Pawn
{
    public List<LiveTool> ToolsPrimary { get; private set; } = [];
    public List<LiveTool> ToolsSecondary { get; private set; } = [];
    public List<LiveTool> ToolsSpecial { get; private set; } = [];
    public List<LiveTool> ToolsMelee { get; private set; } = [];

    public Tool.SlotEnum SelectedSlot { get; private set; } = Tool.SlotEnum.Primary;
    public int SelectedToolIndex { get; private set; } = 0;

    // reference from tool list
    public LiveTool SelectedTool = null;

    public bool SwappingWeapon { get; private set; }

    private bool toolsSynced = false;

    // this should only be called using Rpc
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void ToolAddRpc(string toolId)
    {
        ToolAdd(toolId);
    }

    private void ToolAdd(string toolId)
    {
        var liveTool = (LiveTool)GD.Load<PackedScene>("res://scenes/LiveTool.tscn").Instantiate();
        liveTool.PlayerId = GetMultiplayerAuthority();
        liveTool.ToolFullId = toolId;
        ToolsNode.AddChild(liveTool);
        var list = GetToolListFromTool(liveTool.ToolFullId);
        list.Add(liveTool);
    }

    /// <summary> this should only be called using Rpc </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void ToolRemoveRpc(string toolId)
    {
        foreach (var tool in ToolsNode.GetChildren())
        {
            if (tool is not LiveTool) continue;

            LiveTool liveTool = (LiveTool)tool;
            if (liveTool.ToolFullId == toolId)
            {
                var list = GetToolListFromTool(liveTool.ToolFullId);
                foreach (var item in list)
                {
                    if (item == tool)
                    {
                        list.Remove(item);
                        break;
                    }
                }
                tool.QueueFree();
            }
        }
    }

    /// <summary> this should only be called using Rpc </summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void ToolsSyncRpc(Godot.Collections.Array<string> toolIds, int slot, int index)
    {
        if (toolsSynced) return;
        toolsSynced = true;

        GD.Print("ToolSyncRpc " + Name);

        foreach (var toolId in toolIds)
            ToolAdd(toolId);

        SelectedSlot = (Tool.SlotEnum)slot;
        SelectedToolIndex = index;
        ToolEquip(slot, index);
    }

    /// <summary> this should NOT be called using Rpc, will do so internally </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public async void ToolEquip(int slot, int index)
    {
        GD.Print("ToolEquip " + Name);
        if (!IsMultiplayerAuthority())
        {
            SelectedSlot = (Tool.SlotEnum)slot;
            SelectedToolIndex = index;
        }

        if (SwappingWeapon) return;
        List<LiveTool> list = GetToolListFromSlot(SelectedSlot);
        if (list.Count == 0) return;
        if (SelectedTool is not null && SelectedTool == list[SelectedToolIndex]) return;

        Rpc("ToolEquip", slot, index);
        SwappingWeapon = true;
        if (SelectedTool is not null) await SelectedTool.Unequip();

        SelectedTool = list[SelectedToolIndex];

        await SelectedTool.Equip();
        SwappingWeapon = false;
    }

    // useful for scrolling
    public void SelectToolByDelta(int delta)
    {
        if (SwappingWeapon) return;

        if (SelectedToolIndex + delta < 0)
        {
            SelectedSlot = (Tool.SlotEnum)(((int)SelectedSlot + 4 - 1) % 4);
            SelectedToolIndex = GetToolListFromSlot(SelectedSlot).Count - 1;
        }

        if (SelectedToolIndex + delta >= GetToolListFromSlot(SelectedSlot).Count)
        {
            SelectedSlot = (Tool.SlotEnum)(((int)SelectedSlot + 1) % 4);
            SelectedToolIndex = 0;
        }

        ToolEquip((int)SelectedSlot, SelectedToolIndex);
    }

    public void SelectToolBySlot(Tool.SlotEnum slot)
    {
        if (SwappingWeapon) return;

        var list = GetToolListFromSlot(slot);
        if (list.Count == 0) return;

        if (slot == SelectedSlot)
        {
            SelectedToolIndex++;
            if (SelectedToolIndex >= list.Count) SelectedToolIndex = 0;
            ToolEquip((int)SelectedSlot, SelectedToolIndex);
            return;
        }

        SelectedSlot = slot;
        SelectedToolIndex = 0;
        ToolEquip((int)SelectedSlot, SelectedToolIndex);
    }

    private List<LiveTool> GetToolListFromTool(string toolId)
    {
        return ResourceManager.ToolRegistry.GetResourceReference(toolId).GetSlot() switch
        {
            Tool.SlotEnum.Primary => ToolsPrimary,
            Tool.SlotEnum.Secondary => ToolsSecondary,
            Tool.SlotEnum.Special => ToolsSpecial,
            Tool.SlotEnum.Melee => ToolsMelee,
            _ => ToolsPrimary,
        };
    }

    private List<LiveTool> GetToolListFromSlot(Tool.SlotEnum slot)
    {
        return slot switch
        {
            Tool.SlotEnum.Primary => ToolsPrimary,
            Tool.SlotEnum.Secondary => ToolsSecondary,
            Tool.SlotEnum.Special => ToolsSpecial,
            Tool.SlotEnum.Melee => ToolsMelee,
            _ => ToolsPrimary,
        };
    }
}