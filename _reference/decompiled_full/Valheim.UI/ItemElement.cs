using System;
using TMPro;
using UnityEngine;

namespace Valheim.UI;

public class ItemElement : RadialMenuElement
{
	public GameObject m_go;

	public GuiBar m_durability;

	public TMP_Text m_amount;

	public int m_stackText = -1;

	public ItemDrop.ItemData m_data;

	public Func<GameObject, bool> HoverMenuInteract { get; set; }

	public void Init(ItemDrop.ItemData item)
	{
		base.Name = "";
		base.Interact = null;
		base.SubTitle = "";
		base.SecondaryInteract = null;
		m_go.SetActive(item != null);
		if (item != null)
		{
			m_data = item;
			SetInteraction(item);
			base.Name = Localization.instance.Localize(item.m_shared.m_name);
			base.Description = item.GetTooltip();
			m_icon.sprite = item?.GetIcon();
			base.Activated = (m_data.m_equipped ? 1f : 0f);
			SetAmount(item);
			SetDurability(item);
		}
	}

	public void UpdateQueueAndActivation(float progress, Player.MinorActionData data, int queueCount)
	{
		base.Queued = progress <= 0.01f && (base.Queued || queueCount > 1);
		base.Activated = ((data.m_type == Player.MinorActionData.ActionType.Unequip) ? (1f - progress) : progress);
	}

	public void UpdateQueueAndActivation(bool equipActionQueued)
	{
		base.Queued = equipActionQueued;
		base.Activated = (m_data.m_equipped ? 1f : 0f);
	}

	protected virtual void SetInteraction(ItemDrop.ItemData item)
	{
		base.Interact = delegate
		{
			if ((bool)Player.m_localPlayer)
			{
				Player.m_localPlayer.UseItem(null, item, fromInventoryGui: false);
			}
			return true;
		};
		HoverMenuInteract = delegate(GameObject hoverObject)
		{
			if ((bool)Player.m_localPlayer)
			{
				Player.m_localPlayer.TryUseItemOnInteractable(item, hoverObject, fromInventoryGui: false);
			}
			return true;
		};
		base.SecondaryInteract = delegate
		{
			if ((bool)Player.m_localPlayer)
			{
				Player.m_localPlayer.DropItem(null, item, 1);
			}
			return true;
		};
		base.TryOpenSubRadial = delegate(RadialBase menu, int currentIndex)
		{
			if (m_data.m_stack <= 1 || m_data.m_shared.m_maxStackSize <= 1)
			{
				return false;
			}
			menu.BackIndex = currentIndex;
			menu.QueuedOpen(RadialData.SO.ThrowGroupConfig, menu.CurrentConfig);
			return true;
		};
		base.CloseOnInteract = () => m_data == null || !m_data.IsEquipable();
	}

	public void UpdateDurabilityAndAmount()
	{
		SetAmount(m_data);
		SetDurability(m_data);
	}

	protected virtual void SetAmount(ItemDrop.ItemData item)
	{
		if (item.m_shared.m_maxStackSize > 1)
		{
			m_amount.gameObject.SetActive(value: true);
			if (m_stackText != item.m_stack)
			{
				m_amount.text = $"{item.m_stack} / {item.m_shared.m_maxStackSize}";
				m_stackText = item.m_stack;
			}
			base.SubTitle = m_amount.text;
		}
		else
		{
			m_amount.gameObject.SetActive(value: false);
		}
	}

	protected virtual void SetDurability(ItemDrop.ItemData item)
	{
		bool flag = item.m_shared.m_useDurability && item.m_durability < item.GetMaxDurability();
		m_durability.gameObject.SetActive(flag);
		if (flag)
		{
			if (item.m_durability <= 0f)
			{
				m_durability.SetValue(1f);
				m_durability.SetColor((Mathf.Sin(Time.time * 10f) > 0f) ? Color.red : new Color(0f, 0f, 0f, 0f));
			}
			else
			{
				m_durability.SetValue(item.GetDurabilityPercentage());
				m_durability.ResetColor();
			}
		}
	}
}
