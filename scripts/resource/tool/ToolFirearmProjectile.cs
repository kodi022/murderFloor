namespace MurderFloor;

[GlobalClass]
public partial class ToolFirearmProjectile : ToolFirearm
{
    [Export, ExportGroup("fire")]
    public PackedScene PackedScene { get; set; }
}