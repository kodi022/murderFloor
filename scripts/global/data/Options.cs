namespace MurderFloor;

public static class OptionsManager
{
    public static string OptionsPath { get; private set; } = "user://options.json";
    public static Options CurrentOptions { get; private set; } = new Options();

    public static void Save(Options options)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(options);
        using var file = FileAccess.Open(OptionsPath, FileAccess.ModeFlags.Write);
        file.StoreString(json);
    }

    public static Options Load()
    {
        using var file = FileAccess.Open(OptionsPath, FileAccess.ModeFlags.Read);
        if (file is null) return new Options();
        var options = System.Text.Json.JsonSerializer.Deserialize<Options>(file.GetAsText());
        return options;
    }

    // not all are manually applied to somewhere
    public static void Apply(Options options)
    {
        CurrentOptions = options;

        // ! needs called from something

        // FramerateCap
        // Vsync
        // SDFGI
        // AntiAliasing
    }

    public class Options
    {
        // Control
        [OptionFloat("Control", 0.01f, 4f, 0.001f)]
        public float Sensitivity { get; set; } = 1f;
        [OptionBool("Control")]
        public bool SensitivityFieldOfViewScaling { get; set; } = true;

        [OptionFloat("Display", 24f, 300f, 1f)]
        public float FramerateLimit { get; set; } = DisplayServer.ScreenGetRefreshRate();
        [OptionBool("Display")]
        public bool VSync { get; set; } = true;
        // [OptionFloat("Display", 24f, 300f, 1f)]
        // public Vector2I Resolution { get; set; } = 144f;

        [OptionFloat("Graphics", 60f, 100f, 1f)]
        public float FieldOfView { get; set; } = 90;
        [OptionFloat("Graphics", 0.6f, 1.2f, 0.01f)]
        public float ViewmodelFieldOfViewScale { get; set; } = 0.8f;
        [OptionString("Graphics", ["High", "Medium", "Low", "Off"])]
        public string SDFGI { get; set; } = "Medium";
        [OptionString("Graphics", ["MSAA8x", "MSAA4x", "SMAA", "FXAA", "Off"])]
        public string AntiAliasing { get; set; } = "FXAA";

        [OptionFloat("Gameplay", 0f, 1f, 0.05f)]
        public float CrosshairOpacity { get; set; } = 1f;
        [OptionFloat("Gameplay", 0f, 1f, 0.05f)]
        public float AimCrosshairOpacity { get; set; } = 0.25f;
        [OptionBool("Gameplay")]
        public bool ScalingCrosshair { get; set; } = true;

        public Options()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class OptionAttribute : Attribute
    {
        public string Category { get; set; }

        public OptionAttribute(string category)
        {
            Category = category;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class OptionFloatAttribute(string category, float min, float max, float step) : OptionAttribute(category)
    {
        public float Min { get; set; } = min;
        public float Max { get; set; } = max;
        public float Step { get; set; } = step;

    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class OptionBoolAttribute(string category) : OptionAttribute(category)
    {

    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class OptionStringAttribute : OptionAttribute
    {
        public string[] Values { get; }

        public OptionStringAttribute(string category, string[] values) : base(category)
        {
            if (values is null || values.Length == 0)
                throw new ArgumentException("OptionStringAttribute needs values.", nameof(values));

            Category = category;
            Values = values;
        }
    }
}

