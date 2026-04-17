using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Valheim.UI;

public class HammerItemElement : GroupElement
{
	private static ItemDrop.ItemData m_lastLeftItem;

	private static ItemDrop.ItemData m_lastRightItem;

	[SerializeField]
	protected Sprite m_hammerIcon;

	protected ItemDrop.ItemData m_hammerRef;

	public void Init()
	{
		Player localPlayer = Player.m_localPlayer;
		List<ItemDrop.ItemData> list = (from e in localPlayer.GetInventory().GetAllItemsInGridOrder()
			where IsHammer(e) && e.m_durability > 0f
			select e).ToList();
		if (list.Count > 0)
		{
			m_hammerRef = list.FirstOrDefault((ItemDrop.ItemData i) => i.m_equipped) ?? list[0];
			base.Icon.sprite = m_hammerIcon;
			SetInteraction(m_hammerRef);
			base.Activated = ((!m_hammerRef.m_equipped) ? 0f : ((localPlayer.GetActionQueueCount() > 0) ? 0f : 1f));
			base.Name = Localization.instance.Localize("$radial_hammer");
		}
	}

	private static bool IsHammer(ItemDrop.ItemData item)
	{
		if (item != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Tool && item.m_shared.m_skillType == Skills.SkillType.Swords)
		{
			return item.m_shared.m_name.Contains("hammer");
		}
		return false;
	}

	protected void SetInteraction(ItemDrop.ItemData item)
	{
		base.Interact = delegate
		{
			if ((bool)Player.m_localPlayer)
			{
				if (item.m_equipped)
				{
					Player.m_localPlayer.UseItem(null, item, fromInventoryGui: false);
					if (m_lastLeftItem != null)
					{
						Player.m_localPlayer.EquipItem(m_lastLeftItem);
						m_lastLeftItem = null;
					}
					if (m_lastRightItem != null)
					{
						Player.m_localPlayer.EquipItem(m_lastRightItem);
						m_lastRightItem = null;
					}
				}
				else
				{
					m_lastLeftItem = Player.m_localPlayer.LeftItem;
					m_lastRightItem = Player.m_localPlayer.RightItem;
					Player.m_localPlayer.UseItem(null, item, fromInventoryGui: false);
					Player.m_localPlayer.SetSelectedPiece(Vector2Int.zero);
				}
			}
			base.Activated = (m_hammerRef.m_equipped ? 1f : 0f);
			return true;
		};
	}
}
