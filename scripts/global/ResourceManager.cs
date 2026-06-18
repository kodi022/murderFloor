namespace MurderFloor;

public static class ResourceManager
{
	public static ResourceRegistry<Tool> ToolRegistry { get; private set; } = new();
	public static ResourceRegistry<Tool> AttachmentRegistry { get; private set; } = new();
	public static ResourceRegistry<Mob> MobRegistry { get; private set; } = new();

	public static void Ready()
	{
		ToolRegistry.RegisterFolder("res://resources/tool/");
		// AttachmentRegistry.RegisterFolder("res://resources/attachment/");
		MobRegistry.RegisterFolder("res://resources/mob/");

		// string dirToMods = OS.GetUserDataDir();
	}

	public class ResourceRegistry<T> where T : MFResource
	{
		private readonly Dictionary<int, T> registry = [];

		public void RegisterFolder(string path)
		{
			Dictionary<int, T> folderRegisters = [];

			void ListDirectory(string path)
			{
				foreach (var file in ResourceLoader.ListDirectory(path))
				{
					if (file == "") continue;
					if (file.EndsWith('/'))
					{
						ListDirectory(path + file);
						continue;
					}

					if (!file.EndsWith(".tres")) continue;

					var res = ResourceLoader.Load(path + file);
					if (res is T resource)
					{
						if (resource.PackageId == "") GD.PushWarning($"{typeof(T).Name}Registry: Resource missing PackageId {path}");
						if (resource.ResourceId == "") GD.PushWarning($"{typeof(T).Name}Registry: Resource missing ResourceId {path}");

						resource.BuildIds();
						folderRegisters.Add(resource.HashId, resource);
					}
				}
			}
			ListDirectory(path);

			foreach (var resource in folderRegisters)
			{
				if (!registry.TryAdd(resource.Key, resource.Value))
				{
					GD.PushWarning($"{typeof(T).Name}Registry: Resource of Key already exists {path}");
				}
			}

			GD.Print($"{typeof(T).Name}Registry: {registry.Count} {typeof(T).Name}s {path}");
		}

		/// <summary>
		/// Gets resource by reference or null if fail
		/// </summary>
		public T GetResourceReference(string fullId)
		{
			if (string.IsNullOrEmpty(fullId)) return null;

			if (registry.TryGetValue(Global.StableHash(fullId), out T val))
				return val;

			return null;
		}

		public Dictionary<int, T> GetAllResource()
		{
			return registry;
		}
	}
}
