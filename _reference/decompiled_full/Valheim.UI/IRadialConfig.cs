using UnityEngine;

namespace Valheim.UI;

public interface IRadialConfig
{
	string LocalizedName { get; }

	Sprite Sprite { get; }

	void InitRadialConfig(RadialBase radial);
}
