using UnityEngine;

[CreateAssetMenu(fileName = "New graphics settings preset type", menuName = "Graphics settings preset type")]
public class GraphicsSettingsPresetType : ScriptableObject
{
	[SerializeField]
	[Tooltip("The name of the preset. This string is run though the localization system and can therefore be a localization string key.")]
	private string m_nameTextId;

	[SerializeField]
	[Tooltip("The description displayed next to the preset. This string is run though the localization system and can therefore be a localization string key.")]
	private string m_descriptionTextId;

	[SerializeField]
	[Tooltip("The identifier used to store which preset is currently being used in PlayerPrefs.")]
	private int m_id;

	public string NameTextId => m_nameTextId;

	public string DescriptionTextId => m_descriptionTextId;

	public int ID => m_id;
}
