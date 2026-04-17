using SoftReferenceableAssets;
using UnityEngine;

public class SoftReferencePrefabSpawner : MonoBehaviour
{
	[SerializeField]
	private SoftReference<GameObject> m_prefab;

	[SerializeField]
	private bool m_spawnAsynchronously;

	private bool m_currentlyLoading;

	private void Awake()
	{
		if (m_spawnAsynchronously)
		{
			m_currentlyLoading = true;
			m_prefab.LoadAsync(delegate(AssetID assetID, LoadResult result)
			{
				m_currentlyLoading = false;
				if (result == LoadResult.Succeeded)
				{
					SpawnPrefab();
				}
				m_prefab.Release();
				Object.Destroy(base.gameObject);
			});
		}
		else
		{
			SpawnPrefab();
			Object.Destroy(base.gameObject);
		}
	}

	private void OnDestroy()
	{
		if (m_currentlyLoading)
		{
			m_prefab.Load();
			m_prefab.Release();
		}
	}

	private void SpawnPrefab()
	{
		SoftReferenceableAssets.Utils.Instantiate(m_prefab, base.transform.parent).name = m_prefab.Name;
	}
}
