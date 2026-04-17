using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using SoftReferenceableAssets;
using UnityEngine;

public class ZoneSystem : MonoBehaviour
{
	private class ZoneData
	{
		public GameObject m_root;

		public float m_ttl;
	}

	private class ClearArea
	{
		public Vector3 m_center;

		public float m_radius;

		public ClearArea(Vector3 p, float r)
		{
			m_center = p;
			m_radius = r;
		}
	}

	[Serializable]
	public class ZoneVegetation
	{
		public string m_name = "veg";

		public GameObject m_prefab;

		public bool m_enable = true;

		public float m_min;

		public float m_max = 10f;

		public bool m_forcePlacement;

		public float m_scaleMin = 1f;

		public float m_scaleMax = 1f;

		public float m_randTilt;

		public float m_chanceToUseGroundTilt;

		[BitMask(typeof(Heightmap.Biome))]
		public Heightmap.Biome m_biome;

		[BitMask(typeof(Heightmap.BiomeArea))]
		public Heightmap.BiomeArea m_biomeArea = Heightmap.BiomeArea.Everything;

		public bool m_blockCheck = true;

		public bool m_snapToStaticSolid;

		public float m_minAltitude = -1000f;

		public float m_maxAltitude = 1000f;

		public float m_minVegetation;

		public float m_maxVegetation;

		[Header("Samples points around and choses the highest vegetation")]
		[Tooltip("Samples points around the placement point and choses the point with most total vegetation value")]
		public bool m_surroundCheckVegetation;

		[Tooltip("How far to check surroundings")]
		public float m_surroundCheckDistance = 20f;

		[Tooltip("How many layers of circles to sample. (If distance is large you should have more layers)")]
		public int m_surroundCheckLayers = 2;

		[Tooltip("How much better than the average an accepted point will be. (Procentually between average and best)")]
		public float m_surroundBetterThanAverage;

		[Space(10f)]
		public float m_minOceanDepth;

		public float m_maxOceanDepth;

		public float m_minTilt;

		public float m_maxTilt = 90f;

		public float m_terrainDeltaRadius;

		public float m_maxTerrainDelta = 2f;

		public float m_minTerrainDelta;

		public bool m_snapToWater;

		public float m_groundOffset;

		public int m_groupSizeMin = 1;

		public int m_groupSizeMax = 1;

		public float m_groupRadius;

		[Header("Distance from center")]
		public float m_minDistanceFromCenter;

		public float m_maxDistanceFromCenter;

		[Header("Forest fractal 0-1 inside forest")]
		public bool m_inForest;

		public float m_forestTresholdMin;

		public float m_forestTresholdMax = 1f;

		[HideInInspector]
		public bool m_foldout;

		public ZoneVegetation Clone()
		{
			return MemberwiseClone() as ZoneVegetation;
		}
	}

	[Serializable]
	public class ZoneLocation
	{
		public string m_name;

		public bool m_enable = true;

		[HideInInspector]
		public string m_prefabName;

		public SoftReference<GameObject> m_prefab;

		[BitMask(typeof(Heightmap.Biome))]
		public Heightmap.Biome m_biome;

		[BitMask(typeof(Heightmap.BiomeArea))]
		public Heightmap.BiomeArea m_biomeArea = Heightmap.BiomeArea.Everything;

		public int m_quantity;

		public bool m_prioritized;

		public bool m_centerFirst;

		public bool m_unique;

		public string m_group = "";

		public float m_minDistanceFromSimilar;

		public string m_groupMax = "";

		public float m_maxDistanceFromSimilar;

		public bool m_iconAlways;

		public bool m_iconPlaced;

		public bool m_randomRotation = true;

		public bool m_slopeRotation;

		public bool m_snapToWater;

		public float m_interiorRadius;

		public float m_exteriorRadius;

		public bool m_clearArea;

		public float m_minTerrainDelta;

		public float m_maxTerrainDelta = 2f;

		public float m_minimumVegetation;

		public float m_maximumVegetation = 1f;

		[Header("Samples points around and choses the highest vegetation")]
		[Tooltip("Samples points around the placement point and choses the point with most total vegetation value")]
		public bool m_surroundCheckVegetation;

		[Tooltip("How far to check surroundings")]
		public float m_surroundCheckDistance = 20f;

		[Tooltip("How many layers of circles to sample. (If distance is large you should have more layers)")]
		public int m_surroundCheckLayers = 2;

		[Tooltip("How much better than the average an accepted point will be. (Procentually between average and best)")]
		public float m_surroundBetterThanAverage;

		[Header("Forest fractal 0-1 inside forest")]
		public bool m_inForest;

		public float m_forestTresholdMin;

		public float m_forestTresholdMax = 1f;

		[Header("Distance from center")]
		public float m_minDistanceFromCenter;

		public float m_maxDistanceFromCenter;

		[Space(10f)]
		public float m_minDistance;

		public float m_maxDistance;

		public float m_minAltitude = -1000f;

		public float m_maxAltitude = 1000f;

		[HideInInspector]
		public bool m_foldout;

		public int Hash => m_prefab.Name.GetStableHashCode();

		public ZoneLocation Clone()
		{
			return MemberwiseClone() as ZoneLocation;
		}
	}

	public struct LocationInstance
	{
		public ZoneLocation m_location;

		public Vector3 m_position;

		public bool m_placed;
	}

	private class LocationPrefabLoadData
	{
		private SoftReference<GameObject> m_prefab;

		private SoftReference<GameObject>[] m_possibleRooms;

		private int m_roomsToLoad;

		private bool m_isFirstSpawn;

		public int m_iterationLifetime;

		public bool IsLoaded { get; private set; }

		public AssetID PrefabAssetID => m_prefab.m_assetID;

		public LocationPrefabLoadData(SoftReference<GameObject> prefab, bool isFirstSpawn)
		{
			m_prefab = prefab;
			m_isFirstSpawn = isFirstSpawn;
			m_roomsToLoad = 0;
			m_prefab.LoadAsync(OnPrefabLoaded);
		}

		public void Release()
		{
			if (!m_prefab.IsValid)
			{
				return;
			}
			m_prefab.Release();
			m_prefab.m_assetID = default(AssetID);
			if (m_possibleRooms != null)
			{
				for (int i = 0; i < m_possibleRooms.Length; i++)
				{
					m_possibleRooms[i].Release();
				}
				m_possibleRooms = null;
			}
		}

		private void OnPrefabLoaded(AssetID assetID, LoadResult result)
		{
			if (result != LoadResult.Succeeded || !m_prefab.IsValid)
			{
				return;
			}
			if (!m_isFirstSpawn)
			{
				IsLoaded = true;
				return;
			}
			DungeonGenerator[] enabledComponentsInChildren = Utils.GetEnabledComponentsInChildren<DungeonGenerator>(m_prefab.Asset);
			if (enabledComponentsInChildren.Length == 0)
			{
				IsLoaded = true;
				return;
			}
			if (enabledComponentsInChildren.Length > 1)
			{
				ZLog.LogWarning("Location " + m_prefab.Asset.name + " has more than one dungeon generator! The preloading code only works for one dungeon generator per location.");
			}
			m_possibleRooms = enabledComponentsInChildren[0].GetAvailableRoomPrefabs();
			m_roomsToLoad = m_possibleRooms.Length;
			for (int i = 0; i < m_possibleRooms.Length; i++)
			{
				m_possibleRooms[i].LoadAsync(OnRoomLoaded);
			}
		}

		private void OnRoomLoaded(AssetID assetID, LoadResult result)
		{
			if (result == LoadResult.Succeeded)
			{
				m_roomsToLoad--;
				if (m_possibleRooms != null && m_roomsToLoad <= 0)
				{
					IsLoaded = true;
				}
			}
		}
	}

	public enum SpawnMode
	{
		Full,
		Client,
		Ghost
	}

	private Dictionary<Vector3, string> tempIconList = new Dictionary<Vector3, string>();

	private List<float> s_tempVeg = new List<float>();

	private RaycastHit[] rayHits = new RaycastHit[200];

	private static readonly List<RaycastHit> s_rayHits = new List<RaycastHit>(64);

	private static readonly List<float> s_rayHitsHeight = new List<float>(64);

	private List<string> m_tempKeys = new List<string>();

	private static ZoneSystem m_instance;

	[HideInInspector]
	public List<Heightmap.Biome> m_biomeFolded = new List<Heightmap.Biome>();

	[HideInInspector]
	public List<Heightmap.Biome> m_vegetationFolded = new List<Heightmap.Biome>();

	[HideInInspector]
	public List<Heightmap.Biome> m_locationFolded = new List<Heightmap.Biome>();

	[NonSerialized]
	public bool m_drawLocations;

	[NonSerialized]
	public string m_drawLocationsFilter = "";

	[Tooltip("Zones to load around center sector")]
	public int m_activeArea = 1;

	public int m_activeDistantArea = 1;

	[Tooltip("Zone size, should match netscene sector size")]
	public float m_zoneSize = 64f;

	[Tooltip("Time before destroying inactive zone")]
	public float m_zoneTTL = 4f;

	[Tooltip("Time before spawning active zone")]
	public float m_zoneTTS = 4f;

	public GameObject m_zonePrefab;

	public GameObject m_zoneCtrlPrefab;

	public GameObject m_locationProxyPrefab;

	public float m_waterLevel = 30f;

	public const float c_WaterLevel = 30f;

	public const float c_ZoneSize = 64f;

	public const double c_ZoneSizeDouble = 64.0;

	public const float c_ZoneHalfSize = 32f;

	public const double c_ZoneHalfSizeDouble = 32.0;

	[Header("Versions")]
	public int m_pgwVersion = 53;

	public int m_locationVersion = 1;

	[Header("Generation data")]
	public List<string> m_locationScenes = new List<string>();

	public List<GameObject> m_locationLists = new List<GameObject>();

	public List<ZoneVegetation> m_vegetation = new List<ZoneVegetation>();

	public List<ZoneLocation> m_locations = new List<ZoneLocation>();

	private Dictionary<int, ZoneLocation> m_locationsByHash = new Dictionary<int, ZoneLocation>();

	private bool m_error;

	public bool m_didZoneTest;

	private int m_terrainRayMask;

	private int m_blockRayMask;

	private int m_solidRayMask;

	private int m_staticSolidRayMask;

	private float m_updateTimer;

	private float m_startTime;

	private float m_lastFixedTime;

	private Dictionary<Vector2i, ZoneData> m_zones = new Dictionary<Vector2i, ZoneData>();

	private HashSet<Vector2i> m_generatedZones = new HashSet<Vector2i>();

	private Dictionary<Vector2i, List<ZDO>> m_loadingObjectsInZones = new Dictionary<Vector2i, List<ZDO>>();

	private Coroutine m_generateLocationsCoroutine;

	private DateTime m_estimatedGenerateLocationsCompletionTime;

	private float m_timeSlicedGenerationTimeBudget = 0.01f;

	private bool m_locationsGenerated;

	private float m_generateLocationsProgress;

	[HideInInspector]
	public Dictionary<Vector2i, LocationInstance> m_locationInstances = new Dictionary<Vector2i, LocationInstance>();

