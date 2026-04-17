using System.Collections.Generic;
using UnityEngine;

namespace Valheim.UI;

[CreateAssetMenu(fileName = "ThrowGroupConfig", menuName = "Valheim/Radial/Group Config/Throw Group Config")]
public class ThrowGroupConfig : ScriptableObject, IRadialConfig
{
	[SerializeField]
	protected float[] m_possibleThrowAmounts = new float[0];

	private int m_storedOffset;

	private ItemDrop.ItemData m_storedItemData;

	public string LocalizedName => Localization.instance.Localize("$inventory_drop");

	public Sprite Sprite => null;

	public void InitRadialConfig(RadialBase radial)
	{
		if (m_possibleThrowAmounts.Length == 0)
		{
			Debug.LogError("Possible Throw Amounts needs at least one entry!");
			return;
		}
		if (radial.Selected == null && !radial.IsRefresh)
		{
			Debug.LogError("Selected cannot be null when opening throw radial.");
			return;
		}
		RadialMenuElement selected = radial.Selected;
		ItemDrop.ItemData itemData = ((selected is ItemElement itemElement) ? itemElement.m_data : ((!(selected is ThrowElement throwElement)) ? m_storedItemData : throwElement.m_data));
		ItemDrop.ItemData itemData2 = itemData;
		if (itemData2 == null)
		{
			Debug.LogError("Throw radial must be opened from an ItemElement or ThrowElement.");
			return;
		}
		Player localPlayer = Player.m_localPlayer;
		if (localPlayer != null && !localPlayer.GetInventory().ContainsItem(itemData2))
		{
			radial.Back();
			return;
		}
		List<RadialMenuElement> list = new List<RadialMenuElement>();
		Populate(list, radial, itemData2, radial.MaxElementsPerLayer);
		if (list.Count <= 0)
		{
			radial.Back();
			return;
		}
		bool flag = list.Count + 1 < radial.MaxElementsPerLayer;
		if (radial.Selected is ItemElement)
		{
			m_storedOffset = (int)UIMath.Mod(radial.BackIndex, 360f);
			m_storedItemData = itemData2;
		}
		radial.StartOffset = (int)UIMath.Mod(flag ? m_storedOffset : (m_storedOffset - 1), radial.MaxElementsPerLayer);
		radial.ConstructRadial(list);
	}

	private void Populate(List<RadialMenuElement> elements, RadialBase radial, ItemDrop.ItemData data, int maxElements)
	{
		for (int i = -1; i <= data.m_stack; i++)
		{
			if (i <= 0)
			{
				ThrowElement throwElement = Object.Instantiate(RadialData.SO.ThrowElement);
				throwElement.Init(data, (i == -1) ? data.m_stack : Mathf.CeilToInt((float)data.m_stack * 0.5f), (i == -1) ? "All" : "Half");
				elements.Add(throwElement);
			}
			else if (data.m_stack - i >= 0)
			{
				ThrowElement throwElement2 = Object.Instantiate(RadialData.SO.ThrowElement);
				throwElement2.Init(data, i);
				elements.Add(throwElement2);
			}
		}
		int num = elements.Count + 1;
		if (num < maxElements)
		{
			int num2 = ((num % 2 == 0) ? Mathf.FloorToInt((float)num / 2f) : (num / 2));
			RadialMenuElement item = elements[0];
			elements.Insert(num2 + 1, item);
			elements.RemoveAt(0);
			item = elements[0];
			elements.Insert(num2 + 1, item);
			elements.RemoveAt(0);
		}
	}
}
