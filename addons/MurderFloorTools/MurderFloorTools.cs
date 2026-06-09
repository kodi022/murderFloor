#if TOOLS

[Tool]
public partial class MurderFloorTools : EditorPlugin
{
	private MobSpawnAreaGizmo mobSpawnAreaGizmo;

	public override void _EnterTree()
	{
		// Initialization of the plugin goes here.
		var texture = GD.Load<Texture2D>("res://images/missing.png");

		var mobSpawnAreaScript = GD.Load<CSharpScript>("res://scripts/game/mob/MobSpawnArea.cs");
		AddCustomType("MobSpawnArea", "Node3D", mobSpawnAreaScript, texture);

		mobSpawnAreaGizmo = (MobSpawnAreaGizmo)(GodotObject)GD.Load<CSharpScript>("res://addons/MurderFloorTools/MobSpawnAreaGizmo.cs").New();
		AddNode3DGizmoPlugin(mobSpawnAreaGizmo);
	}

	public override void _ExitTree()
	{
		// Clean-up of the plugin goes here.
		RemoveCustomType("MobSpawnArea");
		RemoveNode3DGizmoPlugin(mobSpawnAreaGizmo);
	}
}
#endif
