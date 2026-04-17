using TMPro;
using UnityEngine;

namespace Valheim.SettingsGui;

public class ResolutionSwitchDialogTimedRemoval : MonoBehaviour
{
	[SerializeField]
	private GraphicsSettings m_graphicsSettings;

	private float m_resCountdownTimer = 1f;

	[SerializeField]
	private TMP_Text m_resSwitchCountdown;

	public float ResCountdownTimer
	{
		get
		{
			return m_resCountdownTimer;
		}
		set
		{
			m_resCountdownTimer = value;
		}
	}

	private void Update()
	{
		m_resCountdownTimer -= Time.unscaledDeltaTime;
		m_resSwitchCountdown.text = Mathf.CeilToInt(m_resCountdownTimer).ToString();
		if (m_resCountdownTimer <= 0f || ZInput.GetButtonDown("JoyBack") || ZInput.GetButtonDown("JoyButtonB") || ZInput.GetKeyDown(KeyCode.Escape))
		{
			m_graphicsSettings.RevertMode();
			base.gameObject.SetActive(value: false);
		}
	}
}
