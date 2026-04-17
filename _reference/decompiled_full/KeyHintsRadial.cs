using TMPro;
using UnityEngine;
using Valheim.UI;

public class KeyHintsRadial : MonoBehaviour
{
	public TextMeshProUGUI m_gamepadInteract;

	public TextMeshProUGUI m_gamepadBack;

	public TextMeshProUGUI m_gamepadDrop;

	public TextMeshProUGUI m_gamepadDropMulti;

	public TextMeshProUGUI m_gamepadClose;

	public TextMeshProUGUI m_gamepadCloseTopLevel;

	public GameObject m_kbInteract;

	public GameObject m_kbBack;

	public GameObject m_kbDrop;

	public GameObject m_kbDropMulti;

	public GameObject m_kbClose;

	public GameObject m_kbCloseTopLevel;

	public void UpdateGamepadHints()
	{
		if (m_gamepadInteract != null)
		{
			Localization.instance.RemoveTextFromCache(m_gamepadInteract);
			m_gamepadInteract.text = "$radial_interact  <mspace=0.6em>$KEY_RadialInteract</mspace>";
			Localization.instance.Localize(m_gamepadInteract.transform);
		}
		if (m_gamepadBack != null)
		{
			Localization.instance.RemoveTextFromCache(m_gamepadBack);
			m_gamepadBack.text = "$radial_back  <mspace=0.6em>$KEY_RadialClose</mspace>  /  <mspace=0.6em>$KEY_RadialBack</mspace>";
			Localization.instance.Localize(m_gamepadBack.transform);
		}
		if (m_gamepadClose != null)
		{
			Localization.instance.RemoveTextFromCache(m_gamepadClose);
			m_gamepadClose.text = "$radial_close  <mspace=0.6em>$KEY_Radial</mspace>";
			Localization.instance.Localize(m_gamepadClose.transform);
		}
		if (m_gamepadCloseTopLevel != null)
		{
			Localization.instance.RemoveTextFromCache(m_gamepadCloseTopLevel);
			m_gamepadCloseTopLevel.text = "$radial_close  <mspace=0.6em>$KEY_RadialClose</mspace>  /  <mspace=0.6em>$KEY_RadialBack</mspace>  /  <mspace=0.6em>$KEY_Radial</mspace>";
			Localization.instance.Localize(m_gamepadCloseTopLevel.transform);
		}
		if (m_gamepadDrop != null)
		{
			Localization.instance.RemoveTextFromCache(m_gamepadDrop);
			m_gamepadDrop.text = "$radial_drop  <mspace=0.6em>$KEY_RadialSecondaryInteract</mspace>";
			Localization.instance.Localize(m_gamepadDrop.transform);
		}
		if (m_gamepadDropMulti != null)
		{
			Localization.instance.RemoveTextFromCache(m_gamepadDropMulti);
			m_gamepadDropMulti.text = "$radial_drop_multiple  <mspace=0.6em>$KEY_RadialSecondaryInteract</mspace>  $radial_hold";
			Localization.instance.Localize(m_gamepadDropMulti.transform);
		}
	}

	public void UpdateRadialHints(RadialBase radial)
	{
		bool isTopLevel = radial.IsTopLevel;
		m_gamepadCloseTopLevel.gameObject.SetActive(isTopLevel);
		m_kbCloseTopLevel.SetActive(isTopLevel);
		m_gamepadClose.gameObject.SetActive(!isTopLevel);
		m_kbClose.gameObject.SetActive(!isTopLevel);
		m_gamepadBack.gameObject.SetActive(!isTopLevel);
		m_kbBack.SetActive(!isTopLevel);
		bool active = false;
		m_gamepadDrop.gameObject.SetActive(active);
		m_gamepadDropMulti.gameObject.SetActive(active);
		m_kbDrop.SetActive(active);
		m_kbDropMulti.SetActive(active);
	}
}
