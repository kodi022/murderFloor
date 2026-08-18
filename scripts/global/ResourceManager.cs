namespace MurderFloor;

public static class ResourceManager
{
	public static ResourceRegistry<Tool> ToolRegistry { get; private set; } = new();
	public static ResourceRegistry<Attachment> AttachmentRegistry { get; private set; } = new();
	public static ResourceRegistry<Mob> MobRegistry { get; private set; } = new();
	public static ResourceRegistry<Map> MapRegistry { get; private set; } = new();

	public static LootResourceRegistry LootRegistry { get; private set; } = new();
	// special loot can be retrieved by file

	public static string ModsPath { get; private set; } = "user://mods/";

	public static void Ready()
	{
		ToolRegistry.RegisterFolder("res://resources/tool/");
		AttachmentRegistry.RegisterFolder("res://resources/tool/attachment/");
		MobRegistry.RegisterFolder("res://resources/mob/");
		MapRegistry.RegisterFolder("res://resources/map/");

		// collect mod data
		//foreach (var file in Direct)
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

						if (resource is ToolMelee) GD.Print(resource.HashId);

						if (resource.UseInGame && resource.IsLoot) LootRegistry.Add(resource);
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
		public T GetResourceRef(int hashId)
		{
			if (registry.TryGetValue(hashId, out T val))
				return val;

			GD.PushWarning($"{typeof(T).Name}Registry.GetResourceRef: hashId not found");
			return null;
		}

		/// <summary>
		/// Gets resource by reference or null if fail
		/// </summary>
		public T GetResourceRef(string fullId)
		{
			if (string.IsNullOrEmpty(fullId))
			{
				GD.PushWarning($"{typeof(T).Name}Registry.GetResourceRef: invalid fullId");
				return null;
			}

			return GetResourceRef(Global.StableHash(fullId));
		}

		public Dictionary<int, T> GetAllResource()
		{
			return registry;
		}
	}

	public class LootResourceRegistry
	{
		private readonly List<MFResource> registry = [];

		public int Count => registry.Count;

		public void Add(MFResource value)
		{
			registry.Add(value);
		}

		// public Dictionary<string, MFResource> GetAllResourceUnderVersion(Global.Version version)
		// {
		// 	var newDict = new Dictionary<string, MFResource>();
		// 	foreach (var res in registry)
		// 	{
		// 		var resVer = Global.Version.FromString(res.Key);
		// 		if (!version.IsGreaterThan(resVer)) newDict.Add(res.Key, res.Value);
		// 	}
		// 	return newDict;
		// }

		public MFResource GetResourceAtIndex(int index)
		{
			if (index < 0 || index >= Count)
			{
				GD.PushWarning("LootResourceRegistry.GetResourceAtIndex: invalid index");
				return null;
			}

			return registry.ElementAt(index);
		}

		/// <summary>
		/// Gets resource by reference or null if fail
		/// </summary>
		public MFResource GetResourceRef(int hashId)
		{
			var res = registry.FirstOrDefault(c => c.HashId == hashId, null);
			if (res is null) GD.PushWarning("LootResourceRegistry.GetResourceRef: hashId not found");
			return res;
		}

		/// <summary>
		/// Gets resource by reference or null if fail
		/// </summary>
		public MFResource GetResourceRef(string fullId)
		{
			if (string.IsNullOrEmpty(fullId))
			{
				GD.PushWarning("LootResourceRegistry.GetResourceRef: invalid fullId");
				return null;
			}

			return GetResourceRef(Global.StableHash(fullId));
		}

		public List<MFResource> GetAllResource()
		{
			return registry;
		}
	}
}
