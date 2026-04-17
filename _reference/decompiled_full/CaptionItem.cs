using System;
using TMPro;
using UnityEngine;

public class CaptionItem : MonoBehaviour
{
	public string m_captionText;

	public ClosedCaptions.CaptionType m_type;

	private TextMeshProUGUI m_text;

	private float m_timer;

	private bool m_dying;

	public bool Killed => m_dying;

	public float TimeSinceSpawn { get; private set; }

	public event Action<CaptionItem> OnDestroyingCaption = delegate
	{
	};

	public void Setup()
	{
		m_text = GetComponent<TextMeshProUGUI>();
		m_text.color = ClosedCaptions.Instance.GetCaptionColor(m_type);
		m_text.text = m_captionText ?? "";
		MonoBehaviour.print(Localization.instance);
		Refresh();
	}

	private void OnDestroy()
	{
		this.OnDestroyingCaption?.Invoke(this);
	}

	public void CustomUpdate(float dt)
	{
		m_timer -= dt;
		TimeSinceSpawn += dt;
		if (m_timer <= 0f)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		float a = Mathf.Clamp01(TimeSinceSpawn * 2f);
		float b = Mathf.Clamp01(m_timer * 4f);
		float num = Mathf.Min(a, b);
		Vector3 localScale = base.transform.localScale;
		localScale.y = num;
		localScale.x = 1f;
		localScale.z = 1f;
		base.transform.localScale = localScale;
		m_text.alpha = num;
	}

	public void Refresh()
	{
		if (!m_dying)
		{
			m_timer = ClosedCaptions.Instance.m_captionDuration;
		}
	}

	public void Kill()
	{
		m_dying = true;
		m_timer = Mathf.Min(m_timer, 0.5f);
	}

	public int GetImportance()
	{
		return (int)m_type;
	}
}
