namespace MurderFloor;

[GlobalClass]
public partial class Attachment : MFResource
{
    [Export]
    public PackedScene MeshScene { get; private set; }
}