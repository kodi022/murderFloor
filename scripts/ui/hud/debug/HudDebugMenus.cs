using MurderFloor.Loot;

namespace MurderFloor;

public partial class HudDebugMenus : Control
{
    [Export]
    private TabContainer tabContainer;

    public override void _Ready()
    {
        // * page 0
        var tree = tabContainer.GetChild<Tree>(0);
        var root = tree.CreateItem();
        tree.HideRoot = true;
        tree.SetColumnTitle(0, "FullId");
        tree.SetColumnTitle(1, "HashId");
        tree.SetColumnTitle(2, "IsLoot");
        tree.SetColumnTitle(3, "");

        tree.ButtonClicked += (item, column, id, mouseButtonIndex) =>
        {
            switch (item.GetParent().GetText(0))
            {
                case "Tools":
                    if (column == 0)
                        Player.Self.Rpc("ToolAddRpc", $"0,{Compression.IntToAB64(item.GetText(1).ToInt())},0.1.0,0,0,0,0,.00,");
                    else if (column == 1)
                        GD.Print(item.GetText(1).ToInt());
                    break;
            }
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
                child.AddButton(1, Global.MissingTexture);
                child.SetText(2, item.Value.IsLoot.ToString());
            }
        }

        AddChildrenToRoot("Tools", ResourceManager.ToolRegistry.GetAllResource(), true);
        AddChildrenToRoot("Attachments", ResourceManager.AttachmentRegistry.GetAllResource());
        AddChildrenToRoot("Mobs", ResourceManager.MobRegistry.GetAllResource(), true);
        AddChildrenToRoot("Maps", ResourceManager.MapRegistry.GetAllResource());

        // * page 1
        var compPanel = tabContainer.GetChild<Panel>(1);
        var lineEdit1 = compPanel.GetChild<LineEdit>(0);
        lineEdit1.GetChild<Button>(0).Pressed += () =>
        {
            var comp = Compression.IntToAB64(int.Parse(lineEdit1.Text));
            lineEdit1.GetChild(1).GetChild<Label>(0).Text = comp;
            var uncomp = Compression.AB64ToInt(comp);
            lineEdit1.GetChild(2).GetChild<Label>(0).Text = uncomp.ToString();
        };
        var lineEdit2 = compPanel.GetChild<LineEdit>(1);
        lineEdit2.GetChild<Button>(0).Pressed += () =>
        {
            var comp = Compression.ULToAB64(ulong.Parse(lineEdit2.Text));
            lineEdit2.GetChild(1).GetChild<Label>(0).Text = comp;
            var uncomp = Compression.AB64ToUL(comp);
            lineEdit2.GetChild(2).GetChild<Label>(0).Text = uncomp.ToString();
        };

        // * page 2
        var lsPanel = tabContainer.GetChild<Panel>(2);
        var lsCreatorPanel = lsPanel.GetChild<Panel>(0);
        lsCreatorPanel.GetChild<Button>(0).Pressed += () =>
        {
            ((LineEdit)lsCreatorPanel.GetChildren().Last()).Text = LootState.Serialize(new LootState(0, 0, 0, 0, false, false, 0));
        };
        lsCreatorPanel.GetChild<Button>(1).Pressed += () =>
        {
            var seed = ulong.Parse(lsCreatorPanel.GetChild<LineEdit>(2).Text);
            var level = lsCreatorPanel.GetChild<LineEdit>(3).Text.ToInt();
            var difficulty = lsCreatorPanel.GetChild<LineEdit>(4).Text.ToInt();
            ((LineEdit)lsCreatorPanel.GetChildren().Last()).Text = LootState.Serialize(new LootState(seed, level, (Game.DifficultyEnum)difficulty, 0, false, false, 0));
        };

        // * page 3
        var genPanel = tabContainer.GetChild<Panel>(3);
        genPanel.GetChild<Button>(2).Pressed += async () =>
        {
            var amount = Math.Min(1000000, genPanel.GetChild<LineEdit>(0).Text.ToInt());
            var level = genPanel.GetChild<LineEdit>(1).Text.ToInt();

            var outputLine = genPanel.GetChild<TextEdit>(3);
            var generationTask = Debug.DebugGenerateLoot(amount, level);

            outputLine.Text = "running";
            var tick = 0;
            while (!generationTask.IsCompleted)
            {
                var dots = new string('.', (tick % 3) + 1);
                outputLine.Text = $"running{dots}";
                tick++;

                await Task.Delay(500);
            }

            var strs = await generationTask;
            outputLine.Text = string.Join("\n", strs);
        };
    }
}