	private Dictionary<Vector3, string> m_locationIcons = new Dictionary<Vector3, string>();

	private HashSet<string> m_globalKeys = new HashSet<string>();

	public HashSet<GlobalKeys> m_globalKeysEnums = new HashSet<GlobalKeys>();

	public Dictionary<string, string> m_globalKeysValues = new Dictionary<string, string>();

	private HashSet<Vector2i> m_tempGeneratedZonesSaveClone;

	private HashSet<string> m_tempGlobalKeysSaveClone;

	private List<LocationInstance> m_tempLocationsSaveClone;

	private bool m_tempLocationsGeneratedSaveClone;

	private List<ClearArea> m_tempClearAreas = new List<ClearArea>();

	private List<GameObject> m_tempSpawnedObjects = new List<GameObject>();

	private List<int> m_tempLocationPrefabsToRelease = new List<int>();

	private List<LocationPrefabLoadData> m_locationPrefabs = new List<LocationPrefabLoadData>();

	private Action m_generateLocationsCompleted;

	public static ZoneSystem instance => m_instance;

	public bool LocationsGenerated
	{
		get
		{
			return m_locationsGenerated;
		}
		private set
		{
			m_locationsGenerated = value;
			if (m_locationsGenerated)
			{
				m_generateLocationsCompleted?.Invoke();
				m_generateLocationsCompleted = null;
			}
		}
	}

	public float GenerateLocationsProgress => m_generateLocationsProgress;

	public event Action GenerateLocationsCompleted
	{
		add
		{
			if (m_locationsGenerated)
			{
				value?.Invoke();
			}
			else
			{
				m_generateLocationsCompleted = (Action)Delegate.Combine(m_generateLocationsCompleted, value);
			}
		}
		remove
		{
			m_generateLocationsCompleted = (Action)Delegate.Remove(m_generateLocationsCompleted, value);
		}
	}

	private ZoneSystem()
	{
	}

	private void Awake()
	{
		m_instance = this;
		m_terrainRayMask = LayerMask.GetMask("terrain");
		m_blockRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece");
		m_solidRayMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain");
		m_staticSolidRayMask = LayerMask.GetMask("static_solid", "terrain");
		foreach (GameObject locationList in m_locationLists)
		{
			UnityEngine.Object.Instantiate(locationList);
		}
		ZLog.Log("Zonesystem Awake " + Time.frameCount);
	}

	private void Start()
	{
		ZLog.Log("Zonesystem Start " + Time.frameCount);
		UpdateWorldRates();
		SetupLocations();
		ValidateVegetation();
		ZRoutedRpc zRoutedRpc = ZRoutedRpc.instance;
		zRoutedRpc.m_onNewPeer = (Action<long>)Delegate.Combine(zRoutedRpc.m_onNewPeer, new Action<long>(OnNewPeer));
		if (ZNet.instance.IsServer())
		{
			ZRoutedRpc.instance.Register<string>("SetGlobalKey", RPC_SetGlobalKey);
			ZRoutedRpc.instance.Register<string>("RemoveGlobalKey", RPC_RemoveGlobalKey);
		}
		else
		{
			ZRoutedRpc.instance.Register<List<string>>("GlobalKeys", RPC_GlobalKeys);
			ZRoutedRpc.instance.Register<ZPackage>("LocationIcons", RPC_LocationIcons);
		}
		m_startTime = (m_lastFixedTime = Time.fixedTime);
	}

	public void GenerateLocationsIfNeeded()
	{
		if (!LocationsGenerated)
		{
			GenerateLocations();
		}
	}

	private void SendGlobalKeys(long peer)
	{
		List<string> list = new List<string>(m_globalKeys);
		ZRoutedRpc.instance.InvokeRoutedRPC(peer, "GlobalKeys", list);
		Player.m_localPlayer?.UpdateEvents();
	}

	private void RPC_GlobalKeys(long sender, List<string> keys)
	{
		ZLog.Log("client got keys " + keys.Count);
		ClearGlobalKeys();
		foreach (string key in keys)
		{
			GlobalKeyAdd(key);
		}
	}

	private void GlobalKeyAdd(string keyStr, bool canSaveToServerOptionKeys = true)
	{
		string value;
		GlobalKeys gk;
		string keyValue = GetKeyValue(keyStr.ToLower(), out value, out gk);
		bool flag = canSaveToServerOptionKeys && ZNet.World != null && gk < GlobalKeys.NonServerOption;
		if (m_globalKeysValues.TryGetValue(keyValue, out var value2))
		{
			string item = (keyValue + " " + value2).TrimEnd();
			m_globalKeys.Remove(item);
			if (flag)
			{
				ZNet.World.m_startingGlobalKeys.Remove(item);
			}
		}
		string text = (keyValue + " " + value).TrimEnd();
		m_globalKeys.Add(text);
		m_globalKeysValues[keyValue] = value;
		if (gk != GlobalKeys.NonServerOption)
		{
			m_globalKeysEnums.Add(gk);
		}
		Game.instance.GetPlayerProfile().m_knownWorldKeys.IncrementOrSet(text);
		if (flag)
		{
			ZNet.World.m_startingGlobalKeys.Add(keyStr.ToLower());
		}
		UpdateWorldRates();
	}

	private bool GlobalKeyRemove(string keyStr, bool canSaveToServerOptionKeys = true)
	{
		string value;
		GlobalKeys gk;
		string keyValue = GetKeyValue(keyStr, out value, out gk);
		if (m_globalKeysValues.TryGetValue(keyValue, out var value2))
		{
			string item = (keyValue + " " + value2).TrimEnd();
			if (canSaveToServerOptionKeys && ZNet.World != null && gk < GlobalKeys.NonServerOption)
			{
				ZNet.World.m_startingGlobalKeys.Remove(item);
			}
			m_globalKeys.Remove(item);
			m_globalKeysValues.Remove(keyValue);
			if (gk != GlobalKeys.NonServerOption)
			{
				m_globalKeysEnums.Remove(gk);
			}
			UpdateWorldRates();
			return true;
		}
		return false;
	}

	public void UpdateWorldRates()
	{
		Game.UpdateWorldRates(m_globalKeys, m_globalKeysValues);
	}

	public void Reset()
	{
		ClearGlobalKeys();
		UpdateWorldRates();
	}

	private void ClearGlobalKeys()
	{
		m_globalKeys.Clear();
		m_globalKeysEnums.Clear();
		m_globalKeysValues.Clear();
	}

	public static string GetKeyValue(string key, out string value, out GlobalKeys gk)
	{
		int num = key.IndexOf(' ');
		value = "";
		string text;
		if (num > 0)
		{
			value = key.Substring(num + 1);
			text = key.Substring(0, num).ToLower();
		}
		else
		{
			text = key.ToLower();
		}
		if (!Enum.TryParse<GlobalKeys>(text, ignoreCase: true, out gk))
		{
			gk = GlobalKeys.NonServerOption;
		}
		return text;
	}

	private void SendLocationIcons(long peer)
	{
		ZPackage zPackage = new ZPackage();
		tempIconList.Clear();
		GetLocationIcons(tempIconList);
		zPackage.Write(tempIconList.Count);
		foreach (KeyValuePair<Vector3, string> tempIcon in tempIconList)
		{
			zPackage.Write(tempIcon.Key);
			zPackage.Write(tempIcon.Value);
		}
		ZRoutedRpc.instance.InvokeRoutedRPC(peer, "LocationIcons", zPackage);
	}

	private void RPC_LocationIcons(long sender, ZPackage pkg)
	{
		ZLog.Log("client got location icons");
		m_locationIcons.Clear();
		int num = pkg.ReadInt();
		for (int i = 0; i < num; i++)
		{
			Vector3 key = pkg.ReadVector3();
			string value = pkg.ReadString();
			m_locationIcons[key] = value;
		}
		ZLog.Log("Icons:" + num);
	}

	private void OnNewPeer(long peerID)
	{
		if (ZNet.instance.IsServer())
		{
			ZLog.Log("Server: New peer connected,sending global keys");
			SendGlobalKeys(peerID);
			SendLocationIcons(peerID);
		}
	}

	private void SetupLocations()
	{
		List<LocationList> allLocationLists = LocationList.GetAllLocationLists();
		allLocationLists.Sort((LocationList a, LocationList b) => a.m_sortOrder.CompareTo(b.m_sortOrder));
		foreach (LocationList item in allLocationLists)
		{
			m_locations.AddRange(item.m_locations);
			m_vegetation.AddRange(item.m_vegetation);
			foreach (EnvSetup environment in item.m_environments)
			{
				EnvMan.instance.AppendEnvironment(environment);
			}
			foreach (BiomeEnvSetup biomeEnvironment in item.m_biomeEnvironments)
			{
				EnvMan.instance.AppendBiomeSetup(biomeEnvironment);
			}
			ClutterSystem.instance.m_clutter.AddRange(item.m_clutter);
			ZLog.Log($"Added {item.m_locations.Count} locations, {item.m_vegetation.Count} vegetations, {item.m_environments.Count} environments, {item.m_biomeEnvironments.Count} biome env-setups, {item.m_clutter.Count} clutter  from " + item.gameObject.scene.name);
			RandEventSystem.instance.m_events.AddRange(item.m_events);
		}
		foreach (ZoneLocation location in m_locations)
		{
			if ((location.m_enable || location.m_prefab.IsValid) && Application.isPlaying)
			{
				location.m_prefabName = location.m_prefab.Name;
				int hash = location.Hash;
				if (!m_locationsByHash.ContainsKey(hash))
				{
					m_locationsByHash.Add(hash, location);
				}
			}
		}
		if (!Settings.AssetMemoryUsagePolicy.HasFlag(AssetMemoryUsagePolicy.KeepAsynchronousLoadedBit))
		{
			return;
		}
		ReferenceHolder referenceHolder = base.gameObject.AddComponent<ReferenceHolder>();
		foreach (ZoneLocation location2 in m_locations)
		{
			if (location2.m_enable)
			{
				location2.m_prefab.Load();
				referenceHolder.HoldReferenceTo(location2.m_prefab);
				location2.m_prefab.Release();
			}
		}
	}

	public static void PrepareNetViews(GameObject root, List<ZNetView> views)
	{
		views.Clear();
		ZNetView[] componentsInChildren = root.GetComponentsInChildren<ZNetView>(includeInactive: true);
		foreach (ZNetView zNetView in componentsInChildren)
		{
			if (Utils.IsEnabledInheirarcy(zNetView.gameObject, root))
			{
				views.Add(zNetView);
			}
		}
	}

	public static void PrepareRandomSpawns(GameObject root, List<RandomSpawn> randomSpawns)
	{
		randomSpawns.Clear();
		RandomSpawn[] componentsInChildren = root.GetComponentsInChildren<RandomSpawn>(includeInactive: true);
		foreach (RandomSpawn randomSpawn in componentsInChildren)
		{
			if (Utils.IsEnabledInheirarcy(randomSpawn.gameObject, root))
			{
				randomSpawns.Add(randomSpawn);
				randomSpawn.Prepare();
			}
		}
	}

