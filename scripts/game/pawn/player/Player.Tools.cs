namespace Shooter.Game;

public partial class Player : Pawn
{
    [Signal]
    public delegate void PlayerToolChangeEventHandler();

    public List<Tool> ToolsPrimary { get; private set; } = [];
    public List<Tool> ToolsSecondary { get; private set; } = [];
    public List<Tool> ToolsSpecial { get; private set; } = [];
    public List<Tool> ToolsMelee { get; private set; } = [];

    // reference from tool list
    public Tool SelectedTool = null;
    public Resource.Tool.SlotEnum SelectedSlot { get; private set; } = Resource.Tool.SlotEnum.Primary;
    public int SelectedToolIndex { get; private set; } = 0;

    public int ToolCount => ToolsPrimary.Count + ToolsSecondary.Count + ToolsSpecial.Count + ToolsMelee.Count;

    public int MaxWeight { get; set; } = 20;
    public int ToolWeight => GetToolsWeight();

    public bool SwappingWeapon { get; private set; }

    /// <summary> this should only be called using Rpc </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void ToolAddRpc(string toolConfigSerialized)
    {
        ToolAdd(toolConfigSerialized);
    }

    // should always be called through an Rpc
    public void ToolAdd(string toolConfigSerialized)
    {
        var toolConfig = ToolConfig.Deserialize(toolConfigSerialized);
        var resource = (Resource.Tool)toolConfig.LootState.GetLootRef();
        resource ??= ResourceManager.ToolRegistry.GetResourceRef(toolConfig.LootState.ResourceHashId);

        if (ToolWeight + resource.CarryWeight > MaxWeight) return;

        var tool = GD.Load<PackedScene>("res://scenes/tool/Tool.tscn").Instantiate<Tool>();
        tool.SetMultiplayerAuthority(Id);
        tool.PlayerId = Id;
        tool.ToolFullId = resource.FullId;
        tool.ToolConfig = toolConfig;

        var list = GetToolListFromTool(tool.ToolFullId);
        tool.Name = $"{resource.FullId}_" + list.Count(t => t.ToolFullId == resource.FullId);

        ToolsNode.AddChild(tool);
        tool.Owner = ToolsNode;
        list.Add(tool);

        EmitSignal(SignalName.PlayerToolChange);
    }

    /// <summary> this should only be called using Rpc </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void ToolRemoveRpc(string toolLootStateSerialized)
    {
        ToolRemove(toolLootStateSerialized);
    }

