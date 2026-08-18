namespace MurderFloor;

public partial class HudDebugItemList : Control
{
    [Export]
    public Tree Tree { get; private set; }

    public override void _Ready()
    {
        var root = Tree.CreateItem();
        Tree.HideRoot = true;
        Tree.SetColumnTitle(0, "FullId");
        Tree.SetColumnTitle(1, "HashId");
        Tree.SetColumnTitle(2, "IsLoot");
        Tree.SetColumnTitle(3, "");

        Tree.ButtonClicked += (item, column, id, mouseButtonIndex) =>
        {
            switch (item.GetParent().GetText(0))
            {
                case "Tools":
                    Player.Self.Rpc("ToolAddRpc", $"0,{item.GetText(1)},0.1.0,0,0,0,0,0");
                    break;
            }
            GD.Print($"{item} {item.GetParent().GetText(0)} {column} {id} {mouseButtonIndex}");
        };

        void AddChildrenToRoot<T>(string itemName, Dictionary<int, T> resources, bool hasFunction = false) where T : MFResource
        {
            var newItem = root.CreateChild();
            newItem.SetText(0, itemName);
            newItem.Collapsed = true;
            foreach (var item in resources)
            {
                var child = newItem.CreateChild();
                child.SetText(0, item.Value.FullId);
                if (hasFunction) child.AddButton(0, Global.MissingTexture);
                child.SetText(1, item.Value.HashId.ToString());
                child.SetText(2, item.Value.IsLoot.ToString());
            }
        }

        AddChildrenToRoot("Tools", ResourceManager.ToolRegistry.GetAllResource(), true);
        AddChildrenToRoot("Attachments", ResourceManager.AttachmentRegistry.GetAllResource());
        AddChildrenToRoot("Mobs", ResourceManager.MobRegistry.GetAllResource(), true);
        AddChildrenToRoot("Maps", ResourceManager.MapRegistry.GetAllResource());
    }
}
