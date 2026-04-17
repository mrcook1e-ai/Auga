namespace Valheim.UI;

public class BackElement : GroupElement
{
	public void Init(RadialBase radial)
	{
		base.Name = "Back";
		base.Interact = delegate
		{
			radial.Back();
			return true;
		};
	}
}
