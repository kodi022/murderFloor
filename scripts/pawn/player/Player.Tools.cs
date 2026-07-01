namespace MurderFloor;

public partial class Player : Pawn
{
    public List<LiveTool> ToolsPrimary { get; private set; } = [];
    public List<LiveTool> ToolsSecondary { get; private set; } = [];
    public List<LiveTool> ToolsSpecial { get; private set; } = [];
    public List<LiveTool> ToolsMelee { get; private set; } = [];

    public Tool.SlotEnum SelectedSlot { get; private set; } = Tool.SlotEnum.Primary;
    public int SelectedToolIndex { get; private set; } = 0;

    public int ToolCount => ToolsPrimary.Count + ToolsSecondary.Count + ToolsSpecial.Count + ToolsMelee.Count;

    // reference from tool list
    public LiveTool SelectedTool = null;

    public bool SwappingWeapon { get; private set; }

    /// <summary> this should only be called using Rpc </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void ToolAddRpc(string toolId)
    {
        ToolAdd(toolId);
    }

    // should always be called through an Rpc
    public void ToolAdd(string toolId)
    {
        var liveTool = (LiveTool)GD.Load<PackedScene>("res://scenes/tool/LiveTool.tscn").Instantiate();
        liveTool.SetMultiplayerAuthority(Id);
        liveTool.PlayerId = Id;
        liveTool.ToolFullId = toolId;
        ToolsNode.AddChild(liveTool);
        liveTool.Owner = ToolsNode;
        var list = GetToolListFromTool(liveTool.ToolFullId);
        liveTool.Name = $"{toolId}_" + list.Count(t => t.ToolFullId == toolId);
        list.Add(liveTool);
    }

    /// <summary> this should only be called using Rpc </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void ToolRemoveRpc(string toolId)
    {
        ToolRemove(toolId);
    }

    // should always be called through an Rpc
    public void ToolRemove(string toolId)
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
                tool.Free();
            }
        }
    }

    /// <summary> this should NOT be called using Rpc, will do so internally </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
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

        if (IsMultiplayerAuthority()) Rpc("ToolEquip", slot, index);

        SwappingWeapon = true;
        if (SelectedTool is not null) await SelectedTool.Unequip();
        SelectedTool = list[SelectedToolIndex];

        await SelectedTool.Equip();
        SwappingWeapon = false;
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
    public async void ToolsSyncRpc(Godot.Collections.Array<string> tools)
    {
        var ready = 0;
        while (ready < AllPlayers.Count)
        {
            await Task.Delay(100);
            ready = 0;
            foreach (var player in AllPlayers)
            {
                if (player.IsNodeReady()) ready++;
            }
        }

        GD.Print($"ToolsSyncRpc ({Id} sync for {Self.Id})");

        foreach (var tool in tools)
        {
            ToolRemove(tool);
        }
        foreach (var tool in tools)
        {
            ToolAdd(tool);
        }

        await Task.Delay(100);
        RpcId(Id, "ToolsSyncCallbackRpc");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void ToolsSyncCallbackRpc()
    {
        GD.Print($"ToolsSyncCallbackRpc ({Self.Id})");

        foreach (var tool in GetAllLiveTools())
        {
            var sync = (MultiplayerSynchronizer)tool.GetChild(0);
            foreach (var plr in AllPlayers)
            {
                sync.SetVisibilityFor(plr.Id, true);
            }
            sync.UpdateVisibility();
        }
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

    public Godot.Collections.Array<string> GetAllTools()
    {
        Godot.Collections.Array<string> tools = [];
        void AddTools(List<LiveTool> liveTools)
        {
            foreach (var tool in liveTools) tools.Add(tool.ToolFullId);
        }
        AddTools(ToolsPrimary);
        AddTools(ToolsSecondary);
        AddTools(ToolsSpecial);
        AddTools(ToolsMelee);
        return tools;
    }

    public List<LiveTool> GetAllLiveTools()
    {
        List<LiveTool> tools = [];
        void AddTools(List<LiveTool> liveTools)
        {
            foreach (var tool in liveTools) tools.Add(tool);
        }
        AddTools(ToolsPrimary);
        AddTools(ToolsSecondary);
        AddTools(ToolsSpecial);
        AddTools(ToolsMelee);
        return tools;
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