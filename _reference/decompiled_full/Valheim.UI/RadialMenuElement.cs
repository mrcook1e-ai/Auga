using System;
using UnityEngine;
using UnityEngine.UI;

namespace Valheim.UI;

public class RadialMenuElement : MonoBehaviour
{
	protected const string c_HOVER_SUFFIX = "_hov";

	protected const string c_NUDGE_SUFFIX = "_nug";

	[SerializeField]
	protected Image m_icon;

	[SerializeField]
	protected Image m_background;

	[SerializeField]
	protected CanvasGroup m_canvasGroup;

	private Material m_backgroundMaterial;

	private RectTransform m_rectTransform;

	public Material BackgroundMaterial
	{
		get
		{
			if ((bool)m_backgroundMaterial)
			{
				return m_backgroundMaterial;
			}
			m_backgroundMaterial = new Material(Background.material);
			Background.material = m_backgroundMaterial;
			return m_backgroundMaterial;
		}
	}

	public RectTransform ElementTransform
	{
		get
		{
			if ((bool)m_rectTransform)
			{
				return m_rectTransform;
			}
			m_rectTransform = base.transform as RectTransform;
			RectTransform rectTransform = m_rectTransform;
			RectTransform rectTransform2 = m_rectTransform;
			Vector2 vector = (m_rectTransform.pivot = new Vector2(0.5f, 0.5f));
			Vector2 anchorMin = (rectTransform2.anchorMax = vector);
			rectTransform.anchorMin = anchorMin;
			m_rectTransform.GetChild(1).eulerAngles = Vector3.zero;
			return m_rectTransform;
		}
	}

	public Vector3 LocalPosition
	{
		get
		{
			return ElementTransform.localPosition;
		}
		set
		{
			ElementTransform.localPosition = value;
		}
	}

	public Image Icon => m_icon;

	public Image Background => m_background;

	public string Name { get; protected set; }

	public string SubTitle { get; protected set; }

	public string Description { get; protected set; }

	public string ID => base.gameObject.GetInstanceID().ToString();

	public Func<RadialBase, RadialArray<RadialMenuElement>, bool> AdvancedCloseOnInteract { get; set; }

	public Func<bool> CloseOnInteract { get; set; } = () => false;

	public Func<bool> Interact { get; set; }

	public Func<bool> SecondaryInteract { get; set; }

	public Func<RadialBase, int, bool> TryOpenSubRadial { get; set; }

	public float Scale
	{
		get
		{
			return ElementTransform.localScale.x;
		}
		set
		{
			ElementTransform.localScale = Vector3.one * value;
		}
	}

	public float Alpha
	{
		get
		{
			return m_canvasGroup.alpha;
		}
		set
		{
			m_canvasGroup.alpha = value;
			UnselectedColorAlpha = value;
			ActivatedColorAlpha = value;
			QueuedColorAlpha = value;
		}
	}

	public float UnselectedColorAlpha
	{
		get
		{
			return BackgroundMaterial.GetColor("_UnselectedColor").a;
		}
		set
		{
			Color color = BackgroundMaterial.GetColor("_UnselectedColor");
			float a = Mathf.Clamp(value, 0f, 0.8f);
			color.a = a;
			BackgroundMaterial.SetColor("_UnselectedColor", color);
		}
	}

	public float ActivatedColorAlpha
	{
		get
		{
			return BackgroundMaterial.GetColor("_ActivatedColor").a;
		}
		set
		{
			Color color = BackgroundMaterial.GetColor("_ActivatedColor");
			float a = Mathf.Clamp(value, 0f, 0.8f);
			color.a = a;
			BackgroundMaterial.SetColor("_ActivatedColor", color);
		}
	}

	public float QueuedColorAlpha
	{
		get
		{
			return BackgroundMaterial.GetColor("_QueuedColor").a;
		}
		set
		{
			Color color = BackgroundMaterial.GetColor("_QueuedColor");
			float a = Mathf.Clamp(value, 0f, 0.8f);
			color.a = a;
			BackgroundMaterial.SetColor("_QueuedColor", color);
		}
	}

