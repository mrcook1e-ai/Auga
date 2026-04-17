using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemSets : MonoBehaviour
{
	[Serializable]
	public class ItemSet
	{
		public string m_name;

		public List<SetItem> m_items = new List<SetItem>();

		public List<SetSkill> m_skills = new List<SetSkill>();

		public List<CraftingStation> m_knownStations = new List<CraftingStation>();

		public List<ItemDrop> m_knownItems = new List<ItemDrop>();

		public List<string> m_inheritKnownFromItemSet = new List<string>();
	}

	[Serializable]
	public class SetItem
	{
		public ItemDrop m_item;

		public int m_quality = 1;

		public int m_stack = 1;

		public bool m_use = true;

		public int m_hotbarSlot;
	}

	[Serializable]
	public class SetSkill
	{
		public Skills.SkillType m_skill;

		public int m_level;
	}

	private static ItemSets m_instance;

	public List<ItemSet> m_sets = new List<ItemSet>();

	public static ItemSets instance => m_instance;

	public void Awake()
	{
		m_instance = this;
	}

	public bool TryGetSet(string name, bool dropCurrentItems = false, int itemLevelOverride = -1, int worldLevel = -1)
	{
		if (Player.m_localPlayer == null)
		{
			return false;
		}
		ItemSet set = GetSet(name);
		if (set != null)
		{
			Skills skills = Player.m_localPlayer.GetSkills();
			if (dropCurrentItems)
			{
				Player.m_localPlayer.CreateTombStone();
				Player.m_localPlayer.ClearFood();
				Player.m_localPlayer.ClearHardDeath();
				Player.m_localPlayer.GetSEMan().RemoveAllStatusEffects();
				foreach (Skills.SkillDef skill in skills.m_skills)
				{
					skills.CheatResetSkill(skill.m_skill.ToString());
				}
			}
			Inventory inventory = Player.m_localPlayer.GetInventory();
			InventoryGui.instance.m_playerGrid.UpdateInventory(inventory, Player.m_localPlayer, null);
			foreach (SetItem item in set.m_items)
			{
				if (item.m_item == null)
				{
					continue;
				}
				int amount = Math.Max(1, item.m_stack);
				int num = Math.Max(1, (itemLevelOverride >= 0) ? itemLevelOverride : item.m_quality);
				if (num > 4)
				{
					num = 4;
				}
				ItemDrop.ItemData itemData = inventory.AddItem(item.m_item.gameObject.name, Math.Max(1, item.m_stack), num, 0, 0L, "Thor", pickedUp: true);
				if (worldLevel >= 0)
				{
					itemData.m_worldLevel = worldLevel;
				}
				if (itemData != null)
				{
					if (item.m_use)
					{
						Player.m_localPlayer.UseItem(inventory, itemData, fromInventoryGui: false);
					}
					if (item.m_hotbarSlot > 0)
					{
						InventoryGui.instance.m_playerGrid.DropItem(inventory, itemData, amount, new Vector2i(item.m_hotbarSlot - 1, 0));
					}
				}
			}
			foreach (SetSkill skill2 in set.m_skills)
			{
				skills.CheatResetSkill(skill2.m_skill.ToString());
				Player.m_localPlayer.GetSkills().CheatRaiseSkill(skill2.m_skill.ToString(), skill2.m_level);
			}
			Player.m_localPlayer.ResetCharacterKnownItems();
			AddKnown(set);
			return true;
		}
		return false;
	}

	private void AddKnown(ItemSet set)
	{
		foreach (CraftingStation knownStation in set.m_knownStations)
		{
			Player.m_localPlayer.AddKnownStation(knownStation);
		}
		foreach (ItemDrop knownItem in set.m_knownItems)
		{
			Player.m_localPlayer.AddKnownItem(knownItem.m_itemData);
		}
		foreach (string item in set.m_inheritKnownFromItemSet)
		{
			ItemSet set2 = GetSet(item);
			if (set2 != null)
			{
				AddKnown(set2);
			}
		}
	}

	public List<string> GetSetNames()
	{
		return GetSetDictionary().Keys.ToList();
	}

	public Dictionary<string, ItemSet> GetSetDictionary()
	{
		Dictionary<string, ItemSet> dictionary = new Dictionary<string, ItemSet>();
		foreach (ItemSet set in m_sets)
		{
			dictionary[set.m_name] = set;
		}
		return dictionary;
	}

	public ItemSet GetSet(string name)
	{
		name = name.ToLower();
		foreach (ItemSet set in m_sets)
		{
			if (set.m_name.ToLower() == name)
			{
				return set;
			}
		}
		return null;
	}
}
