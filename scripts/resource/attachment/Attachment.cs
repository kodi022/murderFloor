namespace Shooter.Resource;

[GlobalClass]
public partial class Attachment : ItemResource
{
    public enum AttachmentTypeEnum
    {
        Optic,
        Muzzle,
        MuzzleSuppressor,
        Barrel,
        Gadget,
    }

    [Export]
    public AttachmentTypeEnum AttachmentType { get; private set; }

    public override async Task<ImageTexture> GenerateThumbnailImage(int resX, int resY)
    {
        if (MeshScene is null) return Utils.Defaults.MissingTextureImage;

        string GetDictKey(int resX, int resY) { return $"{HashId}-{resX}-{resY}"; }
        if (generatedThumbnails.TryGetValue(GetDictKey(resX, resY), out ImageTexture val))
            return val;

        var sceneViewport = new SubViewport
        {
            Size = new Vector2I(resX, resY),
            OwnWorld3D = true,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Once,
            Msaa3D = Viewport.Msaa.Msaa8X,
            TransparentBg = true,
        };
        ((SceneTree)Engine.GetMainLoop()).Root.AddChild(sceneViewport);

        var weaponScene = MeshScene.Instantiate<Node3D>();
        var camera = new Camera3D();
        var bounds = GetBounds(weaponScene);
        var modelWidth = bounds.End.Abs().X + bounds.Position.Abs().X;
        camera.SetOrthogonal(modelWidth * 0.65f, 0.1f, 20f);
        camera.LookAtFromPosition(new Vector3(3f, 0, 3f), Vector3.Zero);

        sceneViewport.AddChild(weaponScene);
        var modelCenter = (bounds.End + bounds.Position) / 2;
        weaponScene.GlobalPosition = -modelCenter;

        sceneViewport.AddChild(camera);
        ApplyThumbnailMaterialToParts(weaponScene);

        await sceneViewport.ToSignal(RenderingServer.Singleton, RenderingServerInstance.SignalName.FramePostDraw);
        var image = sceneViewport.GetViewport().GetTexture().GetImage();
        sceneViewport.QueueFree();

        var imgTex = ImageTexture.CreateFromImage(image);
        if (!generatedThumbnails.ContainsKey(GetDictKey(resX, resY)))
            generatedThumbnails.Add(GetDictKey(resX, resY), imgTex);
        return imgTex;
    }

    // // * IF CHANGING, remember to change in ui/tool/ToolInfo
    // // sign determines buff direction
    // public enum AllStatsEnum
    // {
    //     FalloffRanges = 2,
    //     InitialDegreeSpread = -2,
    //     MaxDegreeSpread = -3,
    //     SpreadRecoveryRate = 8,
    //     SpreadIncreasePerShot = -4,
    //     AimShiftRangeVertical = -8,
    //     AimShiftRangeHorizontal = -9,
    // }

    // /// <summary>Gets stat or -1.0f if not found</summary>
    // public override Variant GetStat(string statName, float mult)
    // {
    //     Variant val = (AllStatsEnum)Enum.Parse(typeof(AllStatsEnum), statName) switch
    //     {
    //         AllStatsEnum.Damages => Damages * mult,
    //         AllStatsEnum.FalloffRanges => FalloffRanges * mult,
    //         AllStatsEnum.RPM => RPM * mult,
    //         AllStatsEnum.PelletCount => PelletCount * mult,
    //         AllStatsEnum.ReloadDelayMs => ReloadDelayMs * mult,
    //         AllStatsEnum.ManualFireDelayMs => ManualFireDelayMs * mult,
    //         AllStatsEnum.MagSize => MagSize * mult,
    //         AllStatsEnum.MagsReserve => MagsReserve * mult,
    //         AllStatsEnum.InitialDegreeSpread => InitialDegreeSpread * mult,
    //         AllStatsEnum.MaxDegreeSpread => MaxDegreeSpread * mult,
    //         AllStatsEnum.SpreadRecoveryRate => SpreadRecoveryRate * mult,
    //         AllStatsEnum.SpreadIncreasePerShot => SpreadIncreasePerShot * mult,
    //         AllStatsEnum.AimShiftRangeVertical => AimShiftRangeVertical * mult,
    //         AllStatsEnum.AimShiftRangeHorizontal => AimShiftRangeHorizontal * mult,
    //         _ => -1f,
    //     };

    //     if (val.VariantType == Variant.Type.Float && (float)val == -1f) GD.PushWarning($"{this.GetType().Name} does not have stat {statName}. Returning -1");
    //     return val;
    // }

    // /// <summary>Gets names of all stats or empty array if none</summary>
    // public override string[] GetStatsEnum()
    // {
    //     return Enum.GetNames(typeof(AllStatsEnum));
    // }

    // /// <summary>Gets sign of stat based on its enum</summary>
    // public override int GetStatSign(string statName)
    // {
    //     return Math.Sign((int)Enum.Parse(typeof(AllStatsEnum), statName));
    // }
}