    // should always be called through an Rpc
    public async void ToolRemove(string toolLootStateSerialized)
    {
        var change = false;
        var lootState = Resource.Loot.LootState.Deserialize(toolLootStateSerialized);
        foreach (var tool in ToolsNode.GetChildren())
        {
            if (tool is not Tool) continue;

            var currentTool = (Tool)tool;
            if (currentTool.ToolConfig.LootState == lootState)
            {
                var list = GetToolListFromTool(currentTool.ToolFullId);
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
    public async void ToolEquipOwner()
    {
        if (!IsMultiplayerAuthority()) return;
        if (SwappingWeapon) return;

        if (IsMultiplayerAuthority()) Rpc(MethodName.ToolEquipRpc, (int)SelectedSlot, SelectedToolIndex);

        try
        {
            await ToolEquip();
        }
        catch (Exception exception)
        {
            GD.PushError($"Tool equip failed: {exception}");
            SwappingWeapon = false;
        }
    }

    /// <summary> Never call manually. ToolEquipOwner calls this </summary>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false)]
    private async void ToolEquipRpc(int slot, int index)
    {
        SelectedSlot = (Resource.Tool.SlotEnum)slot;
        SelectedToolIndex = index;

        try
        {
            await ToolEquip();
        }
        catch (Exception exception)
        {
            GD.PushError($"Tool equip failed: {exception}");
            SwappingWeapon = false;
        }
    }

    private async Task ToolEquip()
    {
        SwappingWeapon = true;
        if (SelectedTool is not null) await SelectedTool.Unequip();

        List<Tool> list = GetToolListFromSlot(SelectedSlot);
        if (list.Count == 0)
        {
            SelectedSlot = Resource.Tool.SlotEnum.Melee;
            list = GetToolListFromSlot(SelectedSlot);
            SelectedToolIndex = 0;
            SelectedTool = list[SelectedToolIndex];
            await SelectedTool.Equip();
            SwappingWeapon = false;
            return;
        }
        if (SelectedTool is not null && SelectedTool == list[SelectedToolIndex])
        {
            SelectedSlot = Resource.Tool.SlotEnum.Melee;
            list = GetToolListFromSlot(SelectedSlot);
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
    public async void ToolsSyncRpc(Godot.Collections.Array<string> toolConfigs)
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

        foreach (var tool in toolConfigs)
        {
            var config = ToolConfig.Deserialize(tool);
            ToolRemove(config.LootState.Serialize());
        }
        foreach (var tool in toolConfigs)
        {
            ToolAdd(tool);
        }

        await Task.Delay(100);
        RpcId(Id, MethodName.ToolsSyncCallbackRpc);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false)]
    public void ToolsSyncCallbackRpc()
    {
        GD.Print($"ToolsSyncCallbackRpc ({Self.Id})");

        foreach (var tool in GetAllTools())
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
            SelectedSlot = (Resource.Tool.SlotEnum)(((int)SelectedSlot + 4 - 1) % 4);
            SelectedToolIndex = GetToolListFromSlot(SelectedSlot).Count - 1;
        }

        if (SelectedToolIndex + delta >= GetToolListFromSlot(SelectedSlot).Count)
        {
            SelectedSlot = (Resource.Tool.SlotEnum)(((int)SelectedSlot + 1) % 4);
            SelectedToolIndex = 0;
        }

        ToolEquipOwner();
    }

    public void SelectToolBySlot(Resource.Tool.SlotEnum slot)
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

    /// <summary>Gets all tools. arg 0 returns FullId. arg 1 returns serialized ToolConfig</summary>
    public Godot.Collections.Array<string> GetAllTools(int returnType = 0)
    {
        Godot.Collections.Array<string> allTools = [];
        void AddTools(List<Tool> tools)
        {
            if (returnType == 1)
                foreach (var tool in tools) allTools.Add(tool.ToolConfig.Serialize());
            else
                foreach (var tool in tools) allTools.Add(tool.ToolFullId);
        }

        AddTools(ToolsPrimary);
        AddTools(ToolsSecondary);
        AddTools(ToolsSpecial);
        AddTools(ToolsMelee);
        return allTools;
    }

    public bool HasTool(Resource.Loot.LootState lootState)
    {
        var tools = GetAllTools();

        var a = tools.FirstOrDefault(c => c.ToolConfig.LootState == lootState, null);

        if (a is not null) return true;
        else return false;
    }

    public List<Tool> GetAllTools()
    {
        List<Tool> allTools = [];
        void AddTools(List<Tool> tools)
        {
            foreach (var tool in tools) allTools.Add(tool);
        }
        AddTools(ToolsPrimary);
        AddTools(ToolsSecondary);
        AddTools(ToolsSpecial);
        AddTools(ToolsMelee);
        return allTools;
    }

    private int GetToolsWeight()
    {
        var weight = 0;
        foreach (var tool in GetAllTools())
        {
            weight += tool.ToolResource.CarryWeight;
        }

        return weight;
    }

    private List<Tool> GetToolListFromTool(string toolId)
    {
        return ResourceManager.ToolRegistry.GetResourceRef(toolId).GetSlot() switch
        {
            Resource.Tool.SlotEnum.Primary => ToolsPrimary,
            Resource.Tool.SlotEnum.Secondary => ToolsSecondary,
            Resource.Tool.SlotEnum.Special => ToolsSpecial,
            Resource.Tool.SlotEnum.Melee => ToolsMelee,
            _ => ToolsPrimary,
        };
    }

    private List<Tool> GetToolListFromSlot(Resource.Tool.SlotEnum slot)
    {
        return slot switch
        {
            Resource.Tool.SlotEnum.Primary => ToolsPrimary,
            Resource.Tool.SlotEnum.Secondary => ToolsSecondary,
            Resource.Tool.SlotEnum.Special => ToolsSpecial,
            Resource.Tool.SlotEnum.Melee => ToolsMelee,
            _ => ToolsPrimary,
        };
    }
}