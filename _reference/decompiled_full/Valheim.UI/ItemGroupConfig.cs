using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Valheim.UI;

[CreateAssetMenu(fileName = "ItemGroupConfig", menuName = "Valheim/Radial/Group Config/Item Group Config")]
public class ItemGroupConfig : ScriptableObject, IRadialConfig
{
	[HideInInspector]
	public List<string> m_customItemList = new List<string>();

	private List<ItemDrop.ItemData> m_storedList;

	public string GroupName { get; set; }

	public ItemDrop.ItemData.ItemType[] ItemTypes { get; set; }

	public string LocalizedName => Localization.instance.Localize(IsCustom ? GroupName : ("$" + RadialData.SO.ItemGroupMappings.GetMapping(GroupName).LocaString));

	public Sprite Sprite => RadialData.SO.ItemGroupMappings.GetMapping(GroupName).Sprite;

	private bool IsCustom => RadialData.SO.ItemGroupMappings.Groups.All((ItemGroupMapping g) => g.Name != GroupName);

	public void InitRadialConfig(RadialBase radial)
	{
		List<RadialMenuElement> list = new List<RadialMenuElement>();
		Player localPlayer = Player.m_localPlayer;
		Inventory inventory = localPlayer.GetInventory();
		if (m_storedList != null)
		{
			foreach (ItemDrop.ItemData stored in m_storedList)
			{
				AddElement(list, stored, radial, localPlayer, inventory);
			}
			m_storedList = null;
			radial.ConstructRadial(list);
			return;
		}
		if (ItemTypes == null)
		{
			ItemDrop.ItemData.ItemType[] array = (ItemTypes = RadialData.SO.ItemGroupMappings.GetMapping(GroupName).ItemTypes);
		}
		if (ItemTypes[0] != ItemDrop.ItemData.ItemType.None)
		{
			foreach (ItemDrop.ItemData item in inventory.GetAllItemsOfType(ItemTypes, sortByGridOrder: true))
			{
				AddElement(list, item, radial, localPlayer, inventory);
			}
			if (m_customItemList.Count > 0 && list.Count > 0 && radial.StartItemIndex == -1)
			{
				if (m_customItemList[0] == "type")
				{
					radial.StartItemIndex = list.IndexOf(list.OfType<ItemElement>().FirstOrDefault((ItemElement e) => m_customItemList.Contains(e.m_data.m_shared.m_itemType.ToString()))) + 1;
				}
				else
				{
					radial.StartItemIndex = list.IndexOf(list.OfType<ItemElement>().FirstOrDefault((ItemElement e) => m_customItemList.Contains(e.m_data.m_shared.m_name))) + 1;
				}
			}
		}
		else
		{
			foreach (ItemDrop.ItemData item2 in from i in inventory.GetAllItemsInGridOrder()
				where m_customItemList.Contains(i.m_shared.m_name)
				select i)
			{
				AddElement(list, item2, radial, localPlayer, inventory);
			}
		}
		radial.ConstructRadial(list);
	}

	private void AddElement(List<RadialMenuElement> elements, ItemDrop.ItemData item, RadialBase radial, Player player, Inventory playerInventory)
	{
		ItemElement itemElement = UnityEngine.Object.Instantiate(RadialData.SO.ItemElement);
		itemElement.Init(item);
		elements.Add(itemElement);
		if (radial.IsHoverMenu)
		{
			itemElement.AdvancedCloseOnInteract = delegate(RadialBase radialBase, RadialArray<RadialMenuElement> radialArray)
			{
				Player localPlayer = Player.m_localPlayer;
				if (!localPlayer)
				{
					return true;
				}
				if (!radialBase.HoverObject)
				{
					return true;
				}
				if (radialBase.HoverObject.TryGetComponentInParent<IHasHoverMenuExtended>(out var result))
				{
					return !result.CanUseItems(localPlayer, radialBase.HoverObject.GetComponent<Switch>(), sendErrorMessage: false);
				}
				IHasHoverMenu result2;
				return radialBase.HoverObject.TryGetComponentInParent<IHasHoverMenu>(out result2) && !result2.CanUseItems(localPlayer, sendErrorMessage: false);
			};
		}
		else
		{
			if (!(item.m_shared.m_food > 0f))
			{
				return;
			}
			itemElement.AdvancedCloseOnInteract = delegate(RadialBase radialBase, RadialArray<RadialMenuElement> radialArray)
			{
				Player localPlayer = Player.m_localPlayer;
				return !localPlayer || radialArray.GetArray.Where((RadialMenuElement e) => e is ItemElement).Cast<ItemElement>().All((ItemElement element) => !localPlayer.CanEat(element.m_data, showMessages: false));
			};
		}
	}

	public bool ShouldAddItem(ItemDrop.ItemData newItemData)
	{
		if (!IsCustom)
		{
			return Array.IndexOf(ItemTypes, newItemData.m_shared.m_itemType) > -1;
		}
		return m_customItemList.Contains(newItemData.m_shared.m_name);
	}

	public void AddItem(RadialBase radial, ItemDrop.ItemData newItemData, RadialArray<RadialMenuElement> currentElements)
	{
		if (ShouldAddItem(newItemData))
		{
			List<ItemDrop.ItemData> list = (from ItemElement element in currentElements.GetArray.Where((RadialMenuElement element) => element is ItemElement)
				select element.m_data).ToList();
			List<ItemDrop.ItemData> list2 = (from item in Player.m_localPlayer.GetInventory().GetAllItems()
				where (!IsCustom) ? (Array.IndexOf(ItemTypes, item.m_shared.m_itemType) > -1) : m_customItemList.Contains(item.m_shared.m_name)
				select item).ToList();
			if (list.Count == list2.Count)
			{
				radial.Refresh();
				return;
			}
			list.Add(newItemData);
			m_storedList = list;
			radial.Refresh();
		}
	}
}