	public bool Selected
	{
		get
		{
			return BackgroundMaterial.GetInt("_Selected") == 1;
		}
		set
		{
			BackgroundMaterial.SetInt("_Selected", value ? 1 : 0);
		}
	}

	public float Activated
	{
		get
		{
			return BackgroundMaterial.GetFloat("_Activated");
		}
		set
		{
			BackgroundMaterial.SetFloat("_Activated", value);
		}
	}

	public bool Queued
	{
		get
		{
			return BackgroundMaterial.GetInt("_Queued") == 1;
		}
		set
		{
			BackgroundMaterial.SetInt("_Queued", value ? 1 : 0);
		}
	}

	public float Hovering
	{
		get
		{
			return BackgroundMaterial.GetFloat("_Hovering");
		}
		set
		{
			m_backgroundMaterial.SetFloat("_Hovering", value);
		}
	}

	internal void OpenAnimation(RadialMenuAnimationManager manager, string id, float duration, float distance, float startOffset, EasingType alphaEasingType, EasingType positionEasingType)
	{
		LocalPosition = LocalPosition.normalized * (distance + startOffset);
		manager.StartTween(() => LocalPosition, delegate(Vector3 val)
		{
			LocalPosition = val;
		}, id, LocalPosition.normalized * distance, duration, positionEasingType);
		float alpha = Alpha;
		Alpha = 0f;
		manager.StartTween(() => Alpha, delegate(float val)
		{
			Alpha = val;
		}, id, alpha, duration + 0.1f, alphaEasingType);
	}

	internal void CloseAnimation(RadialMenuAnimationManager manager, string id, float duration, float distance, float startOffset, EasingType alphaEasingType, EasingType positionEasingType)
	{
		manager.StartTween(() => LocalPosition, delegate(Vector3 val)
		{
			LocalPosition = val;
		}, id, LocalPosition.normalized * (distance + startOffset), duration + 0.1f, positionEasingType);
		manager.StartTween(() => Alpha, delegate(float val)
		{
			Alpha = val;
		}, id, 0f, duration + 0.1f, alphaEasingType);
	}

	internal void StartHoverSelect(RadialMenuAnimationManager manager, float duration, EasingType easingType, Action onEnd)
	{
		manager.StartUniqueTween(() => Hovering, delegate(float val)
		{
			Hovering = val;
		}, ID + "_hov", 1f, (Hovering > 0f) ? (duration - duration * Hovering) : duration, easingType, onEnd);
	}

	internal void ResetHoverSelect(RadialMenuAnimationManager manager, float duration, EasingType easingType)
	{
		manager.StartUniqueTween(() => Hovering, delegate(float val)
		{
			Hovering = val;
		}, ID + "_hov", 0f, Hovering * duration, easingType);
	}

	internal void StartNudge(RadialMenuAnimationManager manager, float distance, float duration, EasingType easingType)
	{
		manager.StartUniqueTween(() => LocalPosition, delegate(Vector3 val)
		{
			LocalPosition = val;
		}, ID + "_nug", LocalPosition.normalized * distance, duration, easingType);
	}

	internal void ResetNudge(RadialMenuAnimationManager manager, float distance, float duration, EasingType easingType)
	{
		manager.StartUniqueTween(() => LocalPosition, delegate(Vector3 val)
		{
			LocalPosition = val;
		}, ID + "_nug", LocalPosition.normalized * distance, duration, easingType);
	}

	internal void EndNudge(RadialMenuAnimationManager manager)
	{
		manager.EndTweens(ID + "_nug");
	}

	public void SetSegment(int segments)
	{
		SetSegment(UIMath.DirectionToAngleDegrees(LocalPosition.normalized) / 360f, segments);
	}

	public void SetSegment(int index, int segments)
	{
		BackgroundMaterial.SetInt("_Segments", segments);
		BackgroundMaterial.SetFloat("_Offset", (float)index / (float)segments);
	}

	public void SetSegment(float offset, int segments)
	{
		offset = Math.Clamp(offset, 0f, 1f);
		BackgroundMaterial.SetInt("_Segments", segments);
		BackgroundMaterial.SetFloat("_Offset", offset);
	}
}
