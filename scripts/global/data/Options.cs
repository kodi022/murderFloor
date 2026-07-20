namespace MurderFloor;

public static class OptionsManager
{
    public static string OptionsPath { get; private set; } = "user://options.json";
    public static Options CurrentOptions { get; private set; } = new Options();

    private static Control framerateDisplay;

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

    // some are applied elsewhere
    public static void Apply(Options options)
    {
        CurrentOptions = new Options(options);

        if (options.UseFramerateLimit)
            Engine.MaxFps = (int)options.FramerateLimit;
        else
            Engine.MaxFps = 0;

        DisplayServer.WindowSetVsyncMode(
            options.VSync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled
        );

        if (Engine.GetMainLoop() is SceneTree tree)
        {
            var viewport = tree.Root.GetWindow();

            viewport.Scaling3DMode = options.Scaling switch
            {
                "FSR2.2" => Viewport.Scaling3DModeEnum.Fsr2,
                "FSR1.0" => Viewport.Scaling3DModeEnum.Fsr,
                _ => Viewport.Scaling3DModeEnum.Bilinear
            };

            viewport.Scaling3DScale = options.ScalingRenderScale / 100f;
            viewport.FsrSharpness = 2f - options.ScalingSharpness;

            viewport.Msaa3D = options.AntiAliasing switch
            {
                "MSAA8x" => Viewport.Msaa.Msaa8X,
                "MSAA4x" => Viewport.Msaa.Msaa4X,
                "MSAA2x" => Viewport.Msaa.Msaa2X,
                _ => Viewport.Msaa.Disabled,
            };

            viewport.ScreenSpaceAA = options.AntiAliasing switch
            {
                "SMAA" => Viewport.ScreenSpaceAAEnum.Smaa,
                "FXAA" => Viewport.ScreenSpaceAAEnum.Fxaa,
                _ => Viewport.ScreenSpaceAAEnum.Disabled
            };

            viewport.UseTaa = options.TAA;

            // ! not working
            var sdfgiEnabled = options.SDFGI != "Off";
            foreach (Node child in tree.Root.GetChildren())
            {
                if (child is WorldEnvironment worldEnvironment && worldEnvironment.Environment != null)
                {
                    worldEnvironment.Environment.SdfgiEnabled = sdfgiEnabled;
                    switch (options.SDFGI)
                    {
                        case "Ultra":
                            break;
                        default:
                            break;
                    }
                }
            }

            // ! pretty sure this isn't working
            viewport.AnisotropicFilteringLevel = options.AntisotropicFiltering switch
            {
                "16x" => Viewport.AnisotropicFiltering.Anisotropy16X,
                "8x" => Viewport.AnisotropicFiltering.Anisotropy8X,
                "4x" => Viewport.AnisotropicFiltering.Anisotropy4X,
                "2x" => Viewport.AnisotropicFiltering.Anisotropy2X,
                _ => Viewport.AnisotropicFiltering.Disabled,
            };

            if (options.DisplayFramerate)
            {
                if (framerateDisplay is not null)
                {
                    framerateDisplay?.Free();
                    framerateDisplay = null;
                }
                framerateDisplay = GD.Load<PackedScene>("res://scenes/ui/FramerateDisplay.tscn").Instantiate<Control>();
                tree.Root.CallDeferred("add_child", framerateDisplay);
            }
            else
            {
                framerateDisplay?.Free();
                framerateDisplay = null;
            }
        }
    }

    public class Options
    {
        // Control
        [OptionFloat("Control", 0.01f, 4f, 0.001f, "")]
        public float Sensitivity { get; set; } = 1f;
        [OptionBool("Control", "")]
        public bool SensitivityFieldOfViewScaling { get; set; } = true;

        [OptionString("Display", ["Windowed", "Maximized", "Fullscreen", "Exclusive Fullscreen"], "")]
        public string WindowMode { get; set; } = "Windowed";
        // [OptionFloat("Display", 24f, 300f, 1f)]
        // public Vector2I Resolution { get; set; } = 144f;
        [OptionBool("Display", "")]
        public bool UseFramerateLimit { get; set; } = false;
        [OptionFloat("Display", 24f, 300f, 1f, "")]
        public float FramerateLimit { get; set; } = DisplayServer.ScreenGetRefreshRate();
        [OptionBool("Display", "")]
        public bool VSync { get; set; } = true;

