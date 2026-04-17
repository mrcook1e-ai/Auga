using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Valheim.UI;

public class ElementInfo : MonoBehaviour
{
	[SerializeField]
	protected TextMeshProUGUI m_title;

	[SerializeField]
	protected Image m_icon;

	[SerializeField]
	protected GuiBar m_durabilityBar;

	[SerializeField]
	protected TextMeshProUGUI m_subTitle;

	[SerializeField]
	protected RadialInventoryInfo m_inventoryInfo;

	protected Image m_background;

	private Image m_durabilityBarBG;

	private RectTransform m_rectTransform;

	protected Material m_cutoutMaterial;

	protected Material m_iconMaterial;

	public Image BackgroundImage
	{
		get
		{
			if (m_background == null)
			{
				m_background = base.gameObject.GetComponent<Image>();
			}
			return m_background;
		}
	}

	public Image DurabilityBarBG
	{
		get
		{
			if (m_durabilityBarBG == null)
			{
				m_durabilityBarBG = m_durabilityBar.gameObject.GetComponent<Image>();
			}
			return m_durabilityBarBG;
		}
	}

	public RectTransform InfoTransform
	{
		get
		{
			if (m_rectTransform == null)
			{
				m_rectTransform = base.transform as RectTransform;
			}
			return m_rectTransform;
		}
	}

	public float Radius
	{
		get
		{
			return InfoTransform.sizeDelta.x;
		}
		set
		{
			InfoTransform.sizeDelta = Vector2.one * value;
		}
	}

	public float Alpha
	{
		get
		{
			return CutoutMaterial.GetColor("_Color").a;
		}
		set
		{
			BGAlpha = value;
			Color color = IconMaterial.color;
			color.a = value;
			IconMaterial.color = color;
			m_title.alpha = value;
			m_subTitle.alpha = value;
			color = m_durabilityBar.GetColor();
			color.a = value;
			m_durabilityBar.SetColor(color);
			color = DurabilityBarBG.color;
			color.a = Mathf.Max(value, 0.65f);
			DurabilityBarBG.color = color;
		}
	}

	public float BGAlpha
	{
		get
		{
			return CutoutMaterial.GetColor("_Color").a;
		}
		set
		{
			Color color = CutoutMaterial.GetColor("_Color");
			color.a = Mathf.Clamp(value, 0f, 0.8f);
			CutoutMaterial.SetColor("_Color", color);
		}
	}

	public Material CutoutMaterial
	{
		get
		{
			if ((bool)m_cutoutMaterial)
			{
				return m_cutoutMaterial;
			}
			m_cutoutMaterial = new Material(BackgroundImage.material);
			BackgroundImage.material = m_cutoutMaterial;
			return m_cutoutMaterial;
		}
	}

	public Material IconMaterial
	{
		get
		{
			if ((bool)m_iconMaterial)
			{
				return m_iconMaterial;
			}
			m_iconMaterial = new Material(m_icon.material);
			m_icon.material = m_iconMaterial;
			return m_iconMaterial;
		}
		set
		{
			m_iconMaterial = value;
		}
	}

	public virtual void Clear()
	{
		m_subTitle.gameObject.SetActive(value: true);
		m_durabilityBar.gameObject.SetActive(value: false);
		m_title.text = "";
		m_subTitle.text = "";
		m_icon.gameObject.SetActive(value: false);
	}

	public void UpdateDurabilityAndWeightInfo(RadialMenuElement element)
	{
		if (m_durabilityBar.gameObject.activeSelf && element is ItemElement itemElement)
		{
			m_durabilityBar.SetValue(itemElement.m_durability.GetSmoothValue());
			m_durabilityBar.SetColor(itemElement.m_durability.GetColor());
		}
		if (m_inventoryInfo.gameObject.activeSelf)
		{
			if (element is ThrowElement element2)
			{
				m_inventoryInfo.RefreshWeight(element2);
			}
			else
			{
				m_inventoryInfo.RefreshWeight();
			}
		}
	}

	internal virtual void Set(RadialMenuElement element, RadialMenuAnimationManager animator)
	{
		if (!element)
		{
			Clear();
			return;
		}
		m_subTitle.gameObject.SetActive(value: true);
		m_durabilityBar.gameObject.SetActive(value: false);
		bool flag = element is ItemElement || element is ThrowElement;
		m_title.text = (flag ? "" : element.Name);
		m_subTitle.text = element.SubTitle;
		m_icon.gameObject.SetActive(flag);
		if (flag)
		{
			m_icon.sprite = element.Icon.sprite;
		}
		if (!m_inventoryInfo.gameObject.activeSelf)
		{
			return;
		}
		m_inventoryInfo.RefreshInfo();
		if (!(element is ItemElement itemElement))
		{
			if (element is ThrowElement throwElement)
			{
				m_inventoryInfo.SetElement(throwElement, animator);
				SetDurabilityData(throwElement.m_data);
			}
			else
			{
				m_inventoryInfo.HideToolTip(animator);
			}
		}
		else
		{
			m_inventoryInfo.SetElement(itemElement, animator);
			SetDurabilityData(itemElement.m_data);
		}
		void SetDurabilityData(ItemDrop.ItemData data)
		{
			bool flag2 = data.m_shared.m_useDurability && data.m_durability < data.GetMaxDurability();
			m_durabilityBar.gameObject.SetActive(flag2);
			m_subTitle.gameObject.SetActive(!flag2);
			if (flag2)
			{
				bool flag3 = data.m_durability <= 0f;
				m_durabilityBar.SetValue(flag3 ? 1f : data.GetDurabilityPercentage());
				if (flag3)
				{
					m_durabilityBar.SetColor((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : new Color(0f, 0f, 0f, 0f));
				}
				else
				{
					m_durabilityBar.ResetColor();
				}
			}
		}
	}

	public virtual void Set(IRadialConfig config, bool updateAlpha = true)
	{
		Clear();
		if (config != null)
		{
			m_title.text = config.LocalizedName;
			m_inventoryInfo.gameObject.SetActive(config is ItemGroupConfig || config is ThrowGroupConfig);
			m_inventoryInfo.RefreshInfo();
			if (updateAlpha)
			{
				Alpha = 1f;
				m_inventoryInfo.SetAlpha(1f);
			}
		}
	}

	internal void OpenAnimation(RadialMenuAnimationManager manager, string id, float duration, float radius, float startOffset, EasingType alphaEasingType, EasingType positionEasingType)
	{
		Radius = radius + startOffset;
		Alpha = 0f;
		manager.StartTween(() => Alpha, delegate(float val)
		{
			Alpha = val;
		}, id, 0.8f, duration + 0.1f, alphaEasingType);
		manager.StartTween(m_inventoryInfo.SetAlpha, id, 0f, 1f, duration + 0.1f, alphaEasingType);
		manager.StartTween(() => Radius, delegate(float val)
		{
			Radius = val;
		}, id, radius, duration, positionEasingType);
	}

	internal void CloseAnimation(RadialMenuAnimationManager manager, string id, float duration, float radius, float startOffset, EasingType alphaEasingType, EasingType positionEasingType)
	{
		manager.StartTween(() => Alpha, delegate(float val)
		{
			Alpha = val;
		}, id, 0f, duration, alphaEasingType);
		manager.StartTween(m_inventoryInfo.SetAlpha, id, 1f, 0f, duration, alphaEasingType);
		manager.StartTween(() => Radius, delegate(float val)
		{
			Radius = val;
		}, id, radius + startOffset, duration + 0.1f, positionEasingType, delegate
		{
			Radius = radius;
		});
	}
}
