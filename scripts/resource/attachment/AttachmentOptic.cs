namespace MurderFloor;

[GlobalClass]
public partial class AttachmentOptic : Attachment
{
    [Export]
    public new AttachmentTypeEnum AttachmentType { get; private set; } = AttachmentTypeEnum.Optic;

    [Export]
    public Texture2D ReticleTexture { get; private set; }
    [Export] // default is Color(3.294, 0.0, 0.0)
    public Color ReticleColor { get; private set; }
}