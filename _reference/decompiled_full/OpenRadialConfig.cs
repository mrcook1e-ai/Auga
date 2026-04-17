using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valheim.UI;

[CreateAssetMenu(fileName = "OpenRadialConfig", menuName = "Valheim/Radial/Group Config/Open Radial Config")]
public class OpenRadialConfig : ScriptableObject, IRadialConfig
{
	public string LocalizedName => "Something Went Wrong If You're Seeing This";

	public Sprite Sprite => null;

	public void InitRadialConfig(RadialBase radial)
	{
		radial.OnInteractionDelay = delegate(float delay)
		{
			PlayerController.SetTakeInputDelay(delay);
		};
		radial.ShouldAnimateIn = true;
		radial.Open(RadialData.SO.EmoteGroupConfig);
	}

	private bool TryOpenNonDefaultRadials(RadialBase radial)
	{
		Player localPlayer = Player.m_localPlayer;
		if (!localPlayer)
		{
			return false;
		}
		GameObject hoverObject = localPlayer.GetHoverObject();
		if (!hoverObject)
		{
			return false;
		}
		if ((hoverObject.TryGetComponentInParent<Catapult>(out var _) || hoverObject.TryGetComponentInParent<ShieldGenerator>(out var _) || hoverObject.TryGetComponentInParent<Chair>(out var _)) && !hoverObject.TryGetComponent<Switch>(out var _))
		{
			return false;
		}
		if ((hoverObject ? hoverObject.GetComponentInParent<Interactable>() : null) == null)
		{
			return false;
		}
		if ((bool)hoverObject.GetComponentInParent<OfferingBowl>() || (bool)hoverObject.GetComponentInParent<Fermenter>() || (bool)hoverObject.GetComponentInParent<ItemStand>() || (bool)hoverObject.GetComponentInParent<Catapult>())
		{
			OpenItemMenu(radial, localPlayer, null, hoverObject);
			return true;
		}
		if (hoverObject.TryGetComponentInParent<IHasHoverMenu>(out var result4))
		{
			if (!result4.TryGetItems(localPlayer, out var items))
			{
				return false;
			}
			if (items.Count <= 0)
			{
				if (RadialData.SO.OpenNormalRadialWhenHoverMenuFails)
				{
					return false;
				}
				radial.QueuedClose();
				return true;
			}
			OpenItemMenu(radial, localPlayer, items, hoverObject);
			return true;
		}
		if (!hoverObject.TryGetComponentInParent<IHasHoverMenuExtended>(out var result5))
		{
			return false;
		}
		if (!result5.TryGetItems(localPlayer, hoverObject.GetComponent<Switch>(), out var items2))
		{
			return false;
		}
		if (items2.Count <= 0)
		{
			if (RadialData.SO.OpenNormalRadialWhenHoverMenuFails)
			{
				return false;
			}
			radial.QueuedClose();
			return true;
		}
		OpenItemMenu(radial, localPlayer, items2, hoverObject);
		return true;
	}

	private void OpenItemMenu(RadialBase radial, Player player, List<string> items, GameObject hoverObject)
	{
		radial.HoverObject = hoverObject;
		ItemGroupConfig itemGroupConfig = UnityEngine.Object.Instantiate(RadialData.SO.ItemGroupConfig);
		itemGroupConfig.GroupName = "allitems";
		if (items == null)
		{
			radial.Open(itemGroupConfig);
			return;
		}
		Inventory inventory = player.GetInventory();
		if (string.Equals(items[0], "type", StringComparison.OrdinalIgnoreCase))
		{
			ItemDrop.ItemData.ItemType[] array = (from i in items
				select Enum.TryParse<ItemDrop.ItemData.ItemType>(i, out var result4) ? result4 : ItemDrop.ItemData.ItemType.None into t
				where t != ItemDrop.ItemData.ItemType.None
				select t).ToArray();
			if (RadialData.SO.AllowSingleItemHoverMenu)
			{
				itemGroupConfig.ItemTypes = array;
				itemGroupConfig.GroupName = (hoverObject.TryGetComponentInParent<Piece>(out var result) ? result.m_name : "$piece_useitem");
			}
			int num = inventory.CountItemsByType(array, -1, matchWorldLevel: true, stacksOnly: true);
			if (num <= 1)
			{
				if (num > 0)
				{
					itemGroupConfig.m_customItemList = items.Skip(0).ToList();
				}
			}
			else
			{
				itemGroupConfig.ItemTypes = array;
				itemGroupConfig.GroupName = (hoverObject.TryGetComponentInParent<Piece>(out var result2) ? result2.m_name : "$piece_useitem");
			}
		}
		else
		{
			itemGroupConfig.m_customItemList = items;
			if (inventory.CountItemsByName(items.ToArray(), -1, matchWorldLevel: true, stacksOnly: true) > 1 || RadialData.SO.AllowSingleItemHoverMenu)
			{
				itemGroupConfig.GroupName = (hoverObject.TryGetComponentInParent<Piece>(out var result3) ? result3.m_name : "$piece_useitem");
			}
		}
		radial.Open(itemGroupConfig);
	}
}
