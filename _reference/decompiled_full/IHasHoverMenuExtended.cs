using System.Collections.Generic;

public interface IHasHoverMenuExtended
{
	bool TryGetItems(Player player, Switch switchRef, out List<string> items);

	bool CanUseItems(Player player, Switch switchRef, bool sendErrorMessage = true);
}
