using System.Linq;
using TMPro;
using UnityEngine;

namespace Valheim.UI;

public class ThrowElement : RadialMenuElement
{
	[SerializeField]
	protected TextMeshProUGUI m_throwAmountText;

	[HideInInspector]
	public string m_inventoryAmountText;

	[HideInInspector]
	public ItemDrop.ItemData m_data;

	protected float m_throwMultiplier = -1f;

	protected int m_throwAmount = -1;

	public string TotalWeightString
	{
		get
		{
			Player localPlayer = Player.m_localPlayer;
			if (localPlayer == null)
			{
				return "";
			}
			int num = Mathf.CeilToInt(localPlayer.GetInventory().GetTotalWeight());
			int num2 = Mathf.CeilToInt(localPlayer.GetMaxCarryWeight());
			int num3 = Mathf.CeilToInt((float)ThrowAmount * m_data.GetNonStackedWeight());
			if (num - num3 > num2 && Mathf.Sin(Time.time * 10f) > 0f)
			{
				return $"<color=red>{num} - {num3}</color> / {num2}";
			}
			return $"{num} - {num3} / {num2}";
		}
	}

	public int ThrowAmount
	{
		get
		{
			if (m_throwAmount >= 0 || !(m_throwMultiplier >= 0f))
			{
				return m_throwAmount;
			}
			return Mathf.RoundToInt((float)m_data.m_stack * m_throwMultiplier);
		}
	}

	public void Init(ItemDrop.ItemData item, int throwAmount, string localString)
	{
		Init(item, throwAmount);
		m_throwAmountText.text = Localization.instance.Localize(localString);
	}

	public void Init(ItemDrop.ItemData item, int throwAmount)
	{
		m_throwAmount = throwAmount;
		m_throwAmountText.text = m_throwAmount.ToString();
		SetProperties(item);
	}

	private void SetProperties(ItemDrop.ItemData item)
	{
		base.Name = "";
		base.Interact = null;
		base.SubTitle = "";
		base.SecondaryInteract = null;
		base.Name = Localization.instance.Localize(item.m_shared.m_name);
		m_data = item;
		SetInteraction(item);
		SetSubTitle(item);
		m_icon.sprite = item?.GetIcon();
		m_icon.gameObject.SetActive(value: false);
		SetDescription(item);
	}

	private void SetDescription(ItemDrop.ItemData item)
	{
		base.Description = item.GetTooltip();
		int num = Mathf.CeilToInt((float)ThrowAmount * m_data.GetNonStackedWeight());
		string newWeightString = $"\n$item_weight: <color=orange>{item.GetNonStackedWeight()} ({item.GetWeight()} - {num} $item_total)</color>";
		base.Description = string.Join("\n", from line in base.Description.Split('\n')
			select (!line.StartsWith("$item_weight:")) ? line : newWeightString);
	}

	private void SetSubTitle(ItemDrop.ItemData item)
	{
		base.SubTitle = ((item.m_shared.m_maxStackSize > 1) ? $"{item.m_stack} - {ThrowAmount} / {item.m_shared.m_maxStackSize}" : "");
	}

	protected void SetInteraction(ItemDrop.ItemData item)
	{
		base.Interact = delegate
		{
			if ((bool)Player.m_localPlayer)
			{
				if (!Player.m_localPlayer.GetInventory().ContainsItemByName(item.m_shared.m_name))
				{
					return false;
				}
				Player.m_localPlayer.DropItem(null, item, ThrowAmount);
			}
			return true;
		};
	}
}
