using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ClosedCaptions : MonoBehaviour
{
	public enum CaptionType
	{
		[InspectorName("Misc.")]
		Default,
		[InspectorName("Wildlife, Enemy Idles")]
		Wildlife,
		Enemy,
		Boss
	}

	private static ClosedCaptions m_instance;

	public float m_captionDuration = 5f;

	public int m_maxCaptionLines = 4;

	public GameObject m_captionPrefab;

	[Header("Directional Indicators")]
	public float m_maxFuzziness = 15f;

	public float m_maxFuzzinessDistance = 50f;

	public AnimationCurve m_fuzzCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

	public GameObject m_indicatorContainer;

	public GameObject m_directionalIndicatorPrefab;

	[Header("Type Colors")]
	[ColorUsage(false)]
	public Color m_defaultColor = Color.white;

	[ColorUsage(false)]
	public Color m_wildlifeColor = new Color(0.78f, 0.43f, 0.65f);

	[ColorUsage(false)]
	public Color m_enemyColor = new Color(0.8f, 0.24f, 0.04f);

	[ColorUsage(false)]
	public Color m_bossColor = new Color(0.34f, 0.24f, 0.62f);

	private List<CaptionItem> m_captionItems = new List<CaptionItem>();

	private List<CaptionItem> m_lowestImportance = new List<CaptionItem>();

	private Image m_image;

	private float m_bgAlpha;

	public static ClosedCaptions Instance => m_instance;

	public static bool Valid { get; private set; }

	private void Awake()
	{
		if (m_instance == null)
		{
			m_instance = this;
			Valid = true;
			foreach (Transform item in base.transform)
			{
				Object.Destroy(item.gameObject);
			}
			m_image = GetComponent<Image>();
			m_bgAlpha = m_image.color.a;
			Color color = m_image.color;
			color.a = 0f;
			m_image.color = color;
		}
		else
		{
			Object.DestroyImmediate(this);
		}
	}

	private void Update()
	{
		foreach (CaptionItem captionItem in m_captionItems)
		{
			captionItem.CustomUpdate(Time.deltaTime);
		}
		Color color = m_image.color;
		float b = ((m_captionItems.Count == 0) ? 0f : m_bgAlpha);
		color.a = Mathf.Lerp(color.a, b, Time.deltaTime / 0.25f);
		m_image.color = color;
	}

	public void RegisterCaption(ZSFX sfx, CaptionType type = CaptionType.Default)
	{
	}

	private string GetCaptionText(ZSFX sfx)
	{
		string text = Localization.instance.Localize(sfx.m_closedCaptionToken);
		if (sfx.m_secondaryCaptionToken.Length > 0)
		{
			text = text + " " + Localization.instance.Localize(sfx.m_secondaryCaptionToken);
		}
		return text;
	}

	private void RemoveCaption(CaptionItem cc)
	{
		m_captionItems.Remove(cc);
	}

	public Color GetCaptionColor(CaptionType type)
	{
		return type switch
		{
			CaptionType.Enemy => Instance.m_enemyColor, 
			CaptionType.Wildlife => Instance.m_wildlifeColor, 
			CaptionType.Boss => Instance.m_bossColor, 
			_ => Instance.m_defaultColor, 
		};
	}
}
