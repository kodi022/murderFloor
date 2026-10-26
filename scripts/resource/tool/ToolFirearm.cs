namespace Shooter.Resource;

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
        Shotgun,
        AR,
        Sniper,
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
    public AudioStreamMP3 FireSoundSuppressed { get; private set; }
    [Export]
    public AudioStreamMP3 DryFireSound { get; private set; }

    [Export]
    public int PelletCount { get; private set; } = 1;

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
    public int MagsReserve { get; private set; } = 5;
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

        return SlotEnum.Primary;
    }

    public virtual void EjectCasing()
    {

    }

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

    public override BuiltToolData BuildToolScene(ToolConfig toolConfig)
    {
        var builtToolData = new BuiltToolData() { ToolHashId = HashId };

        var toolResource = ResourceManager.ToolRegistry.GetResourceRef(HashId);
        builtToolData.Tool = toolResource.MeshScene.Instantiate<Node3D>();

        Node3D FindNode(string name, out Vector3 offset)
        {
            var armature = builtToolData.Tool.FindChild("Armature*");
            var thing = (Node3D)armature.FindChild(name);
            if (thing is null)
            {
                GD.PushError($"Warning: {toolResource.FullId}'s Armature has no child named \"{name}\"");
                offset = Vector3.Zero;
                return new Node3D();
            }

            offset = thing.Position;
            Node3D node = (Node3D)thing.GetParent();
            while (true)
            {
                if (!IsInstanceValid(node) || node == armature) break;
                offset += node.Position;
                node = (Node3D)node.GetParent();
            }

            return thing;
        }

        // ! DO THIS SHIT
        var muzzleNode = FindNode("Point-Muzzle", out Vector3 muzzlePos);
        var sightNode = FindNode("Point-Sight", out Vector3 sightPos);
        var sightAttachmentNode = FindNode("Point-SightAttachment", out Vector3 sightAttachPos);
        var ejectionNode = FindNode("Point-Ejection", out _);
        var foregripNode = FindNode("Point-Foregrip", out _);
        var gadgetNode = FindNode("Point-Gadget", out _);

        builtToolData.SightPositionOffset = new Vector3(-sightPos.Z, -sightPos.Y, 0);
        builtToolData.MuzzlePosition = new Vector3(-muzzlePos.X, muzzlePos.Y, -muzzlePos.Z);

        foreach (var attLootState in toolConfig.AttachmentLootStates)
        {
            var attachment = (Attachment)attLootState.GetLootRef();
            switch (attachment.AttachmentType)
            {
                case Attachment.AttachmentTypeEnum.Optic:
                    var opticModelScene = attachment.MeshScene.Instantiate<Node3D>();
                    sightAttachmentNode.AddChild(opticModelScene);
                    var attachSightNode = (Node3D)opticModelScene.FindChildren("Point-Sight").FirstOrDefault(new Node3D());
                    builtToolData.SightPositionOffset = new Vector3(-sightAttachPos.Z, -sightAttachPos.Y, 0);
                    builtToolData.SightPositionOffset += new Vector3(-attachSightNode.Position.Z, -attachSightNode.Position.Y, 0);
                    builtToolData.OpticZoom = ((AttachmentOptic)attachment).OpticZoom;
                    break;
                case Attachment.AttachmentTypeEnum.Muzzle:
                case Attachment.AttachmentTypeEnum.MuzzleSuppressor:
                    var muzzleModelScene = attachment.MeshScene.Instantiate<Node3D>();
                    muzzleNode.AddChild(muzzleModelScene);
                    var attachMuzzleNode = (Node3D)muzzleModelScene.FindChildren("Point-Muzzle").FirstOrDefault(new Node3D());
                    builtToolData.MuzzlePosition += attachMuzzleNode.Position;

                    if (attachment.AttachmentType is Attachment.AttachmentTypeEnum.MuzzleSuppressor)
                        builtToolData.Suppressed = true;

                    break;
            }
        }

        return builtToolData;
    }
}