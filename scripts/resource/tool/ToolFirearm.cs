namespace MurderFloor;

[GlobalClass]
public partial class ToolFirearm : Tool
{
    public enum FireModeEnum
    {
        Auto,
        Semi,
        Manual
    }

    // order by size/power of weapon
    public enum FirearmTypeEnum
    {
        Pistol,
        Revolver,
        SMG,
        PDW,
        Carbine,
        AR,
        Shotgun,
        DMR,
        Special,
        Melee
    }

    public enum CasingSpawnEventEnum
    {
        Fire,
        Pump,
        Reload
    }

    [Export]
    public FirearmTypeEnum FirearmType { get; private set; }
    [Export]
    public float RPM { get; private set; } = 400f;
    [Export]
    public AudioStreamMP3 FireSound { get; private set; }
    [Export]
    public AudioStreamMP3 DryFireSound { get; private set; }

    [Export]
    public int PelletCount { get; private set; } = 1;
    [Export]
    public float HoldingSpeed { get; private set; } = 1f;

    [Export]
    public string IdleAnimationName { get; private set; } = "idle";

    [Export, ExportSubgroup("Reload")]
    public int ReloadDelayMs { get; private set; } = 800;
    [Export]
    public string ReloadAnimationName { get; private set; }
    [Export]
    public AudioStreamMP3 ReloadSound { get; private set; }
    [Export]
    public bool PartialReload { get; private set; } // ! implement later
    [Export, ExportSubgroup("Reload/PartialReload")]
    public Godot.Collections.Dictionary<int, string> PartialReloadAnimationNames { get; private set; }

    [Export, ExportSubgroup("Ammo")]
    public int MagSize { get; private set; } = 8;
    [Export]
    public int MagsReserve { get; private set; } = 6;
    [Export]
    public bool EndlessReserve { get; private set; } = false;

    [Export, ExportSubgroup("Spread")]
    public Vector2 InitialDegreeSpread { get; private set; } = new Vector2(1f, 1f);
    [Export]
    public Vector2 MaxDegreeSpread { get; private set; } = new Vector2(3f, 3f);
    [Export]
    public float SpreadRecoveryRate { get; private set; } = 2f;
    [Export]
    public Vector2 SpreadIncreasePerShot { get; private set; } = new Vector2(0.5f, 0.5f);
    [Export]
    public Vector2 SlowWalkSpreadMult { get; private set; } = new Vector2(1.2f, 1.2f);
    [Export]
    public Vector2 FastWalkSpreadMult { get; private set; } = new Vector2(1.6f, 1.6f);
    [Export]
    public Vector2 AimSpreadMult { get; private set; } = new Vector2(0.8f, 0.8f);

    [Export, ExportSubgroup("Kick")]
    public Vector2 CameraRotationKick { get; private set; } = new Vector2(0.02f, 0f);
    [Export]
    public Vector2 ViewmodelPositionKick { get; private set; } = new Vector2(0f, 0.075f);
    [Export]
    public Vector2 ViewmodelRotationKick { get; private set; } = new Vector2(0.05f, 0f);
    [Export]
    public Vector2 AimShiftRangeVertical { get; private set; } = new Vector2(0.001f, 0.0015f);
    [Export]
    public Vector2 AimShiftRangeHorizontal { get; private set; } = new Vector2(-0.0003f, 0.0003f);
    [Export]
    public float ScreenShakeAmount { get; private set; } = 0.02f;

    [Export, ExportSubgroup("FireMode")]
    public FireModeEnum FireMode { get; private set; }
    [Export, ExportSubgroup("FireMode/FireModeManual")]
    public string ManualFireAnimationName { get; private set; }
    [Export]
    public int ManualFireDelayMs { get; private set; } = 400;
    [Export]
    public AudioStreamMP3 ManualFireSound { get; private set; }

    // ! implement
    [Export, ExportSubgroup("ShellCasings")]
    public bool SpawnCasings { get; private set; } = true;
    [Export]
    public PackedScene CasingMeshScene { get; private set; }
    [Export]
    public Vector3 CasingEjectionForceMin { get; private set; }
    [Export]
    public Vector3 CasingEjectionForceMax { get; private set; }
    // CasingEjectionPosition is from a Node3D on gun

    protected RandomNumberGenerator Rng { get; private set; } = new();

    public virtual void FireBullet(FireInfo fi) { }

    public override SlotEnum GetSlot()
    {
        if ((int)FirearmType <= (int)FirearmTypeEnum.Revolver)
        {
            return SlotEnum.Secondary;
        }

        if (FirearmType == FirearmTypeEnum.Special) return SlotEnum.Special;
        if (FirearmType == FirearmTypeEnum.Melee) return SlotEnum.Melee;

        return SlotEnum.Primary;
    }

    public virtual void EjectCasing()
    {

    }

    public override async Task<ImageTexture> GenerateThumbnailImage(int resX, int resY)
    {
        if (MeshScene is null) return Global.MissingTextureImage;

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
        weaponScene.RotationDegrees = new Vector3(0, MeshSceneImportYaw, 0);
        camera.SetOrthogonal(MathF.Max(modelWidth * 0.55f, 0.5f), 0.1f, 20f);
        camera.LookAtFromPosition(new Vector3(0, 0, 3f), Vector3.Zero);

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

    public override BuiltToolData BuildToolScene(BuildToolData buildToolData)
    {
        var builtToolData = new BuiltToolData() { ToolHashId = HashId };

        var toolResource = ResourceManager.ToolRegistry.GetResourceRef(HashId);
        builtToolData.Node3D = toolResource.MeshScene.Instantiate<Node3D>();
        builtToolData.Node3D.RotationDegrees = new Vector3(0, MeshSceneImportYaw, 0);

        Node3D FindNode(string name)
        {
            var thing = (Node3D)builtToolData.Node3D.FindChildren(name).FirstOrDefault(new Node3D());
            if (!thing.IsInsideTree())
                GD.PrintErr($"Warning: {toolResource.FullId} has no Node3D named \"{name}\"");

            return thing;
        }

        var muzzleNode = FindNode("Point-Muzzle");
        var sightNode = FindNode("Point-Sight");
        var sightAttachmentNode = FindNode("Point-SightAttachment");
        var ejectionNode = FindNode("Point-Ejection");
        var foregripNode = FindNode("Point-Foregrip");
        var gadgetNode = FindNode("Point-Gadget");

        builtToolData.SightPosition = sightNode.Position.Rotated(Vector3.Up, -builtToolData.Node3D.Rotation.Y);

        foreach (var hashId in buildToolData.AttachmentHashIds)
        {
            var attachment = ResourceManager.AttachmentRegistry.GetResourceRef(hashId);
            switch (attachment.AttachmentType)
            {
                case Attachment.AttachmentTypeEnum.Optic:
                    if (sightAttachmentNode.IsInsideTree())
                    {
                        var opticModelScene = attachment.MeshScene.Instantiate<Node3D>();
                        sightAttachmentNode.AddChild(opticModelScene);
                        opticModelScene.RotationDegrees = new Vector3(0, attachment.MeshSceneImportYaw, 0);
                        var attachSightNode = (Node3D)opticModelScene.FindChildren("Sight").FirstOrDefault(new Node3D());
                        builtToolData.SightPosition = sightAttachmentNode.Position.Rotated(Vector3.Up, -builtToolData.Node3D.Rotation.Y);
                        builtToolData.SightPosition += attachSightNode.Position.Rotated(Vector3.Up, -opticModelScene.Rotation.Y);
                    }
                    break;
            }
        }

        return builtToolData;
    }
}