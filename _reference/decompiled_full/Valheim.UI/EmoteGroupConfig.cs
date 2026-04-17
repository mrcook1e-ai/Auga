using System.Collections.Generic;
using UnityEngine;

namespace Valheim.UI;

[CreateAssetMenu(fileName = "EmoteGroupConfig", menuName = "Valheim/Radial/Group Config/Emote Group Config")]
public class EmoteGroupConfig : ScriptableObject, IRadialConfig
{
	[SerializeField]
	protected Sprite m_icon;

	public string LocalizedName => Localization.instance.Localize("$radial_emotes");

	public Sprite Sprite => m_icon;

	public void InitRadialConfig(RadialBase radial)
	{
		List<RadialMenuElement> list = new List<RadialMenuElement>();
		for (int i = 0; i < 25; i++)
		{
			EmoteElement emoteElement = Object.Instantiate(RadialData.SO.EmoteElement);
			EmoteDataMapping mapping = RadialData.SO.EmoteMappings.GetMapping((Emotes)i);
			emoteElement.Init(mapping);
			list.Add(emoteElement);
		}
		radial.ConstructRadial(list);
	}
}
