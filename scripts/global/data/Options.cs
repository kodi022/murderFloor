namespace MurderFloor;

public static class OptionsManager
{
    public static Options CurrentOptions { get; private set; } = new Options();

    public struct Options
    {
        // Control
        [OptionFloat("Control", 0.01f, 10f, 0.001f)]
        public float Sensitivity { get; set; } = 1f;
        [OptionBool("Control")]
        public bool SensitivityFieldOfViewScaling { get; set; } = true;

        [OptionFloat("Graphics", 60f, 100f, 1f)]
        public float FieldOfView { get; set; } = 90;
        [OptionFloat("Graphics", 0.6f, 1.2f, 0.01f)]
        public float ViewmodelFieldOfViewScale { get; set; } = 0.8f;
        [OptionString("Graphics", ["High", "Medium", "Low", "Off"])]
        public string SDFGI { get; set; } = "Medium";
        [OptionString("Graphics", ["MSAA8x", "MSAA4x", "SMAA", "FXAA", "Off"])]
        public string AntiAliasing { get; set; } = "FXAA";
        // [OptionFloat("Graphics", 24f, 300f, 1f)]
        // public float FramerateCap { get; set; } = 144f;

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

