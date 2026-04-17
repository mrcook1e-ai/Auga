using TMPro;
using UnityEngine;
using Valheim.SettingsGui;

public class KeyHints : MonoBehaviour
{
	private static KeyHints m_instance;

	[Header("Key hints")]
	public GameObject m_buildHints;

	public GameObject m_combatHints;

	public GameObject m_inventoryHints;

	public GameObject m_inventoryWithContainerHints;

	public GameObject m_fishingHints;

	public GameObject m_barberHints;

	public GameObject m_radialHints;

	public GameObject[] m_equipButtons;

	public GameObject m_primaryAttackGP;

	public GameObject m_primaryAttackKB;

	public GameObject m_secondaryAttackGP;

	public GameObject m_secondaryAttackKB;

	public GameObject m_closeMenuHintKB;

	public GameObject m_closeMenuHintGP;

	public GameObject m_bowDrawGP;

	public GameObject m_bowDrawKB;

	private bool m_keyHintsEnabled = true;

	public TextMeshProUGUI m_buildMenuKey;

	public TextMeshProUGUI m_buildRotateKey;

	public TextMeshProUGUI m_buildAlternativePlacingKey;

	public TextMeshProUGUI m_dodgeKey;

	public TextMeshProUGUI m_cycleSnapKey;

	public KeyHintsRadial m_radialKeyHints;

	public static KeyHints instance => m_instance;

	private void OnDestroy()
	{
		m_instance = null;
	}

	private void Awake()
	{
		m_instance = this;
		ApplySettings();
	}

	public void SetGamePadBindings()
	{
		if (m_cycleSnapKey != null)
		{
			m_cycleSnapKey.text = "$hud_cyclesnap  <mspace=0.6em>$KEY_PrevSnap / $KEY_NextSnap</mspace>";
			Localization.instance.Localize(m_cycleSnapKey.transform);
		}
		if (m_buildMenuKey != null)
		{
			Localization.instance.RemoveTextFromCache(m_buildMenuKey);
			switch (ZInput.InputLayout)
			{
			case InputLayout.Default:
				m_buildMenuKey.text = "$hud_buildmenu  <mspace=0.6em>$KEY_Use</mspace>";
				break;
			case InputLayout.Alternative1:
			case InputLayout.Alternative2:
				m_buildMenuKey.text = "$hud_buildmenu  <mspace=0.6em>$KEY_BuildMenu</mspace>";
				break;
			}
			Localization.instance.Localize(m_buildMenuKey.transform);
		}
		if (m_buildRotateKey != null)
		{
			Localization.instance.RemoveTextFromCache(m_buildRotateKey);
			switch (ZInput.InputLayout)
			{
			case InputLayout.Default:
				m_buildRotateKey.text = "$hud_rotate  <mspace=0.6em>$KEY_Block + $KEY_RStick</mspace>";
				break;
			case InputLayout.Alternative1:
			case InputLayout.Alternative2:
				m_buildRotateKey.text = "$hud_rotate  <mspace=0.6em>$KEY_LTrigger / $KEY_RTrigger</mspace>";
				break;
			}
			Localization.instance.Localize(m_buildRotateKey.transform);
		}
		if (m_dodgeKey != null)
		{
			Localization.instance.RemoveTextFromCache(m_dodgeKey);
			switch (ZInput.InputLayout)
			{
			case InputLayout.Default:
				m_dodgeKey.text = "$settings_dodge  <mspace=0.6em>$KEY_Block + $KEY_Jump</mspace>";
				break;
			case InputLayout.Alternative1:
			case InputLayout.Alternative2:
				m_dodgeKey.text = "$settings_dodge  <mspace=0.6em>$KEY_Block + $KEY_Dodge</mspace>";
				break;
			}
			Localization.instance.Localize(m_dodgeKey.transform);
		}
		m_radialKeyHints.UpdateGamepadHints();
	}

	private void Start()
	{
	}

	public void ApplySettings()
	{
		m_keyHintsEnabled = PlatformPrefs.GetInt("KeyHints", 1) == 1;
		SetGamePadBindings();
	}

