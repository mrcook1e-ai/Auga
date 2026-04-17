using UnityEngine;
using Valheim.UI;

public class RadialOverlapPreventer : MonoBehaviour
{
	[SerializeField]
	private RectTransform m_elementInfoElement;

	[SerializeField]
	private RectTransform m_tooltipElement;

	[SerializeField]
	private RectTransform m_parentElement;

	private void Start()
	{
		PreventOverlap();
	}

	private void OnEnable()
	{
		PreventOverlap();
	}

	private void PreventOverlap()
	{
		float num = m_parentElement.rect.width * 0.5f;
		float num2 = AnchoredPositionAt(m_tooltipElement.anchoredPosition, m_tooltipElement.anchorMin, new Vector2(0.5f, 0.5f)).x + m_tooltipElement.rect.width * (1f - m_tooltipElement.pivot.x);
		float num3 = ((RadialData.SO.RadialSize == RadialSizeSetting.Big) ? RadialData.SO.RadialBigScale : RadialData.SO.RadialSmallScale);
		float num4 = RadialData.SO.RadialReferenceWidth * num3;
		float num5 = num - num2;
		int num6 = ((RadialData.SO.RadialSize == RadialSizeSetting.SmallEdge) ? 1 : 0);
		float num7 = Mathf.Clamp(Vector2.right.x * (float)num6 * num, num2, num);
		Vector2 anchoredPosition = AnchoredPositionAt(m_elementInfoElement.anchoredPosition, m_elementInfoElement.anchorMin, new Vector2(0.5f, 0.5f)) + m_elementInfoElement.rect.size * (new Vector2(0.5f, 0.5f) - m_elementInfoElement.pivot);
		m_elementInfoElement.localScale = Vector3.one * num3;
		if (num4 <= num5)
		{
			float num8 = num4 * 0.5f;
			float min = num2 + num8;
			float max = num - num8;
			anchoredPosition.x = Mathf.Clamp(num7, min, max);
		}
		else
		{
			float a = (num7 - num2) * 2f / RadialData.SO.RadialReferenceWidth;
			float b = (num - num7) * 2f / RadialData.SO.RadialReferenceWidth;
			float b2 = Mathf.Min(a, b);
			float num9 = Mathf.Min(num3, b2);
			m_elementInfoElement.localScale = new Vector3(num9, num9, num9);
			float num10 = RadialData.SO.RadialReferenceWidth * num9 * 0.5f;
			anchoredPosition.x = Mathf.Clamp(num7, num2 + num10, num - num10);
		}
		m_elementInfoElement.anchoredPosition = anchoredPosition;
	}

	private Vector2 AnchoredPositionAt(Vector2 currentPosition, Vector2 currentAnchor, Vector2 targetAnchor)
	{
		Vector2 size = m_parentElement.rect.size;
		Vector2 vector = targetAnchor - currentAnchor;
		Vector2 vector2 = new Vector2(size.x * vector.x, size.y * vector.y);
		return currentPosition - vector2;
	}
}