	private void OnDestroy()
	{
		ForceReleaseLoadedPrefabs();
		m_instance = null;
	}

	private void ValidateVegetation()
	{
		foreach (ZoneVegetation item in m_vegetation)
		{
			if (item.m_enable && (bool)item.m_prefab && item.m_prefab.GetComponent<ZNetView>() == null)
			{
				ZLog.LogError("Vegetation " + item.m_prefab.name + " [ " + item.m_name + "] is missing ZNetView");
			}
		}
	}

	public void PrepareSave()
	{
		m_tempGeneratedZonesSaveClone = new HashSet<Vector2i>(m_generatedZones);
		m_tempGlobalKeysSaveClone = new HashSet<string>(m_globalKeys);
		m_tempLocationsSaveClone = new List<LocationInstance>(m_locationInstances.Values);
		m_tempLocationsGeneratedSaveClone = LocationsGenerated;
	}

	public void SaveASync(BinaryWriter writer)
	{
		writer.Write(m_tempGeneratedZonesSaveClone.Count);
		foreach (Vector2i item in m_tempGeneratedZonesSaveClone)
		{
			writer.Write(item.x);
			writer.Write(item.y);
		}
		writer.Write(0);
		writer.Write(m_locationVersion);
		m_tempGlobalKeysSaveClone.RemoveWhere(delegate(string x)
		{
			GetKeyValue(x, out var _, out var gk);
			return gk < GlobalKeys.NonServerOption;
		});
		writer.Write(m_tempGlobalKeysSaveClone.Count);
		foreach (string item2 in m_tempGlobalKeysSaveClone)
		{
			writer.Write(item2);
		}
		writer.Write(m_tempLocationsGeneratedSaveClone);
		writer.Write(m_tempLocationsSaveClone.Count);
		foreach (LocationInstance item3 in m_tempLocationsSaveClone)
		{
			writer.Write(item3.m_location.m_prefabName);
			writer.Write(item3.m_position.x);
			writer.Write(item3.m_position.y);
			writer.Write(item3.m_position.z);
			writer.Write(item3.m_placed);
		}
		m_tempGeneratedZonesSaveClone.Clear();
		m_tempGeneratedZonesSaveClone = null;
		m_tempGlobalKeysSaveClone.Clear();
		m_tempGlobalKeysSaveClone = null;
		m_tempLocationsSaveClone.Clear();
		m_tempLocationsSaveClone = null;
	}