	private void Update()
	{
		UpdateHints();
		if (ZInput.GetKeyDown(KeyCode.F9))
		{
			ZInput.instance.ChangeLayout(GamepadMapController.NextLayout(ZInput.InputLayout));
			Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, Localization.instance.Localize("Changed controller layout to: " + GamepadMapController.GetLayoutStringId(ZInput.InputLayout)));
			ApplySettings();
		}
	}

	private void UpdateHints()
	{
		Player localPlayer = Player.m_localPlayer;
		if (!m_keyHintsEnabled || localPlayer == null || localPlayer.IsDead() || Chat.instance.IsChatDialogWindowVisible() || Game.IsPaused() || (InventoryGui.instance != null && (InventoryGui.instance.IsSkillsPanelOpen || InventoryGui.instance.IsTrophisPanelOpen || InventoryGui.instance.IsTextPanelOpen)))
		{
			m_buildHints.SetActive(value: false);
			m_combatHints.SetActive(value: false);
			m_inventoryHints.SetActive(value: false);
			m_inventoryWithContainerHints.SetActive(value: false);
			m_fishingHints.SetActive(value: false);
			m_barberHints.SetActive(value: false);
			m_radialHints.SetActive(value: false);
			return;
		}
		_ = m_buildHints.activeSelf;
		_ = m_buildHints.activeSelf;
		ItemDrop.ItemData currentWeapon = localPlayer.GetCurrentWeapon();
		if (InventoryGui.IsVisible())
		{
			bool flag = InventoryGui.instance.IsContainerOpen();
			bool flag2 = InventoryGui.instance.ActiveGroup == 0;
			ItemDrop.ItemData itemData = (flag2 ? InventoryGui.instance.ContainerGrid.GetGamepadSelectedItem() : InventoryGui.instance.m_playerGrid.GetGamepadSelectedItem());
			bool flag3 = itemData?.IsEquipable() ?? false;
			bool flag4 = itemData != null && itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable;
			m_buildHints.SetActive(value: false);
			m_combatHints.SetActive(value: false);
			m_inventoryHints.SetActive(!flag);
			m_inventoryWithContainerHints.SetActive(flag);
			for (int i = 0; i < m_equipButtons.Length; i++)
			{
				m_equipButtons[i].SetActive(flag4 || (flag3 && !flag2));
			}
			m_fishingHints.SetActive(value: false);
			m_barberHints.SetActive(value: false);
			m_radialHints.SetActive(value: false);
		}
		else if (Hud.instance.m_radialMenu.Active)
		{
			m_buildHints.SetActive(value: false);
			m_combatHints.SetActive(value: false);
			m_inventoryHints.SetActive(value: false);
			m_inventoryWithContainerHints.SetActive(value: false);
			m_fishingHints.SetActive(value: false);
			m_barberHints.SetActive(value: false);
			m_radialHints.SetActive(value: true);
		}
		else if (localPlayer.InPlaceMode())
		{
			if (ZInput.IsNonClassicFunctionality())
			{
				string text = Localization.instance.Localize("$hud_altplacement  <mspace=0.6em>$KEY_AltKeys + $KEY_AltPlace</mspace>");
				string text2 = (localPlayer.AlternativePlacementActive ? Localization.instance.Localize("$hud_off") : Localization.instance.Localize("$hud_on"));
				m_buildAlternativePlacingKey.text = text + " " + text2;
			}
			if (Hud.IsPieceSelectionVisible())
			{
				if (ZInput.IsGamepadActive())
				{
					m_closeMenuHintGP.SetActive(value: true);
				}
				else
				{
					m_closeMenuHintKB.SetActive(value: true);
				}
			}
			else
			{
				m_closeMenuHintGP.SetActive(value: false);
				m_closeMenuHintKB.SetActive(value: false);
			}
			m_buildHints.SetActive(value: true);
			m_combatHints.SetActive(value: false);
			m_inventoryHints.SetActive(value: false);
			m_inventoryWithContainerHints.SetActive(value: false);
			m_fishingHints.SetActive(value: false);
			m_barberHints.SetActive(value: false);
			m_radialHints.SetActive(value: false);
		}
		else if (PlayerCustomizaton.IsBarberGuiVisible())
		{
			m_buildHints.SetActive(value: false);
			m_combatHints.SetActive(value: false);
			m_inventoryHints.SetActive(value: false);
			m_inventoryWithContainerHints.SetActive(value: false);
			m_fishingHints.SetActive(value: false);
			m_barberHints.SetActive(value: true);
			m_radialHints.SetActive(value: false);
		}
		else if (localPlayer.GetDoodadController() != null)
		{
			m_buildHints.SetActive(value: false);
			m_combatHints.SetActive(value: false);
			m_inventoryHints.SetActive(value: false);
			m_inventoryWithContainerHints.SetActive(value: false);
			m_fishingHints.SetActive(value: false);
			m_barberHints.SetActive(value: false);
			m_radialHints.SetActive(value: false);
		}
		else if (currentWeapon != null && currentWeapon.m_shared.m_animationState == ItemDrop.ItemData.AnimationState.FishingRod)
		{
			m_buildHints.SetActive(value: false);
			m_combatHints.SetActive(value: false);
			m_inventoryHints.SetActive(value: false);
			m_inventoryWithContainerHints.SetActive(value: false);
			m_fishingHints.SetActive(value: true);
			m_radialHints.SetActive(value: false);
		}
		else if (currentWeapon != null && (currentWeapon != localPlayer.m_unarmedWeapon.m_itemData || localPlayer.IsTargeted()))
		{
			m_buildHints.SetActive(value: false);
			m_combatHints.SetActive(value: true);
			m_inventoryHints.SetActive(value: false);
			m_inventoryWithContainerHints.SetActive(value: false);
			m_fishingHints.SetActive(value: false);
			m_barberHints.SetActive(value: false);
			m_radialHints.SetActive(value: false);
			bool flag5 = currentWeapon.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow && currentWeapon.m_shared.m_skillType != Skills.SkillType.Crossbows;
			bool active = !flag5 && currentWeapon.HavePrimaryAttack();
			bool active2 = !flag5 && currentWeapon.HaveSecondaryAttack();
			m_bowDrawGP.SetActive(flag5);
			m_bowDrawKB.SetActive(flag5);
			m_primaryAttackGP.SetActive(active);
			m_primaryAttackKB.SetActive(active);
			m_secondaryAttackGP.SetActive(active2);
			m_secondaryAttackKB.SetActive(active2);
		}
		else
		{
			m_buildHints.SetActive(value: false);
			m_combatHints.SetActive(value: false);
			m_inventoryHints.SetActive(value: false);
			m_inventoryWithContainerHints.SetActive(value: false);
			m_fishingHints.SetActive(value: false);
			m_barberHints.SetActive(value: false);
			m_radialHints.SetActive(value: false);
		}
		if (m_radialKeyHints.isActiveAndEnabled)
		{
			m_radialKeyHints.UpdateRadialHints(Hud.instance.m_radialMenu);
		}
	}
}
