namespace MurderFloor;

public static class OptionsManager
{
    public static Options CurrentOptions { get; private set; } = new Options();

    public struct Options
    {
        // Control
        public float Sensitivity = 0.8f;
        public bool SensitivityFieldOfViewScaling = true;

        // Graphics
        public float FieldOfView = 90;
        public float ViewmodelFieldOfViewScale = 0.8f;

        // Gameplay
        public float AimCrosshairOpacity = 0.25f;
        public bool ScalingCrosshair = true;

        public Options()
        {
        }
    }
}

