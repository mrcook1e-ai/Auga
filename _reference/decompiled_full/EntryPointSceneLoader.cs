using System.Collections;
using SoftReferenceableAssets.SceneManagement;
using UnityEngine;

public class EntryPointSceneLoader : MonoBehaviour
{
	[SerializeField]
	private SceneReference m_scene;

	private void Start()
	{
		StartCoroutine(LoadSceneAndWaitForPrefs());
	}

	private IEnumerator LoadSceneAndWaitForPrefs()
	{
		ZLog.Log("Loading first scene!");
		ILoadSceneAsyncOperation op = SceneManager.LoadSceneAsync(m_scene);
		op.AllowSceneActivation = false;
		while (!PlatformInitializer.PreferencesInitialized)
		{
			yield return null;
		}
		ZLog.Log("Preferences initialized! Activating first scene!");
		op.AllowSceneActivation = true;
	}
}
