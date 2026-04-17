using System.Collections.Generic;

public interface IHasHoverMenu
{
	bool TryGetItems(Player player, out List<string> items);

	bool CanUseItems(Player player, bool sendErrorMessage = true);
}
