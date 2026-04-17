using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class LoadingIndicator : MonoBehaviour
{
	[Tooltip("Whether to initially show the loading indicator. If not, visibility has to be set through script.")]
	[SerializeField]
	[FormerlySerializedAs("m_visibleInitially")]
	private bool m_showInitially;

	[Tooltip("Whether the progress indicator part of the loading indicator is shown.")]
	[SerializeField]
	private bool m_showProgressIndicator = true;

	[Tooltip("The time it takes for the progress indicator and its components to fade in and fade out when they are shown and hidden.")]
	[SerializeField]
	[FormerlySerializedAs("m_visibilityFadeTime")]
	private float m_showFadeTime = 0.2f;

	[Tooltip("If the delta time value is greater than this, this value is used instead to animate the progress indicator.")]
	[SerializeField]
	private float m_maxDeltaTime = 1f / 30f;

	[SerializeField]
	private Image m_spinner;

	[SerializeField]
	private Image m_progressIndicator;

	[SerializeField]
	private Image m_background;

	[SerializeField]
	private TMP_Text m_text;

	private bool m_show;

	private float m_progress;

	private float m_spinnerVisibility;

	private float m_progressVisibility;

	private float m_progressSmoothVelocity;

	private Color m_progressIndicatorOriginalColor;

	private Color m_spinnerOriginalColor;

	private Color m_backgroundOriginalColor;

	private Color m_textOriginalColor;

	public bool IsVisible
	{
		get
		{
			if (!(m_spinnerVisibility > 0f))
			{
				return m_progressVisibility > 0f;
			}
			return true;
		}
	}

	private void Awake()
	{
		m_show = m_showInitially;
		m_spinnerVisibility = (m_show ? 1f : 0f);
		m_progressVisibility = ((m_show && m_showProgressIndicator) ? 1f : 0f);
		m_text.text = "";
		m_progressIndicatorOriginalColor = m_progressIndicator.color;
		m_spinnerOriginalColor = m_spinner.color;
		m_backgroundOriginalColor = m_background.color;
		m_textOriginalColor = m_text.color;
		UpdateGUIVisibility();
	}

	private void LateUpdate()
	{
		float num = Mathf.Min(Time.deltaTime, m_maxDeltaTime);
		float num2 = (m_show ? 1f : 0f);
		float num3 = ((m_show && m_showProgressIndicator) ? 1f : 0f);
		bool flag = false;
		if (m_spinnerVisibility != num2)
		{
			if (m_showFadeTime <= 0f)
			{
				m_spinnerVisibility = num2;
			}
			else
			{
				m_spinnerVisibility = Mathf.MoveTowards(m_spinnerVisibility, num2, num / m_showFadeTime);
			}
			flag = true;
		}
		if (m_progressVisibility != num3)
		{
			if (m_showFadeTime <= 0f)
			{
				m_progressVisibility = num3;
			}
			else
			{
				m_progressVisibility = Mathf.MoveTowards(m_progressVisibility, num3, num / m_showFadeTime);
			}
			flag = true;
		}
		if (flag)
		{
			UpdateGUIVisibility();
		}
		float target = ((m_progress < 1f) ? m_progress : 1.05f);
		m_progressIndicator.fillAmount = Mathf.Min(1f, Mathf.SmoothDamp(m_progressIndicator.fillAmount, target, ref m_progressSmoothVelocity, 0.2f, float.PositiveInfinity, num));
	}

	private void UpdateGUIVisibility()
	{
		Color spinnerOriginalColor = m_spinnerOriginalColor;
		spinnerOriginalColor.a *= m_spinnerVisibility;
		m_spinner.color = spinnerOriginalColor;
		spinnerOriginalColor = m_progressIndicatorOriginalColor;
		spinnerOriginalColor.a *= m_progressVisibility;
		m_progressIndicator.color = spinnerOriginalColor;
		spinnerOriginalColor = m_backgroundOriginalColor;
		spinnerOriginalColor.a *= m_progressVisibility;
		m_background.color = spinnerOriginalColor;
		spinnerOriginalColor = m_textOriginalColor;
		spinnerOriginalColor.a *= m_progressVisibility;
		m_text.color = spinnerOriginalColor;
	}

	public void SetShow(bool show)
	{
		m_show = show;
	}

	public void SetShowProgress(bool show)
	{
		m_showProgressIndicator = show;
	}

	public void SetProgress(float progress)
	{
		m_progress = progress;
	}

	public void SetText(string progressText, bool localize = true)
	{
		if (progressText == null)
		{
			ZLog.LogError("Progress text was null!");
		}
		else
		{
			m_text.text = (localize ? Localization.instance.Localize(progressText) : progressText);
		}
	}
}
