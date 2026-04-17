using System.Collections;
using SoftReferenceableAssets.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
	public SceneReference m_scene;

	private bool _showLogos = true;

	private bool _showHealthWarning;

	private bool _showSaveNotification;

	private bool _skipEnabled;

	private bool _logosSkippable;

	private bool _skipAllAtOnce;

	private bool _skipped;

	private ILoadSceneAsyncOperation _sceneLoadOperation;

	private ThreadPriority _currentLoadingBudgetRequest;

	private float _fakeProgress;

	[SerializeField]
	private LoadingIndicator loadingIndicator;

	[SerializeField]
	private GameObject gameLogo;

	[SerializeField]
	private GameObject coffeeStainLogo;

	[SerializeField]
	private GameObject ironGateLogo;

	[SerializeField]
	private CanvasGroup savingNotification;

	[SerializeField]
	private CanvasGroup healthWarning;

	public AnimationCurve alphaCurve;

	public AnimationCurve scalingCurve;

	private const float LogoDisplayTime = 2f;

	private const float SaveNotificationDisplayTime = 5f;

	private const float HealthWarningDisplayTime = 5f;

	private const float FadeInOutTime = 0.5f;

	private void Awake()
	{
		_showLogos = true;
		_showHealthWarning = false;
		_showSaveNotification = false;
		healthWarning.gameObject.SetActive(value: false);
		savingNotification.gameObject.SetActive(value: false);
		coffeeStainLogo.SetActive(value: false);
		ironGateLogo.SetActive(value: false);
		gameLogo.SetActive(value: false);
		ZInput.Initialize();
	}

	private void Start()
	{
		StartLoading();
	}

	private void Update()
	{
		ZInput.Update(Time.unscaledDeltaTime);
		if (_skipEnabled && (ZInput.GetButtonDown("JoyButtonA") || ZInput.GetMouseButtonDown(0)))
		{
			_skipped = true;
		}
		if (!loadingIndicator.IsVisible)
		{
			return;
		}
		float num = ((_sceneLoadOperation == null) ? 0f : _sceneLoadOperation.Progress);
		if (num <= 0.25f)
		{
			float num2 = num / 0.25f * 0.05f;
			if (_fakeProgress < num2)
			{
				_fakeProgress = num2;
			}
			else if (num == 0.25f)
			{
				_fakeProgress = Mathf.Min(num, _fakeProgress + Time.deltaTime * 0.01f);
			}
		}
		else
		{
			_fakeProgress = num;
		}
		loadingIndicator.SetProgress(_fakeProgress);
	}

	private void OnDestroy()
	{
		if (_currentLoadingBudgetRequest != ThreadPriority.Low)
		{
			BackgroundLoadingBudgetController.ReleaseLoadingBudgetRequest(_currentLoadingBudgetRequest);
		}
	}

	private void StartLoading()
	{
		StartCoroutine(LoadSceneAsync());
	}

	private IEnumerator LoadSceneAsync()
	{
		SceneReference scene = m_scene;
		ZLog.Log("Starting to load scene:" + scene.ToString());
		_sceneLoadOperation = SceneManager.LoadSceneAsync(m_scene);
		_currentLoadingBudgetRequest = BackgroundLoadingBudgetController.RequestLoadingBudget(ThreadPriority.Normal);
		_sceneLoadOperation.AllowSceneActivation = false;
		_ = Localization.instance;
		PlatformInitializer.AllowSaveDataInitialization = false;
		if (PlatformInitializer.StartedSaveDataInitialization)
		{
			while (!PlatformInitializer.SaveDataInitialized)
			{
				yield return null;
			}
		}
		if (_showLogos)
		{
			Image componentInChildren = coffeeStainLogo.GetComponentInChildren<Image>();
			Image igImage = ironGateLogo.GetComponentInChildren<Image>();
			if (!_skipAllAtOnce)
			{
				_skipped = false;
			}
			if (!_logosSkippable || !_skipped)
			{
				yield return FadeLogo(coffeeStainLogo, componentInChildren, 2f, alphaCurve, scalingCurve);
			}
			coffeeStainLogo.SetActive(value: false);
			if (!_skipAllAtOnce)
			{
				_skipped = false;
			}
			if (!_logosSkippable || !_skipped)
			{
				yield return FadeLogo(ironGateLogo, igImage, 2f, alphaCurve, scalingCurve);
			}
			ironGateLogo.SetActive(value: false);
		}
		if (_showSaveNotification)
		{
			if (!_skipAllAtOnce)
			{
				_skipped = false;
			}
			if (!_skipped)
			{
				yield return ShowSaveNotification();
			}
		}
		if (_showHealthWarning)
		{
			if (!_skipAllAtOnce)
			{
				_skipped = false;
			}
			if (!_skipped)
			{
				yield return ShowHealthWarning();
			}
		}
		gameLogo.SetActive(value: true);
		_currentLoadingBudgetRequest = BackgroundLoadingBudgetController.UpdateLoadingBudgetRequest(_currentLoadingBudgetRequest, ThreadPriority.High);
		loadingIndicator.SetShow(show: true);
		PlatformInitializer.AllowSaveDataInitialization = true;
		while (!PlatformInitializer.SaveDataInitialized)
		{
			yield return null;
		}
		PlatformInitializer.InputDeviceRequired = true;
		if ((object)StartupMessages.Instance != null)
		{
			StartupMessages.Instance.DisplayStartupMessages();
		}
		while (!_sceneLoadOperation.IsLoadedButNotActivated)
		{
			yield return null;
		}
		loadingIndicator.SetShow(show: false);
		while (loadingIndicator.IsVisible)
		{
			yield return null;
		}
		yield return null;
		while (PlatformInitializer.WaitingForInputDevice)
		{
			yield return null;
		}
		if ((object)StartupMessages.Instance != null)
		{
			while (StartupMessages.Instance.StartupMessageDisplayed)
			{
				yield return null;
			}
		}
		_sceneLoadOperation.AllowSceneActivation = true;
	}

	private IEnumerator ShowSaveNotification()
	{
		if (!_skipped)
		{
			savingNotification.alpha = 0f;
			savingNotification.gameObject.SetActive(value: true);
			yield return null;
			LayoutRebuilder.ForceRebuildLayoutImmediate(healthWarning.transform as RectTransform);
			float fadeTimer = 0f;
			while (true)
			{
				if (savingNotification.alpha < 1f)
				{
					float t = 1f - (0.5f - fadeTimer) / 0.5f;
					float alpha = Mathf.SmoothStep(0f, 1f, t);
					savingNotification.alpha = alpha;
					fadeTimer += Time.unscaledDeltaTime;
					if (_skipped)
					{
						break;
					}
					yield return null;
					continue;
				}
				fadeTimer = 0f;
				while (true)
				{
					if (fadeTimer < 5f)
					{
						fadeTimer += Time.unscaledDeltaTime;
						if (_skipped)
						{
							break;
						}
						yield return null;
						continue;
					}
					fadeTimer = 0f;
					while (savingNotification.alpha > 0f)
					{
						float t = 1f - (0.5f - fadeTimer) / 0.5f;
						float alpha = Mathf.SmoothStep(savingNotification.alpha, 0f, t);
						savingNotification.alpha = alpha;
						fadeTimer += Time.unscaledDeltaTime;
						if (_skipped)
						{
							break;
						}
						yield return null;
					}
					break;
				}
				break;
			}
		}
		savingNotification.gameObject.SetActive(value: false);
	}

	private IEnumerator ShowHealthWarning()
	{
		if (!_skipped)
		{
			healthWarning.alpha = 0f;
			healthWarning.gameObject.SetActive(value: true);
			yield return null;
			LayoutRebuilder.ForceRebuildLayoutImmediate(healthWarning.transform as RectTransform);
			float fadeTimer = 0f;
			while (true)
			{
				if (healthWarning.alpha < 1f)
				{
					float t = 1f - (0.5f - fadeTimer) / 0.5f;
					float alpha = Mathf.SmoothStep(0f, 1f, t);
					healthWarning.alpha = alpha;
					fadeTimer += Time.unscaledDeltaTime;
					if (_skipped)
					{
						break;
					}
					yield return null;
					continue;
				}
				fadeTimer = 0f;
				while (true)
				{
					if (fadeTimer < 5f)
					{
						fadeTimer += Time.unscaledDeltaTime;
						if (_skipped)
						{
							break;
						}
						yield return null;
						continue;
					}
					fadeTimer = 0f;
					while (healthWarning.alpha > 0f)
					{
						float t = 1f - (0.5f - fadeTimer) / 0.5f;
						float alpha = Mathf.SmoothStep(healthWarning.alpha, 0f, t);
						healthWarning.alpha = alpha;
						fadeTimer += Time.unscaledDeltaTime;
						if (_skipped)
						{
							break;
						}
						yield return null;
					}
					break;
				}
				break;
			}
		}
		healthWarning.gameObject.SetActive(value: false);
	}

	private IEnumerator FadeLogo(GameObject parentGameObject, Image logo, float duration, AnimationCurve alpha, AnimationCurve scale)
	{
		Color spriteColor = logo.color;
		float timer = 0f;
		parentGameObject.SetActive(value: true);
		while (timer < duration && (!_logosSkippable || !_skipped))
		{
			float a = alpha.Evaluate(timer);
			spriteColor.a = a;
			logo.color = spriteColor;
			a = scale.Evaluate(timer);
			logo.transform.localScale = Vector3.one * a;
			timer += Time.deltaTime;
			yield return null;
		}
	}
}
