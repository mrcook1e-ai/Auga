using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Valheim.UI;

public class ThrowElementInfo : ElementInfo
{
	[SerializeField]
	protected Image m_itemIcon;

	[SerializeField]
	protected TextMeshProUGUI m_amountText;

	protected ItemDrop.ItemData m_data;

	protected string m_itemName;

	protected Material m_itemIconMaterial;

	public Sprite ItemIcon
	{
		get
		{
			return m_itemIcon.sprite;
		}
		set
		{
			m_itemIcon.sprite = value;
		}
	}

	public Material ItemIconMaterial
	{
		get
		{
			if (m_iconMaterial == null)
			{
				m_itemIconMaterial = new Material(m_itemIcon.material);
				m_itemIcon.material = m_itemIconMaterial;
			}
			return m_itemIconMaterial;
		}
		set
		{
			m_itemIconMaterial = value;
		}
	}

	public void Init(ItemDrop.ItemData item)
	{
		if (item != null)
		{
			ItemIcon = item.GetIcon();
			m_itemName = Localization.instance.Localize(item.m_shared.m_name);
			Color color = ItemIconMaterial.color;
			color.a = 1f;
			ItemIconMaterial.color = color;
			m_data = item;
			Clear();
		}
	}

	internal override void Set(RadialMenuElement element, RadialMenuAnimationManager animator)
	{
		if (element is ThrowElement throwElement)
		{
			m_title.text = m_itemName;
			m_subTitle.text = throwElement.SubTitle;
			m_icon.gameObject.SetActive(value: true);
			m_itemIcon.gameObject.SetActive(value: true);
			m_amountText.gameObject.SetActive(value: true);
			m_amountText.text = throwElement.m_inventoryAmountText;
		}
		else if (element is BackElement)
		{
			Clear();
			m_title.text = element.Name;
			m_amountText.gameObject.SetActive(value: false);
			m_icon.gameObject.SetActive(value: false);
		}
		else
		{
			Clear();
		}
	}

	public override void Clear()
	{
		m_amountText.gameObject.SetActive(value: true);
		m_title.text = m_itemName;
		m_subTitle.text = "";
		m_icon.gameObject.SetActive(value: true);
		if (m_data != null)
		{
			if (m_data.m_shared.m_maxStackSize > 1)
			{
				m_amountText.text = $"{m_data.m_stack} / {m_data.m_shared.m_maxStackSize}";
			}
			else
			{
				m_amountText.text = $"{m_data.m_stack}";
			}
		}
	}
}
