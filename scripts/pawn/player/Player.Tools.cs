namespace MurderFloor;

public partial class Player : Pawn
{
    [Signal]
    public delegate void PlayerToolChangeEventHandler();

    public List<LiveTool> ToolsPrimary { get; private set; } = [];
    public List<LiveTool> ToolsSecondary { get; private set; } = [];
    public List<LiveTool> ToolsSpecial { get; private set; } = [];
    public List<LiveTool> ToolsMelee { get; private set; } = [];

    public Tool.SlotEnum SelectedSlot { get; private set; } = Tool.SlotEnum.Primary;
    public int SelectedToolIndex { get; private set; } = 0;

    public int ToolCount => ToolsPrimary.Count + ToolsSecondary.Count + ToolsSpecial.Count + ToolsMelee.Count;

    public int MaxWeight { get; set; } = 20;
    public int ToolWeight => GetToolsWeight();

    // reference from tool list
    public LiveTool SelectedTool = null;

    public bool SwappingWeapon { get; private set; }

    /// <summary> this should only be called using Rpc </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void ToolAddRpc(string lootState)
    {
        ToolAdd(lootState);
    }

    // should always be called through an Rpc
    public void ToolAdd(string lootState)
    {
        var lootStateStruct = Loot.LootState.Deserialize(lootState);
        var resource = ResourceManager.ToolRegistry.GetResourceRef(lootStateStruct.HashId);
        if (ToolWeight + resource.CarryWeight > MaxWeight) return;

        var liveTool = GD.Load<PackedScene>("res://scenes/tool/LiveTool.tscn").Instantiate<LiveTool>();
        liveTool.SetMultiplayerAuthority(Id);
        liveTool.PlayerId = Id;
        liveTool.ToolFullId = resource.FullId;
        liveTool.LootState = lootStateStruct;
        ToolsNode.AddChild(liveTool);
        liveTool.Owner = ToolsNode;
        var list = GetToolListFromTool(liveTool.ToolFullId);
        liveTool.Name = $"{resource.FullId}_" + list.Count(t => t.ToolFullId == resource.FullId);
        list.Add(liveTool);
        EmitSignal(SignalName.PlayerToolChange);
    }

    /// <summary> this should only be called using Rpc </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void ToolRemoveRpc(string lootState)
    {
        ToolRemove(lootState);
    }

    // should always be called through an Rpc
    public async void ToolRemove(string lootState)
    {
        var change = false;
        var lootStateStruct = Loot.LootState.Deserialize(lootState);
        foreach (var tool in ToolsNode.GetChildren())
        {
            if (tool is not LiveTool) continue;

            LiveTool liveTool = (LiveTool)tool;
            if (liveTool.LootState == lootStateStruct)
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

                if (tool == SelectedTool)
                {
                    SwappingWeapon = true;
                    await SelectedTool.Unequip();
                    SwappingWeapon = false;
                    SelectedTool = null;
                }

                tool.Free();
                change = true;
            }
        }

        if (change) EmitSignal(SignalName.PlayerToolChange);
    }

    /// <summary> Call for Owner only. Will call ToolEquipRpc if successful </summary>
    public void ToolEquipOwner()
    {
        if (!IsMultiplayerAuthority()) return;
        if (SwappingWeapon) return;

        if (IsMultiplayerAuthority()) Rpc("ToolEquipRpc", (int)SelectedSlot, SelectedToolIndex);
        ToolEquip();
    }

    /// <summary> Never call manually. ToolEquipOwner calls this </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
    private void ToolEquipRpc(int slot, int index)
    {
        SelectedSlot = (Tool.SlotEnum)slot;
        SelectedToolIndex = index;
        ToolEquip();
    }

    private async void ToolEquip()
    {
        SwappingWeapon = true;
        if (SelectedTool is not null) await SelectedTool.Unequip();

        List<LiveTool> list = GetToolListFromSlot(SelectedSlot);
        if (list.Count == 0)
        {
            SelectedSlot = Tool.SlotEnum.Melee;
            SelectedToolIndex = 0;
            SelectedTool = list[SelectedToolIndex];
            await SelectedTool.Equip();
            SwappingWeapon = false;
            return;
        }
        if (SelectedTool is not null && SelectedTool == list[SelectedToolIndex])
        {
            SelectedSlot = Tool.SlotEnum.Melee;
            SelectedToolIndex = 0;
            SelectedTool = list[SelectedToolIndex];
            await SelectedTool.Equip();
            SwappingWeapon = false;
            return;
        }

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

        ToolEquipOwner();
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
            ToolEquipOwner();
            return;
        }

        SelectedSlot = slot;
        SelectedToolIndex = 0;
        ToolEquipOwner();
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

    public bool HasTool(Loot.LootState lootState)
    {
        var tools = GetAllLiveTools();

        var a = tools.FirstOrDefault(c => c.LootState == lootState, null);

        if (a is not null) return true;
        else return false;
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

    private int GetToolsWeight()
    {
        var weight = 0;
        foreach (var tool in GetAllLiveTools())
        {
            weight += tool.ToolResource.CarryWeight;
        }

        return weight;
    }

    private List<LiveTool> GetToolListFromTool(string toolId)
    {
        return ResourceManager.ToolRegistry.GetResourceRef(toolId).GetSlot() switch
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