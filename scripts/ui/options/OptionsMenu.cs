using System.Collections.Immutable;
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

    private string selectedTab = "";

    private List<string> categories = [];

    public override void _Ready()
    {
        ReturnButton.Pressed += QueueFree;
        DiscardReturnButton.Pressed += QueueFree;

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
        selectedTab = categories.First();

        foreach (var category in categories)
        {
            var tabButton = new Button() { Text = category };
            tabButton.Pressed += () => { selectedTab = category; BuildList(); };
            Tabs.AddChild(tabButton);
        }
    }

    private void BuildList()
    {
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

            var entry = new PanelContainer();
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
                    Value = (float)prop.GetValue(OptionsManager.CurrentOptions),
                    Step = floatAtt.Step,
                    MinValue = floatAtt.Min,
                    MaxValue = floatAtt.Max,
                    CustomMinimumSize = new Vector2(200, 0)  // Static width
                };
                hbox.AddChild(slider);

                var valueLabel = new Label() { Text = slider.Value.ToString("F2") };
                slider.ValueChanged += (value) => valueLabel.Text = value.ToString("F2");
                hbox.AddChild(valueLabel);
            }

            type = typeof(OptionsManager.OptionStringAttribute);
            if (prop.GetCustomAttribute(type) is OptionsManager.OptionStringAttribute enumAtt)
            {
                label.Text = prop.Name;
                var optionButton = new OptionButton()
                {
                    CustomMinimumSize = new Vector2(200, 0)
                };
                foreach (var opt in enumAtt.Values) optionButton.AddItem(opt);
                var value = enumAtt.Values.First(c => c == (string)prop.GetValue(OptionsManager.CurrentOptions));
                var index = Array.IndexOf(enumAtt.Values, value);
                optionButton.Selected = index;
                hbox.AddChild(optionButton);
            }

            type = typeof(OptionsManager.OptionBoolAttribute);
            if (prop.GetCustomAttribute(type) is OptionsManager.OptionBoolAttribute boolAtt)
            {
                label.Text = prop.Name;
                var checkbox = new CheckButton()
                {
                    ButtonPressed = (bool)prop.GetValue(OptionsManager.CurrentOptions),
                    CustomMinimumSize = new Vector2(100, 0)
                };
                hbox.AddChild(checkbox);
            }

            OptionList.AddChild(entry);
        }
    }
}