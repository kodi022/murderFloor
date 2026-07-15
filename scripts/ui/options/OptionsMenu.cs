using System.Reflection;

namespace MurderFloor;

public partial class OptionsMenu : Control
{
    [Export]
    public HBoxContainer Tabs { get; private set; }
    [Export]
    public VBoxContainer OptionList { get; private set; }
    [Export]
    public Button ReturnButton { get; private set; }
    [Export]
    public Button DiscardReturnButton { get; private set; }
    [Export]
    public Button DefaultsButton { get; private set; }

    private string selectedTab = "";

    private List<string> categories = [];

    private OptionsManager.Options currentOptions;

    public override void _Ready()
    {
        currentOptions = OptionsManager.Load();

        ReturnButton.Pressed += () =>
        {
            OptionsManager.Save(currentOptions);
            QueueFree();
        };
        DiscardReturnButton.Pressed += QueueFree;
        DefaultsButton.Pressed += () =>
        {
            currentOptions = new OptionsManager.Options();
            selectedTab = "";
            BuildList(categories.First());
        };

        var optionType = typeof(OptionsManager.Options);
        foreach (var prop in optionType.GetProperties())
        {
            var type = typeof(OptionsManager.OptionAttribute);
            if (prop.GetCustomAttribute(type) is OptionsManager.OptionAttribute att)
            {
                if (!categories.Contains(att.Category)) categories.Add(att.Category);
            }
        }

        categories.Sort();
        BuildList(categories.First());

        foreach (var category in categories)
        {
            var tabButton = new Button() { Text = category };
            tabButton.Pressed += () => { BuildList(category); };
            Tabs.AddChild(tabButton);
        }
    }

    private void BuildList(string category)
    {
        if (selectedTab == category) return;
        selectedTab = category;

        foreach (var child in OptionList.GetChildren())
        {
            child.Free();
        }

        var optionType = typeof(OptionsManager.Options);
        foreach (var prop in optionType.GetProperties())
        {
            var type = typeof(OptionsManager.OptionAttribute);
            if (prop.GetCustomAttribute(type) is OptionsManager.OptionAttribute att)
            {
                if (att.Category != selectedTab) continue;
            }

            var entry = new PanelContainer() { Size = new Vector2(0, 100) };
            var margin = new MarginContainer();
            entry.AddChild(margin);
            var hbox = new HBoxContainer();
            margin.AddChild(hbox);
            var label = new Label();
            hbox.AddChild(label);

            var spacer2 = new Control() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            hbox.AddChild(spacer2);

            type = typeof(OptionsManager.OptionFloatAttribute);
            if (prop.GetCustomAttribute(type) is OptionsManager.OptionFloatAttribute floatAtt)
            {
                label.Text = prop.Name;
                var slider = new HSlider()
                {
                    Step = floatAtt.Step,
                    MinValue = floatAtt.Min,
                    MaxValue = floatAtt.Max,
                    CustomMinimumSize = new Vector2(400, 0)
                };
                hbox.AddChild(slider);

                slider.Value = (float)prop.GetValue(currentOptions);
                // using a function for Text does not work, memory bug
                var valueLabel = new Label()
                {
                    Text = floatAtt.Step >= 1 ? slider.Value.ToString("F0") : slider.Value.ToString("F2"),
                    CustomMinimumSize = new Vector2(60, 0)
                };
                hbox.AddChild(valueLabel);

                slider.ValueChanged += (value) =>
                {
                    valueLabel.Text = floatAtt.Step >= 1 ? value.ToString("F0") : value.ToString("F2");
                    prop.SetValue(currentOptions, (float)value);
                };
            }

            type = typeof(OptionsManager.OptionStringAttribute);
            if (prop.GetCustomAttribute(type) is OptionsManager.OptionStringAttribute enumAtt)
            {
                label.Text = prop.Name;
                var optionButton = new OptionButton() { CustomMinimumSize = new Vector2(260, 0) };
                hbox.AddChild(optionButton);

                var spacer = new Control() { CustomMinimumSize = new Vector2(60, 0) };
                hbox.AddChild(spacer);

                margin.AddThemeConstantOverride("margin_left", 10);
                margin.AddThemeConstantOverride("margin_right", 10);
                margin.AddThemeConstantOverride("margin_top", 6);
                margin.AddThemeConstantOverride("margin_bottom", 6);

                foreach (var opt in enumAtt.Values) optionButton.AddItem(opt);
                var value = enumAtt.Values.First(c => c == (string)prop.GetValue(currentOptions));
                var index = Array.IndexOf(enumAtt.Values, value);
                optionButton.Selected = index;
                optionButton.ItemSelected += (idx) => { prop.SetValue(currentOptions, enumAtt.Values[idx]); };
            }

            type = typeof(OptionsManager.OptionBoolAttribute);
            if (prop.GetCustomAttribute(type) is OptionsManager.OptionBoolAttribute boolAtt)
            {
                label.Text = prop.Name;
                var checkbox = new CheckButton() { CustomMinimumSize = new Vector2(100, 0) };
                hbox.AddChild(checkbox);

                var spacer = new Control() { CustomMinimumSize = new Vector2(60, 0) };
                hbox.AddChild(spacer);

                margin.AddThemeConstantOverride("margin_left", 10);
                margin.AddThemeConstantOverride("margin_right", 10);
                margin.AddThemeConstantOverride("margin_top", 9);
                margin.AddThemeConstantOverride("margin_bottom", 9);

                checkbox.ButtonPressed = (bool)prop.GetValue(currentOptions);
                checkbox.Toggled += (pressed) => { prop.SetValue(currentOptions, pressed); };
            }

            OptionList.AddChild(entry);
        }
    }
}