        [OptionFloat("Graphics", 60f, 100f, 1f, "")]
        public float FieldOfView { get; set; } = 90;
        [OptionFloat("Graphics", 0.6f, 1.2f, 0.01f, "")]
        public float ViewmodelFieldOfViewScale { get; set; } = 0.8f;
        [OptionString("Graphics", ["FSR2.2", "FSR1.0", "None"], "")]
        public string Scaling { get; set; } = "None";
        [OptionFloat("Graphics", 25f, 200f, 5f, "Resource intensive and FSR not supported when above 100.")]
        public float ScalingRenderScale { get; set; } = 100f;
        [OptionFloat("Graphics", 0f, 2f, 0.1f, "Used only with FSR.")]
        public float ScalingSharpness { get; set; } = 1.8f;
        [OptionString("Graphics", ["MSAA8x", "MSAA4x", "MSAA2x", "SMAA", "FXAA", "Off"], "Not Recommended with FSR2.2 enabled. MSAA can be resource intensive.")]
        public string AntiAliasing { get; set; } = "FXAA";
        [OptionBool("Graphics", "Ignored when FSR2.2 is enabled.")]
        public bool TAA { get; set; } = false;
        [OptionString("Graphics", ["Ultra", "High", "Medium", "Low", "Off"], "")]
        public string SDFGI { get; set; } = "Medium";
        [OptionBool("Graphics", "")]
        public bool SDFGIHalfResolution { get; set; } = false;
        [OptionString("Graphics", ["16x", "8x", "4x", "2x", "Off"], "")]
        public string AntisotropicFiltering { get; set; } = "8x";

        [OptionFloat("Gameplay", 0f, 1f, 0.05f, "")]
        public float CrosshairOpacity { get; set; } = 1f;
        [OptionFloat("Gameplay", 0f, 1f, 0.05f, "")]
        public float AimCrosshairOpacity { get; set; } = 0.25f;
        [OptionBool("Gameplay", "")]
        public bool ScalingCrosshair { get; set; } = true;
        [OptionBool("Gameplay", "")]
        public bool DisplayFramerate { get; set; } = false;

        public Options()
        {
        }

        public Options(Options other)
        {
            Sensitivity = other.Sensitivity;
            SensitivityFieldOfViewScaling = other.SensitivityFieldOfViewScaling;

            UseFramerateLimit = other.UseFramerateLimit;
            FramerateLimit = other.FramerateLimit;
            VSync = other.VSync;

            FieldOfView = other.FieldOfView;
            ViewmodelFieldOfViewScale = other.ViewmodelFieldOfViewScale;
            Scaling = other.Scaling;
            ScalingRenderScale = other.ScalingRenderScale;
            ScalingSharpness = other.ScalingSharpness;
            AntiAliasing = other.AntiAliasing;
            TAA = other.TAA;
            SDFGI = other.SDFGI;
            SDFGIHalfResolution = other.SDFGIHalfResolution;
            AntisotropicFiltering = other.AntisotropicFiltering;

            CrosshairOpacity = other.CrosshairOpacity;
            AimCrosshairOpacity = other.AimCrosshairOpacity;
            ScalingCrosshair = other.ScalingCrosshair;
            DisplayFramerate = other.DisplayFramerate;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class OptionAttribute(string category, string tip) : Attribute
    {
        public string Category { get; set; } = category;
        public string Tip { get; set; } = tip;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class OptionFloatAttribute : OptionAttribute
    {
        public float Min { get; set; } = 0f;
        public float Max { get; set; } = 1f;
        public float Step { get; set; } = 0.1f;

        public OptionFloatAttribute(string category, float min, float max, float step, string tip) : base(category, tip)
        {
            Min = min;
            Max = max;
            Step = step;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class OptionBoolAttribute : OptionAttribute
    {
        public OptionBoolAttribute(string category, string tip) : base(category, tip)
        {

        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class OptionStringAttribute : OptionAttribute
    {
        public string[] Values { get; }

        public OptionStringAttribute(string category, string[] values, string tip) : base(category, tip)
        {
            if (values is null || values.Length == 0)
                throw new ArgumentException("OptionStringAttribute needs values.", nameof(values));

            Category = category;
            Values = values;
        }
    }
}