	public void Load(BinaryReader reader, int version)
	{
		m_generatedZones.Clear();
		int num = reader.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			Vector2i item = new Vector2i
			{
				x = reader.ReadInt32(),
				y = reader.ReadInt32()
			};
			m_generatedZones.Add(item);
		}
		if (version < 13)
		{
			return;
		}
		reader.ReadInt32();
		int num2 = ((version >= 21) ? reader.ReadInt32() : 0);
		if (version >= 14)
		{
			ClearGlobalKeys();
			int num3 = reader.ReadInt32();
			for (int j = 0; j < num3; j++)
			{
				string keyStr = reader.ReadString();
				GlobalKeyAdd(keyStr);
			}
		}
		if (version < 18)
		{
			return;
		}
		if (version >= 20)
		{
			LocationsGenerated = reader.ReadBoolean();
		}
		m_locationInstances.Clear();
		int num4 = reader.ReadInt32();
		for (int k = 0; k < num4; k++)
		{
			string text = reader.ReadString();
			Vector3 zero = Vector3.zero;
			zero.x = reader.ReadSingle();
			zero.y = reader.ReadSingle();
			zero.z = reader.ReadSingle();
			bool generated = false;
			if (version >= 19)
			{
				generated = reader.ReadBoolean();
			}
			ZoneLocation location = GetLocation(text);
			if (location != null)
			{
				RegisterLocation(location, zero, generated);
			}
			else
			{
				ZLog.DevLog("Failed to find location " + text);
			}
		}
		ZLog.Log("Loaded " + num4 + " locations");
		if (num2 != m_locationVersion)
		{
			LocationsGenerated = false;
		}
	}

	private void Update()
	{
		m_lastFixedTime = Time.fixedTime;
		if (ZNet.GetConnectionStatus() != ZNet.ConnectionStatus.Connected)
		{
			return;
		}
		if (ZNet.instance.IsServer() && !LocationsGenerated)
		{
			if (TextViewer.IsShowingIntro())
			{
				m_timeSlicedGenerationTimeBudget = GetGenerationTimeBudgetForTargetFrameRate(out var _);
			}
			else
			{
				m_timeSlicedGenerationTimeBudget = 0.1f;
			}
			return;
		}
		if (Terminal.m_showTests)
		{
			Terminal.m_testList["Time"] = Time.fixedTime.ToString("0.00") + " / " + TimeSinceStart().ToString("0.00");
		}
		m_updateTimer += Time.deltaTime;
		if (!(m_updateTimer > 0.1f))
		{
			return;
		}
		m_updateTimer = 0f;
		bool flag = CreateLocalZones(ZNet.instance.GetReferencePosition());
		UpdateTTL(0.1f);
		if (ZNet.instance.IsServer() && !flag)
		{
			CreateGhostZones(ZNet.instance.GetReferencePosition());
			foreach (ZNetPeer peer in ZNet.instance.GetPeers())
			{
				CreateGhostZones(peer.GetRefPos());
			}
		}
		UpdatePrefabLifetimes();
	}

	private void UpdatePrefabLifetimes()
	{
		for (int i = 0; i < m_locationPrefabs.Count; i++)
		{
			m_locationPrefabs[i].m_iterationLifetime--;
			if (m_locationPrefabs[i].m_iterationLifetime <= 0)
			{
				m_tempLocationPrefabsToRelease.Add(i);
			}
		}
		for (int num = m_tempLocationPrefabsToRelease.Count - 1; num >= 0; num--)
		{
			int index = m_tempLocationPrefabsToRelease[num];
			m_locationPrefabs[index].Release();
			m_locationPrefabs.RemoveAt(index);
		}
		m_tempLocationPrefabsToRelease.Clear();
	}

	private void ForceReleaseLoadedPrefabs()
	{
		foreach (LocationPrefabLoadData locationPrefab in m_locationPrefabs)
		{
			locationPrefab.Release();
		}
		m_locationPrefabs.Clear();
	}

	private bool CreateGhostZones(Vector3 refPoint)
	{
		Vector2i zone = GetZone(refPoint);
		if (!IsZoneGenerated(zone) && SpawnZone(zone, SpawnMode.Ghost, out var _))
		{
			return true;
		}
		int num = m_activeArea + m_activeDistantArea;
		for (int i = zone.y - num; i <= zone.y + num; i++)
		{
			for (int j = zone.x - num; j <= zone.x + num; j++)
			{
				Vector2i zoneID = new Vector2i(j, i);
				if (!IsZoneGenerated(zoneID) && SpawnZone(zoneID, SpawnMode.Ghost, out var _))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool CreateLocalZones(Vector3 refPoint)
	{
		Vector2i zone = GetZone(refPoint);
		if (PokeLocalZone(zone))
		{
			return true;
		}
		for (int i = zone.y - m_activeArea; i <= zone.y + m_activeArea; i++)
		{
			for (int j = zone.x - m_activeArea; j <= zone.x + m_activeArea; j++)
			{
				Vector2i vector2i = new Vector2i(j, i);
				if (!(vector2i == zone) && PokeLocalZone(vector2i))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool PokeLocalZone(Vector2i zoneID)
	{
		if (m_zones.TryGetValue(zoneID, out var value))
		{
			value.m_ttl = 0f;
			return false;
		}
		SpawnMode mode = ((!ZNet.instance.IsServer() || IsZoneGenerated(zoneID)) ? SpawnMode.Client : SpawnMode.Full);
		if (SpawnZone(zoneID, mode, out var root))
		{
			ZoneData zoneData = new ZoneData();
			zoneData.m_root = root;
			m_zones.Add(zoneID, zoneData);
			return true;
		}
		return false;
	}

	public bool IsZoneLoaded(Vector3 point)
	{
		Vector2i zone = GetZone(point);
		return IsZoneLoaded(zone);
	}

	public bool IsZoneLoaded(Vector2i zoneID)
	{
		if (m_zones.ContainsKey(zoneID))
		{
			return !m_loadingObjectsInZones.ContainsKey(zoneID);
		}
		return false;
	}

	public bool IsActiveAreaLoaded()
	{
		Vector2i zone = GetZone(ZNet.instance.GetReferencePosition());
		for (int i = zone.y - m_activeArea; i <= zone.y + m_activeArea; i++)
		{
			for (int j = zone.x - m_activeArea; j <= zone.x + m_activeArea; j++)
			{
				if (!m_zones.ContainsKey(new Vector2i(j, i)))
				{
					return false;
				}
			}
		}
		return true;
	}

	private bool SpawnZone(Vector2i zoneID, SpawnMode mode, out GameObject root)
	{
		Vector3 zonePos = GetZonePos(zoneID);
		Heightmap componentInChildren = m_zonePrefab.GetComponentInChildren<Heightmap>();
		if (!HeightmapBuilder.instance.IsTerrainReady(zonePos, componentInChildren.m_width, componentInChildren.m_scale, componentInChildren.IsDistantLod, WorldGenerator.instance))
		{
			root = null;
			return false;
		}
		if (m_locationInstances.TryGetValue(zoneID, out var value) && !value.m_placed && !PokeCanSpawnLocation(value.m_location, isFirstSpawn: true))
		{
			root = null;
			return false;
		}
		root = UnityEngine.Object.Instantiate(m_zonePrefab, zonePos, Quaternion.identity);
		if ((mode == SpawnMode.Ghost || mode == SpawnMode.Full) && !IsZoneGenerated(zoneID))
		{
			Heightmap componentInChildren2 = root.GetComponentInChildren<Heightmap>();
			m_tempClearAreas.Clear();
			m_tempSpawnedObjects.Clear();
			PlaceLocations(zoneID, zonePos, root.transform, componentInChildren2, m_tempClearAreas, mode, m_tempSpawnedObjects);
			PlaceVegetation(zoneID, zonePos, root.transform, componentInChildren2, m_tempClearAreas, mode, m_tempSpawnedObjects);
			PlaceZoneCtrl(zoneID, zonePos, mode, m_tempSpawnedObjects);
			if (mode == SpawnMode.Ghost)
			{
				foreach (GameObject tempSpawnedObject in m_tempSpawnedObjects)
				{
					UnityEngine.Object.Destroy(tempSpawnedObject);
				}
				m_tempSpawnedObjects.Clear();
				UnityEngine.Object.Destroy(root);
				root = null;
			}
			SetZoneGenerated(zoneID);
		}
		return true;
	}

	private void PlaceZoneCtrl(Vector2i zoneID, Vector3 zoneCenterPos, SpawnMode mode, List<GameObject> spawnedObjects)
	{
		if (mode == SpawnMode.Full || mode == SpawnMode.Ghost)
		{
			if (mode == SpawnMode.Ghost)
			{
				ZNetView.StartGhostInit();
			}
			GameObject gameObject = UnityEngine.Object.Instantiate(m_zoneCtrlPrefab, zoneCenterPos, Quaternion.identity);
			gameObject.GetComponent<ZNetView>();
			if (mode == SpawnMode.Ghost)
			{
				spawnedObjects.Add(gameObject);
				ZNetView.FinishGhostInit();
			}
		}
	}

	private Vector3 GetRandomPointInRadius(Vector3 center, float radius)
	{
		float f = UnityEngine.Random.value * MathF.PI * 2f;
		float num = UnityEngine.Random.Range(0f, radius);
		return center + new Vector3(Mathf.Sin(f) * num, 0f, Mathf.Cos(f) * num);
	}

	private void PlaceVegetation(Vector2i zoneID, Vector3 zoneCenterPos, Transform parent, Heightmap hmap, List<ClearArea> clearAreas, SpawnMode mode, List<GameObject> spawnedObjects)
	{
		UnityEngine.Random.State state = UnityEngine.Random.state;
		int seed = WorldGenerator.instance.GetSeed();
		int num = 1;
		foreach (ZoneVegetation item in m_vegetation)
		{
			num++;
			if (!item.m_enable || !hmap.HaveBiome(item.m_biome))
			{
				continue;
			}
			UnityEngine.Random.InitState(seed + zoneID.x * 4271 + zoneID.y * 9187 + item.m_prefab.name.GetStableHashCode());
			int num2 = 1;
			if (item.m_max < 1f)
			{
				if (UnityEngine.Random.value > item.m_max)
				{
					continue;
				}
			}
			else
			{
				num2 = UnityEngine.Random.Range((int)item.m_min, (int)item.m_max + 1);
			}
			bool flag = item.m_prefab.GetComponent<ZNetView>() != null;
			float num3 = Mathf.Cos(MathF.PI / 180f * item.m_maxTilt);
			float num4 = Mathf.Cos(MathF.PI / 180f * item.m_minTilt);
			float num5 = 32f - item.m_groupRadius;
			s_tempVeg.Clear();
			int num6 = (item.m_forcePlacement ? (num2 * 50) : num2);
			int num7 = 0;
			for (int i = 0; i < num6; i++)
			{
				Vector3 vector = new Vector3(UnityEngine.Random.Range(zoneCenterPos.x - num5, zoneCenterPos.x + num5), 0f, UnityEngine.Random.Range(zoneCenterPos.z - num5, zoneCenterPos.z + num5));
				int num8 = UnityEngine.Random.Range(item.m_groupSizeMin, item.m_groupSizeMax + 1);
				bool flag2 = false;
				for (int j = 0; j < num8; j++)
				{
					Vector3 p = ((j == 0) ? vector : GetRandomPointInRadius(vector, item.m_groupRadius));
					float y = UnityEngine.Random.Range(0, 360);
					float num9 = UnityEngine.Random.Range(item.m_scaleMin, item.m_scaleMax);
					float x = UnityEngine.Random.Range(0f - item.m_randTilt, item.m_randTilt);
					float z = UnityEngine.Random.Range(0f - item.m_randTilt, item.m_randTilt);
					if (item.m_blockCheck && IsBlocked(p))
					{
						continue;
					}
					GetGroundData(ref p, out var normal, out var biome, out var biomeArea, out var hmap2);
					if ((item.m_biome & biome) == 0 || (item.m_biomeArea & biomeArea) == 0)
					{
						continue;
					}
					if (item.m_snapToStaticSolid && GetStaticSolidHeight(p, out var height, out var normal2))
					{
						p.y = height;
						normal = normal2;
					}
					float num10 = p.y - 30f;
					if (num10 < item.m_minAltitude || num10 > item.m_maxAltitude)
					{
						continue;
					}
					if (item.m_minVegetation != item.m_maxVegetation)
					{
						float vegetationMask = hmap2.GetVegetationMask(p);
						if (vegetationMask > item.m_maxVegetation || vegetationMask < item.m_minVegetation)
						{
							continue;
						}
					}
					if (item.m_minOceanDepth != item.m_maxOceanDepth)
					{
						float oceanDepth = hmap2.GetOceanDepth(p);
						if (oceanDepth < item.m_minOceanDepth || oceanDepth > item.m_maxOceanDepth)
						{
							continue;
						}
					}
					if (normal.y < num3 || normal.y > num4)
					{
						continue;
					}
					if (item.m_terrainDeltaRadius > 0f)
					{
						GetTerrainDelta(p, item.m_terrainDeltaRadius, out var delta, out var _);
						if (delta > item.m_maxTerrainDelta || delta < item.m_minTerrainDelta)
						{
							continue;
						}
					}
					if (item.m_minDistanceFromCenter > 0f || item.m_maxDistanceFromCenter > 0f)
					{
						float num11 = Utils.LengthXZ(p);
						if ((item.m_minDistanceFromCenter > 0f && num11 < item.m_minDistanceFromCenter) || (item.m_maxDistanceFromCenter > 0f && num11 > item.m_maxDistanceFromCenter))
						{
							continue;
						}
					}
					if (item.m_inForest)
					{
						float forestFactor = WorldGenerator.GetForestFactor(p);
						if (forestFactor < item.m_forestTresholdMin || forestFactor > item.m_forestTresholdMax)
						{
							continue;
						}
					}
					if (item.m_surroundCheckVegetation)
					{
						float num12 = 0f;
						for (int k = 0; k < item.m_surroundCheckLayers; k++)
						{
							float num13 = (float)(k + 1) / (float)item.m_surroundCheckLayers * item.m_surroundCheckDistance;
							for (int l = 0; l < 6; l++)
							{
								float f = (float)l / 6f * MathF.PI * 2f;
								float vegetationMask2 = hmap2.GetVegetationMask(p + new Vector3(Mathf.Sin(f) * num13, 0f, Mathf.Cos(f) * num13));
								float num14 = (1f - num13) / (item.m_surroundCheckDistance * 2f);
								num12 += vegetationMask2 * num14;
							}
						}
						s_tempVeg.Add(num12);
						if (s_tempVeg.Count < 10)
						{
							continue;
						}
						float num15 = s_tempVeg.Max();
						float num16 = s_tempVeg.Average();
						float num17 = num16 + (num15 - num16) * item.m_surroundBetterThanAverage;
						if (num12 < num17)
						{
							continue;
						}
					}
					if (InsideClearArea(clearAreas, p))
					{
						continue;
					}
					if (item.m_snapToWater)
					{
						p.y = 30f;
					}
					p.y += item.m_groundOffset;
					Quaternion identity = Quaternion.identity;
					if (item.m_chanceToUseGroundTilt > 0f && UnityEngine.Random.value <= item.m_chanceToUseGroundTilt)
					{
						Quaternion quaternion = Quaternion.Euler(0f, y, 0f);
						identity = Quaternion.LookRotation(Vector3.Cross(normal, quaternion * Vector3.forward), normal);
					}
					else
					{
						identity = Quaternion.Euler(x, y, z);
					}
					if (flag)
					{
						if (mode == SpawnMode.Full || mode == SpawnMode.Ghost)
						{
							if (mode == SpawnMode.Ghost)
							{
								ZNetView.StartGhostInit();
							}
							GameObject gameObject = UnityEngine.Object.Instantiate(item.m_prefab, p, identity);
							ZNetView component = gameObject.GetComponent<ZNetView>();
							if (num9 != gameObject.transform.localScale.x)
							{
								component.SetLocalScale(new Vector3(num9, num9, num9));
								Collider[] componentsInChildren = gameObject.GetComponentsInChildren<Collider>();
								foreach (Collider obj in componentsInChildren)
								{
									obj.enabled = false;
									obj.enabled = true;
								}
							}
							if (mode == SpawnMode.Ghost)
							{
								spawnedObjects.Add(gameObject);
								ZNetView.FinishGhostInit();
							}
						}
					}
					else
					{
						GameObject obj2 = UnityEngine.Object.Instantiate(item.m_prefab, p, identity);
						obj2.transform.localScale = new Vector3(num9, num9, num9);
						obj2.transform.SetParent(parent, worldPositionStays: true);
					}
					flag2 = true;
				}
				if (flag2)
				{
					num7++;
				}
				if (num7 >= num2)
				{
					break;
				}
			}
		}
		UnityEngine.Random.state = state;
	}

	private bool InsideClearArea(List<ClearArea> areas, Vector3 point)
	{
		foreach (ClearArea area in areas)
		{
			if (point.x > area.m_center.x - area.m_radius && point.x < area.m_center.x + area.m_radius && point.z > area.m_center.z - area.m_radius && point.z < area.m_center.z + area.m_radius)
			{
				return true;
			}
		}
		return false;
	}

	private ZoneLocation GetLocation(int hash)
	{
		if (m_locationsByHash.TryGetValue(hash, out var value))
		{
			return value;
		}
		return null;
	}

	private ZoneLocation GetLocation(string name)
	{
		foreach (ZoneLocation location in m_locations)
		{
			if (!location.m_prefab.IsValid)
			{
				if (location.m_enable)
				{
					throw new NullReferenceException($"Location in list of locations was invalid! Asset: {location.m_prefab.m_assetID}");
				}
			}
			else if (location.m_prefab.Name == name)
			{
				return location;
			}
		}
		return null;
	}

	private void ClearNonPlacedLocations()
	{
		Dictionary<Vector2i, LocationInstance> dictionary = new Dictionary<Vector2i, LocationInstance>();
		foreach (KeyValuePair<Vector2i, LocationInstance> locationInstance in m_locationInstances)
		{
			if (locationInstance.Value.m_placed)
			{
				dictionary.Add(locationInstance.Key, locationInstance.Value);
			}
		}
		m_locationInstances = dictionary;
	}

	private void CheckLocationDuplicates()
	{
		ZLog.Log("Checking for location duplicates");
		Stopwatch stopwatch = new Stopwatch();
		stopwatch.Start();
		for (int i = 0; i < m_locations.Count; i++)
		{
			ZoneLocation zoneLocation = m_locations[i];
			if (!zoneLocation.m_enable)
			{
				continue;
			}
			for (int j = i + 1; j < m_locations.Count; j++)
			{
				ZoneLocation zoneLocation2 = m_locations[j];
				if (zoneLocation2.m_enable)
				{
					if (zoneLocation.m_prefab.Name == zoneLocation2.m_prefab.Name)
					{
						SoftReference<GameObject> prefab = zoneLocation.m_prefab;
						ZLog.LogWarning("Two locations have the same location prefab name " + prefab.ToString());
					}
					if (zoneLocation.m_prefab == zoneLocation2.m_prefab)
					{
						ZLog.LogWarning($"Locations {zoneLocation.m_prefab} and {zoneLocation2.m_prefab} point to the same location prefab");
					}
				}
			}
		}
		stopwatch.Stop();
		ZLog.Log($"Location duplicate check took {stopwatch.Elapsed.TotalMilliseconds} ms");
	}

	public void GenerateLocations()
	{
		if (m_generateLocationsCoroutine == null)
		{
			if (!Application.isPlaying)
			{
				ZLog.Log("Setting up locations");
				SetupLocations();
			}
			m_generateLocationsCoroutine = StartCoroutine(GenerateLocationsTimeSliced());
		}
	}

	private IEnumerator GenerateLocationsTimeSliced()
	{
		m_estimatedGenerateLocationsCompletionTime = DateTime.MaxValue;
		yield return null;
		Stopwatch timeSliceStopwatch = Stopwatch.StartNew();
		ZLog.Log("Generating locations");
		DateTime startTime = DateTime.UtcNow;
		ClearNonPlacedLocations();
		List<ZoneLocation> ordered = m_locations.OrderByDescending((ZoneLocation a) => a.m_prioritized).ToList();
		int totalEstimatedIterationsLeft = 0;
		for (int num = ordered.Count - 1; num >= 0; num--)
		{
			ZoneLocation zoneLocation = ordered[num];
			if (!zoneLocation.m_enable || zoneLocation.m_quantity == 0)
			{
				ordered.RemoveAt(num);
			}
			else
			{
				totalEstimatedIterationsLeft += (zoneLocation.m_prioritized ? 200000 : 100000) * 20 / 2;
			}
		}
		int runIterations = 0;
		for (int i = 0; i < ordered.Count; i++)
		{
			ZoneLocation location = ordered[i];
			if (timeSliceStopwatch.Elapsed.TotalSeconds >= (double)m_timeSlicedGenerationTimeBudget)
			{
				yield return null;
				timeSliceStopwatch.Restart();
			}
			ZPackage iterationsPkg = new ZPackage();
			yield return GenerateLocationsTimeSliced(location, timeSliceStopwatch, iterationsPkg);
			runIterations += iterationsPkg.ReadInt();
			totalEstimatedIterationsLeft -= (location.m_prioritized ? 200000 : 100000) * 20 / 2;
			DateTime utcNow = DateTime.UtcNow;
			double totalSeconds = (utcNow - startTime).TotalSeconds;
			double num2 = ((runIterations != 0) ? (totalSeconds / (double)runIterations) : double.MaxValue);
			double num3 = num2 * (double)totalEstimatedIterationsLeft;
			if (double.IsInfinity(num3))
			{
				m_estimatedGenerateLocationsCompletionTime = DateTime.MaxValue;
			}
			else
			{
				m_estimatedGenerateLocationsCompletionTime = utcNow.AddSeconds(num3);
			}
			m_generateLocationsProgress = (float)(i + 1) / (float)ordered.Count;
		}
		LocationsGenerated = true;
		ZLog.Log(" Done generating locations, duration:" + (DateTime.UtcNow - startTime).TotalMilliseconds + " ms");
		m_generateLocationsCoroutine = null;
	}

	private int CountNrOfLocation(ZoneLocation location)
	{
		int num = 0;
		foreach (LocationInstance value in m_locationInstances.Values)
		{
			if (value.m_location.m_prefab.Name == location.m_prefab.Name)
			{
				num++;
			}
		}
		if (num > 0)
		{
			SoftReference<GameObject> prefab = location.m_prefab;
			ZLog.Log("Old location found " + prefab.ToString() + " x " + num);
		}
		return num;
	}

	private IEnumerator GenerateLocationsTimeSliced(ZoneLocation location, Stopwatch timeSliceStopwatch, ZPackage iterationsPkg)
	{
		DateTime t = DateTime.Now;
		int seed = WorldGenerator.instance.GetSeed() + location.m_prefab.Name.GetStableHashCode();
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(seed);
		int errorLocationInZone = 0;
		int errorCenterDistance = 0;
		int errorBiome = 0;
		int errorBiomeArea = 0;
		int errorAlt = 0;
		int errorForest = 0;
		int errorSimilar = 0;
		int errorNotSimilar = 0;
		int errorTerrainDelta = 0;
		int errorVegetation = 0;
		float maxRadius = Mathf.Max(location.m_exteriorRadius, location.m_interiorRadius);
		int attempts = (location.m_prioritized ? 200000 : 100000);
		int iterations = 0;
		int placed = CountNrOfLocation(location);
		float maxRange = 10000f;
		if (location.m_centerFirst)
		{
			maxRange = location.m_minDistance;
		}
		if (!location.m_unique || placed <= 0)
		{
			s_tempVeg.Clear();
			int i = 0;
			while (i < attempts && placed < location.m_quantity)
			{
				if (timeSliceStopwatch.Elapsed.TotalSeconds >= (double)m_timeSlicedGenerationTimeBudget)
				{
					UnityEngine.Random.State insideState = UnityEngine.Random.state;
					UnityEngine.Random.state = state;
					yield return null;
					timeSliceStopwatch.Restart();
					state = UnityEngine.Random.state;
					UnityEngine.Random.state = insideState;
				}
				Vector2i zoneID = GetRandomZone(maxRange);
				if (location.m_centerFirst)
				{
					maxRange += 1f;
				}
				int num;
				if (m_locationInstances.ContainsKey(zoneID))
				{
					num = errorLocationInZone + 1;
					errorLocationInZone = num;
				}
				else if (!IsZoneGenerated(zoneID))
				{
					Vector3 zonePos = GetZonePos(zoneID);
					Heightmap.BiomeArea biomeArea = WorldGenerator.instance.GetBiomeArea(zonePos);
					if ((location.m_biomeArea & biomeArea) == 0)
					{
						num = errorBiomeArea + 1;
						errorBiomeArea = num;
					}
					else
					{
						for (int j = 0; j < 20; num = j + 1, j = num)
						{
							if (timeSliceStopwatch.Elapsed.TotalSeconds >= (double)m_timeSlicedGenerationTimeBudget)
							{
								UnityEngine.Random.State insideState = UnityEngine.Random.state;
								UnityEngine.Random.state = state;
								yield return null;
								timeSliceStopwatch.Restart();
								state = UnityEngine.Random.state;
								UnityEngine.Random.state = insideState;
							}
							num = iterations + 1;
							iterations = num;
							Vector3 randomPointInZone = GetRandomPointInZone(zoneID, maxRadius);
							float magnitude = randomPointInZone.magnitude;
							if (location.m_minDistance != 0f && magnitude < location.m_minDistance)
							{
								num = errorCenterDistance + 1;
								errorCenterDistance = num;
								continue;
							}
							if (location.m_maxDistance != 0f && magnitude > location.m_maxDistance)
							{
								num = errorCenterDistance + 1;
								errorCenterDistance = num;
								continue;
							}
							Heightmap.Biome biome = WorldGenerator.instance.GetBiome(randomPointInZone);
							if ((location.m_biome & biome) == 0)
							{
								num = errorBiome + 1;
								errorBiome = num;
								continue;
							}
							randomPointInZone.y = WorldGenerator.instance.GetHeight(randomPointInZone.x, randomPointInZone.z, out var mask);
							float num2 = (float)((double)randomPointInZone.y - 30.0);
							if (num2 < location.m_minAltitude || num2 > location.m_maxAltitude)
							{
								num = errorAlt + 1;
								errorAlt = num;
								continue;
							}
							if (location.m_inForest)
							{
								float forestFactor = WorldGenerator.GetForestFactor(randomPointInZone);
								if (forestFactor < location.m_forestTresholdMin || forestFactor > location.m_forestTresholdMax)
								{
									num = errorForest + 1;
									errorForest = num;
									continue;
								}
							}
							if (location.m_minDistanceFromCenter > 0f || location.m_maxDistanceFromCenter > 0f)
							{
								float num3 = Utils.LengthXZ(randomPointInZone);
								if ((location.m_minDistanceFromCenter > 0f && num3 < location.m_minDistanceFromCenter) || (location.m_maxDistanceFromCenter > 0f && num3 > location.m_maxDistanceFromCenter))
								{
									continue;
								}
							}
							WorldGenerator.instance.GetTerrainDelta(randomPointInZone, location.m_exteriorRadius, out var delta, out var _);
							if (delta > location.m_maxTerrainDelta || delta < location.m_minTerrainDelta)
							{
								num = errorTerrainDelta + 1;
								errorTerrainDelta = num;
								continue;
							}
							if (location.m_minDistanceFromSimilar > 0f && HaveLocationInRange(location.m_prefab.Name, location.m_group, randomPointInZone, location.m_minDistanceFromSimilar))
							{
								num = errorSimilar + 1;
								errorSimilar = num;
								continue;
							}
							if (location.m_maxDistanceFromSimilar > 0f && !HaveLocationInRange(location.m_prefabName, location.m_groupMax, randomPointInZone, location.m_maxDistanceFromSimilar, maxGroup: true))
							{
								num = errorNotSimilar + 1;
								errorNotSimilar = num;
								continue;
							}
							float a = mask.a;
							if (location.m_minimumVegetation > 0f && a <= location.m_minimumVegetation)
							{
								num = errorVegetation + 1;
								errorVegetation = num;
								continue;
							}
							if (location.m_maximumVegetation < 1f && a >= location.m_maximumVegetation)
							{
								num = errorVegetation + 1;
								errorVegetation = num;
								continue;
							}
							if (location.m_surroundCheckVegetation)
							{
								float num4 = 0f;
								for (int k = 0; k < location.m_surroundCheckLayers; k++)
								{
									float num5 = (float)(k + 1) / (float)location.m_surroundCheckLayers * location.m_surroundCheckDistance;
									for (int l = 0; l < 6; l++)
									{
										float f = (float)l / 6f * MathF.PI * 2f;
										Vector3 vector = randomPointInZone + new Vector3(Mathf.Sin(f) * num5, 0f, Mathf.Cos(f) * num5);
										WorldGenerator.instance.GetHeight(vector.x, vector.z, out var mask2);
										float num6 = (location.m_surroundCheckDistance - num5) / (location.m_surroundCheckDistance * 2f);
										num4 += mask2.a * num6;
									}
								}
								s_tempVeg.Add(num4);
								if (s_tempVeg.Count < 10)
								{
									continue;
								}
								float num7 = s_tempVeg.Max();
								float num8 = s_tempVeg.Average();
								float num9 = num8 + (num7 - num8) * location.m_surroundBetterThanAverage;
								if (num4 < num9)
								{
									continue;
								}
								ZLog.DevLog($"Surround check passed with a value of {num4}, cutoff was {num9}, max: {num7}, average: {num8}.");
							}
							RegisterLocation(location, randomPointInZone, generated: false);
							num = placed + 1;
							placed = num;
							break;
						}
					}
				}
				num = i + 1;
				i = num;
			}
			if (placed < location.m_quantity)
			{
				ZLog.LogWarning("Failed to place all " + location.m_prefab.Name + ", placed " + placed + " out of " + location.m_quantity);
				ZLog.DevLog("errorLocationInZone " + errorLocationInZone);
				ZLog.DevLog("errorCenterDistance " + errorCenterDistance);
				ZLog.DevLog("errorBiome " + errorBiome);
				ZLog.DevLog("errorBiomeArea " + errorBiomeArea);
				ZLog.DevLog("errorAlt " + errorAlt);
				ZLog.DevLog("errorForest " + errorForest);
				ZLog.DevLog("errorSimilar " + errorSimilar);
				ZLog.DevLog("errorNotSimilar " + errorNotSimilar);
				ZLog.DevLog("errorTerrainDelta " + errorTerrainDelta);
				ZLog.DevLog("errorVegetation " + errorVegetation);
			}
		}
		UnityEngine.Random.state = state;
		_ = DateTime.Now - t;
		iterationsPkg.Write(iterations);
		iterationsPkg.SetPos(0);
	}

	public float GetEstimatedGenerationCompletionTimeFromNow()
	{
		if (m_generateLocationsCoroutine == null)
		{
			return 0f;
		}
		DateTime utcNow = DateTime.UtcNow;
		return (float)(m_estimatedGenerateLocationsCompletionTime - utcNow).TotalSeconds;
	}

	public static float GetGenerationTimeBudgetForTargetFrameRate(out float targetFrameTime)
	{
		float num;
		if (QualitySettings.vSyncCount > 0)
		{
			num = (float)Screen.currentResolution.refreshRateRatio.denominator / (float)Screen.currentResolution.refreshRateRatio.numerator * (float)QualitySettings.vSyncCount;
		}
		else
		{
			num = 1f / (float)Application.targetFrameRate;
			if (num < 0f)
			{
				num = 0f;
			}
		}
		targetFrameTime = Mathf.Clamp(num, 1f / 60f, 1f / 30f);
		float num2 = 1f / 150f;
		return targetFrameTime - num2;
	}

	private static Vector2i GetRandomZone(float range)
	{
		int num = (int)range / 64;
		Vector2i vector2i;
		do
		{
			vector2i = new Vector2i(UnityEngine.Random.Range(-num, num), UnityEngine.Random.Range(-num, num));
		}
		while (!(GetZonePos(vector2i).magnitude < 10000f));
		return vector2i;
	}

	private static Vector3 GetRandomPointInZone(Vector2i zone, float locationRadius)
	{
		Vector3 zonePos = GetZonePos(zone);
		float x = UnityEngine.Random.Range(-32f + locationRadius, 32f - locationRadius);
		float z = UnityEngine.Random.Range(-32f + locationRadius, 32f - locationRadius);
		return zonePos + new Vector3(x, 0f, z);
	}

	private static Vector3 GetRandomPointInZone(float locationRadius)
	{
		Vector3 zonePos = GetZonePos(GetZone(new Vector3(UnityEngine.Random.Range(-10000f, 10000f), 0f, UnityEngine.Random.Range(-10000f, 10000f))));
		return new Vector3(UnityEngine.Random.Range(zonePos.x - 32f + locationRadius, zonePos.x + 32f - locationRadius), 0f, UnityEngine.Random.Range(zonePos.z - 32f + locationRadius, zonePos.z + 32f - locationRadius));
	}

	private void PlaceLocations(Vector2i zoneID, Vector3 zoneCenterPos, Transform parent, Heightmap hmap, List<ClearArea> clearAreas, SpawnMode mode, List<GameObject> spawnedObjects)
	{
		DateTime now = DateTime.Now;
		if (m_locationInstances.TryGetValue(zoneID, out var value) && !value.m_placed)
		{
			Vector3 p = value.m_position;
			GetGroundData(ref p, out var _, out var _, out var _, out var _);
			if (value.m_location.m_snapToWater)
			{
				p.y = 30f;
			}
			if (value.m_location.m_clearArea)
			{
				ClearArea item = new ClearArea(p, value.m_location.m_exteriorRadius);
				clearAreas.Add(item);
			}
			Quaternion rot = Quaternion.identity;
			if (value.m_location.m_slopeRotation)
			{
				GetTerrainDelta(p, value.m_location.m_exteriorRadius, out var _, out var slopeDirection);
				Vector3 forward = new Vector3(slopeDirection.x, 0f, slopeDirection.z);
				forward.Normalize();
				rot = Quaternion.LookRotation(forward);
				Vector3 eulerAngles = rot.eulerAngles;
				eulerAngles.y = Mathf.Round(eulerAngles.y / 22.5f) * 22.5f;
				rot.eulerAngles = eulerAngles;
			}
			else if (value.m_location.m_randomRotation)
			{
				rot = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 16) * 22.5f, 0f);
			}
			int seed = WorldGenerator.instance.GetSeed() + zoneID.x * 4271 + zoneID.y * 9187;
			SpawnLocation(value.m_location, seed, p, rot, mode, spawnedObjects);
			value.m_placed = true;
			m_locationInstances[zoneID] = value;
			TimeSpan timeSpan = DateTime.Now - now;
			string[] obj = new string[5] { "Placed locations in zone ", null, null, null, null };
			Vector2i vector2i = zoneID;
			obj[1] = vector2i.ToString();
			obj[2] = "  duration ";
			obj[3] = timeSpan.TotalMilliseconds.ToString();
			obj[4] = " ms";
			ZLog.Log(string.Concat(obj));
			if (value.m_location.m_unique)
			{
				RemoveUnplacedLocations(value.m_location);
			}
			if (value.m_location.m_iconPlaced)
			{
				SendLocationIcons(ZRoutedRpc.Everybody);
			}
		}
	}

	private void RemoveUnplacedLocations(ZoneLocation location)
	{
		List<Vector2i> list = new List<Vector2i>();
		foreach (KeyValuePair<Vector2i, LocationInstance> locationInstance in m_locationInstances)
		{
			if (locationInstance.Value.m_location == location && !locationInstance.Value.m_placed)
			{
				list.Add(locationInstance.Key);
			}
		}
		foreach (Vector2i item in list)
		{
			m_locationInstances.Remove(item);
		}
		ZLog.DevLog("Removed " + list.Count + " unplaced locations of type " + location.m_prefab.Name);
	}

	public bool TestSpawnLocation(string name, Vector3 pos, bool disableSave = true)
	{
		if (!ZNet.instance.IsServer())
		{
			return false;
		}
		ZoneLocation location = GetLocation(name);
		if (location == null)
		{
			ZLog.Log("Missing location:" + name);
			Console.instance.Print("Missing location:" + name);
			return false;
		}
		if (!location.m_prefab.IsValid)
		{
			ZLog.Log("Missing prefab in location:" + name);
			Console.instance.Print("Missing location:" + name);
			return false;
		}
		float num = Mathf.Max(location.m_exteriorRadius, location.m_interiorRadius);
		Vector3 zonePos = GetZonePos(GetZone(pos));
		pos.x = Mathf.Clamp(pos.x, zonePos.x - 32f + num, zonePos.x + 32f - num);
		pos.z = Mathf.Clamp(pos.z, zonePos.z - 32f + num, zonePos.z + 32f - num);
		string[] obj = new string[6]
		{
			"radius ",
			num.ToString(),
			"  ",
			null,
			null,
			null
		};
		Vector3 vector = zonePos;
		obj[3] = vector.ToString();
		obj[4] = " ";
		vector = pos;
		obj[5] = vector.ToString();
		ZLog.Log(string.Concat(obj));
		MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Location spawned, " + (disableSave ? "world saving DISABLED until restart" : "CAUTION! world saving is ENABLED, use normal location command to disable it!"));
		m_didZoneTest = disableSave;
		float y = (float)UnityEngine.Random.Range(0, 16) * 22.5f;
		List<GameObject> spawnedGhostObjects = new List<GameObject>();
		SpawnLocation(location, UnityEngine.Random.Range(0, 99999), pos, Quaternion.Euler(0f, y, 0f), SpawnMode.Full, spawnedGhostObjects);
		return true;
	}

	private bool PokeCanSpawnLocation(ZoneLocation location, bool isFirstSpawn)
	{
		LocationPrefabLoadData locationPrefabLoadData = null;
		for (int i = 0; i < m_locationPrefabs.Count; i++)
		{
			if (m_locationPrefabs[i].PrefabAssetID == location.m_prefab.m_assetID)
			{
				locationPrefabLoadData = m_locationPrefabs[i];
				break;
			}
		}
		if (locationPrefabLoadData == null)
		{
			locationPrefabLoadData = new LocationPrefabLoadData(location.m_prefab, isFirstSpawn);
			m_locationPrefabs.Add(locationPrefabLoadData);
		}
		locationPrefabLoadData.m_iterationLifetime = GetLocationPrefabLifetime();
		return locationPrefabLoadData.IsLoaded;
	}

	public int GetLocationPrefabLifetime()
	{
		int num = 2 * (m_activeArea + m_activeDistantArea) + 1;
		int num2 = num * num;
		int num3 = ((!ZNet.instance.IsServer()) ? 1 : (ZNet.instance.GetPeers().Count + 1));
		return num2 * num3;
	}

	public bool ShouldDelayProxyLocationSpawning(int hash)
	{
		ZoneLocation location = GetLocation(hash);
		if (location == null)
		{
			ZLog.LogWarning("Missing location:" + hash);
			return false;
		}
		return !PokeCanSpawnLocation(location, isFirstSpawn: false);
	}

	public GameObject SpawnProxyLocation(int hash, int seed, Vector3 pos, Quaternion rot)
	{
		ZoneLocation location = GetLocation(hash);
		if (location == null)
		{
			ZLog.LogWarning("Missing location:" + hash);
			return null;
		}
		List<GameObject> spawnedGhostObjects = new List<GameObject>();
		return SpawnLocation(location, seed, pos, rot, SpawnMode.Client, spawnedGhostObjects);
	}

	private GameObject SpawnLocation(ZoneLocation location, int seed, Vector3 pos, Quaternion rot, SpawnMode mode, List<GameObject> spawnedGhostObjects)
	{
		location.m_prefab.Load();
		ZNetView[] enabledComponentsInChildren = Utils.GetEnabledComponentsInChildren<ZNetView>(location.m_prefab.Asset);
		RandomSpawn[] enabledComponentsInChildren2 = Utils.GetEnabledComponentsInChildren<RandomSpawn>(location.m_prefab.Asset);
		for (int i = 0; i < enabledComponentsInChildren2.Length; i++)
		{
			enabledComponentsInChildren2[i].Prepare();
		}
		Location component = location.m_prefab.Asset.GetComponent<Location>();
		Vector3 vector = Vector3.zero;
		Vector3 vector2 = Vector3.zero;
		if ((bool)component.m_interiorTransform && (bool)component.m_generator)
		{
			vector = component.m_interiorTransform.localPosition;
			vector2 = component.m_generator.transform.localPosition;
		}
		Vector3 position = location.m_prefab.Asset.transform.position;
		Quaternion rotation = location.m_prefab.Asset.transform.rotation;
		location.m_prefab.Asset.transform.position = Vector3.zero;
		location.m_prefab.Asset.transform.rotation = Quaternion.identity;
		UnityEngine.Random.InitState(seed);
		bool flag = (bool)component && component.m_useCustomInteriorTransform && (bool)component.m_interiorTransform && (bool)component.m_generator;
		Vector3 localPosition = Vector3.zero;
		Vector3 localPosition2 = Vector3.zero;
		Quaternion localRotation = Quaternion.identity;
		if (flag)
		{
			localPosition = component.m_generator.transform.localPosition;
			localPosition2 = component.m_interiorTransform.localPosition;
			localRotation = component.m_interiorTransform.localRotation;
			Vector3 zonePos = GetZonePos(GetZone(pos));
			component.m_generator.transform.localPosition = Vector3.zero;
			Vector3 vector3 = zonePos + vector + vector2 - pos;
			Vector3 localPosition3 = (Matrix4x4.Rotate(Quaternion.Inverse(rot)) * Matrix4x4.Translate(vector3)).GetColumn(3);
			localPosition3.y = component.m_interiorTransform.localPosition.y;
			component.m_interiorTransform.localPosition = localPosition3;
			component.m_interiorTransform.localRotation = Quaternion.Inverse(rot);
		}
		if ((bool)component && (bool)component.m_generator && component.m_useCustomInteriorTransform != component.m_generator.m_useCustomInteriorTransform)
		{
			ZLog.LogWarning(component.name + " & " + component.m_generator.name + " don't have matching m_useCustomInteriorTransform()! If one has it the other should as well!");
		}
		GameObject gameObject = null;
		if (mode == SpawnMode.Full || mode == SpawnMode.Ghost)
		{
			UnityEngine.Random.InitState(seed);
			RandomSpawn[] array = enabledComponentsInChildren2;
			foreach (RandomSpawn obj in array)
			{
				Vector3 position2 = obj.gameObject.transform.position;
				Vector3 pos2 = pos + rot * position2;
				obj.Randomize(pos2, component);
			}
			WearNTear.m_randomInitialDamage = component.m_applyRandomDamage;
			ZNetView[] array2 = enabledComponentsInChildren;
			foreach (ZNetView zNetView in array2)
			{
				if (!zNetView.gameObject.activeSelf)
				{
					continue;
				}
				Vector3 position3 = zNetView.gameObject.transform.position;
				Vector3 position4 = pos + rot * position3;
				Quaternion rotation2 = zNetView.gameObject.transform.rotation;
				Quaternion rotation3 = rot * rotation2;
				if (mode == SpawnMode.Ghost)
				{
					ZNetView.StartGhostInit();
				}
				GameObject gameObject2 = UnityEngine.Object.Instantiate(zNetView.gameObject, position4, rotation3);
				gameObject2.HoldReferenceTo(location.m_prefab);
				DungeonGenerator component2 = gameObject2.GetComponent<DungeonGenerator>();
				if ((bool)component2)
				{
					if (flag)
					{
						component2.m_originalPosition = vector2;
					}
					component2.Generate(mode);
				}
				if (mode == SpawnMode.Ghost)
				{
					spawnedGhostObjects.Add(gameObject2);
					ZNetView.FinishGhostInit();
				}
			}
			WearNTear.m_randomInitialDamage = false;
			array = enabledComponentsInChildren2;
			for (int j = 0; j < array.Length; j++)
			{
				array[j].Reset();
			}
			array2 = enabledComponentsInChildren;
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j].gameObject.SetActive(value: true);
			}
			location.m_prefab.Asset.transform.position = position;
			location.m_prefab.Asset.transform.rotation = rotation;
			if (flag)
			{
				component.m_generator.transform.localPosition = localPosition;
				component.m_interiorTransform.localPosition = localPosition2;
				component.m_interiorTransform.localRotation = localRotation;
			}
			CreateLocationProxy(location, seed, pos, rot, mode, spawnedGhostObjects);
		}
		else
		{
			UnityEngine.Random.InitState(seed);
			RandomSpawn[] array = enabledComponentsInChildren2;
			foreach (RandomSpawn obj2 in array)
			{
				Vector3 position5 = obj2.gameObject.transform.position;
				Vector3 pos3 = pos + rot * position5;
				obj2.Randomize(pos3, component);
			}
			ZNetView[] array2 = enabledComponentsInChildren;
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j].gameObject.SetActive(value: false);
			}
			gameObject = SoftReferenceableAssets.Utils.Instantiate(location.m_prefab, pos, rot);
			gameObject.SetActive(value: true);
			array = enabledComponentsInChildren2;
			for (int j = 0; j < array.Length; j++)
			{
				array[j].Reset();
			}
			array2 = enabledComponentsInChildren;
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j].gameObject.SetActive(value: true);
			}
			location.m_prefab.Asset.transform.position = position;
			location.m_prefab.Asset.transform.rotation = rotation;
			if (flag)
			{
				component.m_generator.transform.localPosition = localPosition;
				component.m_interiorTransform.localPosition = localPosition2;
				component.m_interiorTransform.localRotation = localRotation;
			}
		}
		location.m_prefab.Release();
		SnapToGround.SnappAll();
		return gameObject;
	}

	private void CreateLocationProxy(ZoneLocation location, int seed, Vector3 pos, Quaternion rotation, SpawnMode mode, List<GameObject> spawnedGhostObjects)
	{
		if (mode == SpawnMode.Ghost)
		{
			ZNetView.StartGhostInit();
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(m_locationProxyPrefab, pos, rotation);
		LocationProxy component = gameObject.GetComponent<LocationProxy>();
		bool spawnNow = mode == SpawnMode.Full;
		component.SetLocation(location.m_prefab.Name, seed, spawnNow);
		if (mode == SpawnMode.Ghost)
		{
			spawnedGhostObjects.Add(gameObject);
			ZNetView.FinishGhostInit();
		}
	}

	private void RegisterLocation(ZoneLocation location, Vector3 pos, bool generated)
	{
		LocationInstance value = new LocationInstance
		{
			m_location = location,
			m_position = pos,
			m_placed = generated
		};
		Vector2i zone = GetZone(pos);
		if (m_locationInstances.ContainsKey(zone))
		{
			Vector2i vector2i = zone;
			ZLog.LogWarning("Location already exist in zone " + vector2i.ToString());
		}
		else
		{
			m_locationInstances.Add(zone, value);
		}
	}

	private bool HaveLocationInRange(string prefabName, string group, Vector3 p, float radius, bool maxGroup = false)
	{
		foreach (LocationInstance value in m_locationInstances.Values)
		{
			if ((value.m_location.m_prefab.Name == prefabName || (!maxGroup && group.Length > 0 && group == value.m_location.m_group) || (maxGroup && group.Length > 0 && group == value.m_location.m_groupMax)) && Vector3.Distance(value.m_position, p) < radius)
			{
				return true;
			}
		}
		return false;
	}

	public bool GetLocationIcon(string name, out Vector3 pos)
	{
		if (ZNet.instance.IsServer())
		{
			foreach (KeyValuePair<Vector2i, LocationInstance> locationInstance in m_locationInstances)
			{
				if ((locationInstance.Value.m_location.m_iconAlways || (locationInstance.Value.m_location.m_iconPlaced && locationInstance.Value.m_placed)) && locationInstance.Value.m_location.m_prefab.Name == name)
				{
					pos = locationInstance.Value.m_position;
					return true;
				}
			}
		}
		else
		{
			foreach (KeyValuePair<Vector3, string> locationIcon in m_locationIcons)
			{
				if (locationIcon.Value == name)
				{
					pos = locationIcon.Key;
					return true;
				}
			}
		}
		pos = Vector3.zero;
		return false;
	}

	public void GetLocationIcons(Dictionary<Vector3, string> icons)
	{
		if (ZNet.instance.IsServer())
		{
			foreach (LocationInstance value in m_locationInstances.Values)
			{
				if (value.m_location.m_iconAlways || (value.m_location.m_iconPlaced && value.m_placed))
				{
					icons[value.m_position] = value.m_location.m_prefab.Name;
				}
			}
			return;
		}
		foreach (KeyValuePair<Vector3, string> locationIcon in m_locationIcons)
		{
			icons.Add(locationIcon.Key, locationIcon.Value);
		}
	}

	private void GetTerrainDelta(Vector3 center, float radius, out float delta, out Vector3 slopeDirection)
	{
		int num = 10;
		float num2 = -999999f;
		float num3 = 999999f;
		Vector3 vector = center;
		Vector3 vector2 = center;
		for (int i = 0; i < num; i++)
		{
			Vector2 vector3 = UnityEngine.Random.insideUnitCircle * radius;
			Vector3 vector4 = center + new Vector3(vector3.x, 0f, vector3.y);
			float groundHeight = GetGroundHeight(vector4);
			if (groundHeight < num3)
			{
				num3 = groundHeight;
				vector2 = vector4;
			}
			if (groundHeight > num2)
			{
				num2 = groundHeight;
				vector = vector4;
			}
		}
		delta = num2 - num3;
		slopeDirection = Vector3.Normalize(vector2 - vector);
	}

	public bool IsBlocked(Vector3 p)
	{
		p.y += 2000f;
		if (Physics.Raycast(p, Vector3.down, 10000f, m_blockRayMask))
		{
			return true;
		}
		return false;
	}

	public float GetGroundHeight(Vector3 p)
	{
		Vector3 origin = p;
		origin.y = 6000f;
		if (Physics.Raycast(origin, Vector3.down, out var hitInfo, 10000f, m_terrainRayMask))
		{
			return hitInfo.point.y;
		}
		return p.y;
	}

	public bool GetGroundHeight(Vector3 p, out float height)
	{
		p.y = 6000f;
		if (Physics.Raycast(p, Vector3.down, out var hitInfo, 10000f, m_terrainRayMask))
		{
			height = hitInfo.point.y;
			return true;
		}
		height = 0f;
		return false;
	}

	public float GetSolidHeight(Vector3 p)
	{
		Vector3 origin = p;
		origin.y += 1000f;
		if (Physics.Raycast(origin, Vector3.down, out var hitInfo, 2000f, m_solidRayMask))
		{
			return hitInfo.point.y;
		}
		return p.y;
	}

	public bool GetSolidHeight(Vector3 p, out float height, int heightMargin = 1000)
	{
		p.y += heightMargin;
		if (Physics.Raycast(p, Vector3.down, out var hitInfo, 2000f, m_solidRayMask) && !hitInfo.collider.attachedRigidbody)
		{
			height = hitInfo.point.y;
			return true;
		}
		height = 0f;
		return false;
	}

	public bool GetSolidHeight(Vector3 p, float radius, out float height, Transform ignore)
	{
		height = p.y - 1000f;
		p.y += 1000f;
		int num = ((!(radius <= 0f)) ? Physics.SphereCastNonAlloc(p, radius, Vector3.down, rayHits, 2000f, m_solidRayMask) : Physics.RaycastNonAlloc(p, Vector3.down, rayHits, 2000f, m_solidRayMask));
		s_rayHits.Clear();
		s_rayHitsHeight.Clear();
		for (int i = 0; i < num; i++)
		{
			RaycastHit item = rayHits[i];
			float y = item.point.y;
			Utils.InsertSortNoAlloc(s_rayHits, item, s_rayHitsHeight, y);
		}
		for (int num2 = s_rayHits.Count - 1; num2 >= 0; num2--)
		{
			RaycastHit raycastHit = rayHits[num2];
			Collider collider = raycastHit.collider;
			if (!(collider.attachedRigidbody != null) && (!(ignore != null) || !Utils.IsParent(collider.transform, ignore)))
			{
				height = Mathf.Max(height, s_rayHitsHeight[num2]);
				return true;
			}
		}
		return false;
	}

	public bool GetSolidHeight(Vector3 p, out float height, out Vector3 normal, out GameObject go)
	{
		p.y += 1000f;
		if (Physics.Raycast(p, Vector3.down, out var hitInfo, 2000f, m_solidRayMask) && !hitInfo.collider.attachedRigidbody)
		{
			height = hitInfo.point.y;
			normal = hitInfo.normal;
			go = hitInfo.collider.gameObject;
			return true;
		}
		height = 0f;
		normal = Vector3.zero;
		go = null;
		return false;
	}

	public bool GetStaticSolidHeight(Vector3 p, out float height, out Vector3 normal)
	{
		p.y += 1000f;
		if (Physics.Raycast(p, Vector3.down, out var hitInfo, 2000f, m_staticSolidRayMask) && !hitInfo.collider.attachedRigidbody)
		{
			height = hitInfo.point.y;
			normal = hitInfo.normal;
			return true;
		}
		height = 0f;
		normal = Vector3.zero;
		return false;
	}

	public bool FindFloor(Vector3 p, out float height)
	{
		if (Physics.Raycast(p + Vector3.up * 1f, Vector3.down, out var hitInfo, 1000f, m_solidRayMask))
		{
			height = hitInfo.point.y;
			return true;
		}
		height = 0f;
		return false;
	}

	public float GetGroundOffset(Vector3 position)
	{
		GetGroundData(ref position, out var _, out var _, out var _, out var hmap);
		if ((bool)hmap)
		{
			return hmap.GetHeightOffset(position);
		}
		return 0f;
	}

	public static bool IsLavaPreHeightmap(Vector3 position, float lavaValue = 0.6f)
	{
		if (WorldGenerator.instance.GetBiome(position.x, position.z) != Heightmap.Biome.AshLands)
		{
			return false;
		}
		WorldGenerator.instance.GetBiomeHeight(Heightmap.Biome.AshLands, position.x, position.z, out var mask);
		return mask.a > lavaValue;
	}

	public bool IsLava(Vector3 position, bool defaultTrue = false)
	{
		GetGroundData(ref position, out var _, out var _, out var _, out var hmap);
		if (!hmap)
		{
			return defaultTrue;
		}
		return hmap.IsLava(position);
	}

	public bool IsLava(ref Vector3 position, bool defaultTrue = false)
	{
		GetGroundData(ref position, out var _, out var _, out var _, out var hmap);
		if (!hmap)
		{
			return defaultTrue;
		}
		return hmap.IsLava(position);
	}

	public void GetGroundData(ref Vector3 p, out Vector3 normal, out Heightmap.Biome biome, out Heightmap.BiomeArea biomeArea, out Heightmap hmap)
	{
		biome = Heightmap.Biome.None;
		biomeArea = Heightmap.BiomeArea.Everything;
		hmap = null;
		if (Physics.Raycast(p + Vector3.up * 5000f, Vector3.down, out var hitInfo, 10000f, m_terrainRayMask))
		{
			p.y = hitInfo.point.y;
			normal = hitInfo.normal;
			Heightmap component = hitInfo.collider.GetComponent<Heightmap>();
			if ((bool)component)
			{
				biome = component.GetBiome(hitInfo.point);
				biomeArea = component.GetBiomeArea();
				hmap = component;
			}
		}
		else
		{
			normal = Vector3.up;
		}
	}

	private void UpdateTTL(float dt)
	{
		foreach (KeyValuePair<Vector2i, ZoneData> zone in m_zones)
		{
			zone.Value.m_ttl += dt;
		}
		foreach (KeyValuePair<Vector2i, ZoneData> zone2 in m_zones)
		{
			if (zone2.Value.m_ttl > m_zoneTTL && !ZNetScene.instance.HaveInstanceInSector(zone2.Key))
			{
				UnityEngine.Object.Destroy(zone2.Value.m_root);
				m_zones.Remove(zone2.Key);
				break;
			}
		}
	}

	public bool FindClosestLocation(string name, Vector3 point, out LocationInstance closest)
	{
		float num = 999999f;
		closest = default(LocationInstance);
		bool result = false;
		foreach (LocationInstance value in m_locationInstances.Values)
		{
			float num2 = Vector3.Distance(value.m_position, point);
			if (value.m_location.m_prefab.Name == name && num2 < num)
			{
				num = num2;
				closest = value;
				result = true;
			}
		}
		return result;
	}

	public bool FindLocations(string name, ref List<LocationInstance> locations)
	{
		locations.Clear();
		foreach (LocationInstance value in m_locationInstances.Values)
		{
			if (value.m_location.m_prefab.Name == name)
			{
				locations.Add(value);
			}
		}
		return locations.Count > 0;
	}

	public static Vector2i GetZone(Vector3 point)
	{
		int x = Utils.FloorToInt((float)(((double)point.x + 32.0) / 64.0));
		int y = Utils.FloorToInt((float)(((double)point.z + 32.0) / 64.0));
		return new Vector2i(x, y);
	}

	public static Vector3 GetZonePos(Vector2i id)
	{
		return new Vector3((float)((double)id.x * 64.0), 0f, (float)((double)id.y * 64.0));
	}

	private void SetZoneGenerated(Vector2i zoneID)
	{
		m_generatedZones.Add(zoneID);
	}

	private bool IsZoneGenerated(Vector2i zoneID)
	{
		return m_generatedZones.Contains(zoneID);
	}

	public bool IsZoneReadyForType(Vector2i zoneID, ZDO.ObjectType objectType)
	{
		if (m_loadingObjectsInZones.Count <= 0)
		{
			return true;
		}
		if (!m_loadingObjectsInZones.ContainsKey(zoneID))
		{
			return true;
		}
		foreach (ZDO item in m_loadingObjectsInZones[zoneID])
		{
			if ((int)objectType < (int)item.Type)
			{
				return false;
			}
		}
		return true;
	}

	public void SetLoadingInZone(ZDO zdo)
	{
		Vector2i sector = zdo.GetSector();
		if (m_loadingObjectsInZones.ContainsKey(sector))
		{
			m_loadingObjectsInZones[sector].Add(zdo);
			return;
		}
		List<ZDO> list = new List<ZDO>();
		list.Add(zdo);
		m_loadingObjectsInZones.Add(sector, list);
	}

	public void UnsetLoadingInZone(ZDO zdo)
	{
		Vector2i sector = zdo.GetSector();
		m_loadingObjectsInZones[sector].Remove(zdo);
		if (m_loadingObjectsInZones[sector].Count <= 0)
		{
			m_loadingObjectsInZones.Remove(sector);
		}
	}

	public bool SkipSaving()
	{
		if (!m_error)
		{
			return m_didZoneTest;
		}
		return true;
	}

	public float TimeSinceStart()
	{
		return m_lastFixedTime - m_startTime;
	}

	public void ResetGlobalKeys()
	{
		ClearGlobalKeys();
		SetStartingGlobalKeys(send: false);
		SendGlobalKeys(ZRoutedRpc.Everybody);
	}

	public void ResetWorldKeys()
	{
		for (int i = 0; i < 32; i++)
		{
			GlobalKeys globalKeys = (GlobalKeys)i;
			RemoveGlobalKey(globalKeys.ToString());
		}
	}

	public void SetStartingGlobalKeys(bool send = true)
	{
		for (int i = 0; i < 32; i++)
		{
			GlobalKeys globalKeys = (GlobalKeys)i;
			GlobalKeyRemove(globalKeys.ToString(), canSaveToServerOptionKeys: false);
		}
		string text = null;
		m_tempKeys.Clear();
		m_tempKeys.AddRange(ZNet.World.m_startingGlobalKeys);
		foreach (string tempKey in m_tempKeys)
		{
			GetKeyValue(tempKey.ToLower(), out var value, out var gk);
			if (gk == GlobalKeys.Preset)
			{
				text = value;
			}
			GlobalKeyAdd(tempKey, canSaveToServerOptionKeys: false);
		}
		if (text != null)
		{
			ServerOptionsGUI.m_instance.SetPreset(ZNet.World, text);
		}
		if (send)
		{
			SendGlobalKeys(ZRoutedRpc.Everybody);
		}
	}

	public void SetGlobalKey(GlobalKeys key, float value)
	{
		SetGlobalKey($"{key} {value.ToString(CultureInfo.InvariantCulture)}");
	}

	public void SetGlobalKey(GlobalKeys key)
	{
		SetGlobalKey(key.ToString());
	}

	public void SetGlobalKey(string name)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC("SetGlobalKey", name);
	}

	public bool GetGlobalKey(GlobalKeys key)
	{
		return m_globalKeysEnums.Contains(key);
	}

	public bool GetGlobalKey(GlobalKeys key, out string value)
	{
		return m_globalKeysValues.TryGetValue(key.ToString().ToLower(), out value);
	}

	public bool GetGlobalKey(GlobalKeys key, out float value)
	{
		if (m_globalKeysValues.TryGetValue(key.ToString().ToLower(), out var value2) && float.TryParse(value2, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
		{
			return true;
		}
		value = 0f;
		return false;
	}

	public bool GetGlobalKey(string name)
	{
		string value;
		return GetGlobalKey(name, out value);
	}

	public bool GetGlobalKey(string name, out string value)
	{
		return m_globalKeysValues.TryGetValue(name.ToLower(), out value);
	}

	public bool GetGlobalKeyExact(string fullLine)
	{
		return m_globalKeys.Contains(fullLine);
	}

	public bool CheckKey(string key, GameKeyType type = GameKeyType.Global, bool trueWhenKeySet = true)
	{
		switch (type)
		{
		case GameKeyType.Global:
			return instance.GetGlobalKey(key) == trueWhenKeySet;
		case GameKeyType.Player:
			if ((bool)Player.m_localPlayer)
			{
				return Player.m_localPlayer.HaveUniqueKey(key) == trueWhenKeySet;
			}
			return false;
		default:
			ZLog.LogError("Unknown GameKeyType type");
			return false;
		}
	}

	private void RPC_SetGlobalKey(long sender, string name)
	{
		if (!m_globalKeys.Contains(name))
		{
			GlobalKeyAdd(name);
			SendGlobalKeys(ZRoutedRpc.Everybody);
		}
	}

	public void RemoveGlobalKey(GlobalKeys key)
	{
		RemoveGlobalKey(key.ToString());
	}

	public void RemoveGlobalKey(string name)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC("RemoveGlobalKey", name);
	}

	private void RPC_RemoveGlobalKey(long sender, string name)
	{
		if (GlobalKeyRemove(name))
		{
			SendGlobalKeys(ZRoutedRpc.Everybody);
		}
	}

	public List<string> GetGlobalKeys()
	{
		return new List<string>(m_globalKeys);
	}

	public Dictionary<Vector2i, LocationInstance>.ValueCollection GetLocationList()
	{
		return m_locationInstances.Values;
	}
}
