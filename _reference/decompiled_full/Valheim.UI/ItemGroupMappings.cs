using UnityEngine;

namespace Valheim.UI;

[CreateAssetMenu(fileName = "ItemGroupMappings", menuName = "Valheim/Radial/Mappings/Item Group Mappings")]
public class ItemGroupMappings : ScriptableObject
{
	[SerializeField]
	protected ItemGroupMapping[] _itemGroups;

	public ItemGroupMapping[] Groups => _itemGroups;

	public ItemGroupMapping GetMapping(string group)
	{
		if (_itemGroups != null)
		{
			for (int i = 0; i < _itemGroups.Length; i++)
			{
				if (_itemGroups[i].Name == group)
				{
					return _itemGroups[i];
				}
			}
		}
		return new ItemGroupMapping
		{
			Name = ItemGroupMapping.None,
			ItemTypes = new ItemDrop.ItemData.ItemType[1]
		};
	}
}
