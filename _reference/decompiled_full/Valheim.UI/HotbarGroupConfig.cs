using System.Collections.Generic;
using UnityEngine;

namespace Valheim.UI;

[CreateAssetMenu(fileName = "HotbarGroupConfig", menuName = "Valheim/Radial/Group Config/Hotbar Group Config")]
public class HotbarGroupConfig : ScriptableObject, IRadialConfig
{
	[SerializeField]
	protected Sprite m_icon;

	public string LocalizedName => Localization.instance.Localize("$radial_hotbar");

	public Sprite Sprite => m_icon;

	public void InitRadialConfig(RadialBase radial)
	{
		List<RadialMenuElement> list = new List<RadialMenuElement>();
		foreach (ItemDrop.ItemData item2 in Player.m_localPlayer.GetInventory().GetHotbar(includeEmpty: true))
		{
			RadialMenuElement item;
			if (item2 == null)
			{
				EmptyElement emptyElement = Object.Instantiate(RadialData.SO.EmptyElement);
				emptyElement.Init();
				item = emptyElement;
			}
			else
			{
				ItemElement itemElement = Object.Instantiate(RadialData.SO.ItemElement);
				itemElement.Init(item2);
				itemElement.CloseOnInteract = () => true;
				itemElement.AdvancedCloseOnInteract = null;
				item = itemElement;
			}
			list.Add(item);
		}
		radial.ConstructRadial(list);
	}
}
