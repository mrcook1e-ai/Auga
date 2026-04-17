using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Pet : MonoBehaviour, Hoverable, Interactable, IRemoved, IPlaced
{
	public ItemDrop m_FeedItem;

	public int m_UpdateRate = 10;

	public List<string> m_deepKnowledge = new List<string>();

	private ItemStand m_itemStand;

	private Tameable m_tameable;

	private Procreation m_procreation;

	private RandomSpeak m_randomSpeak;

	private MaterialVariation m_materialVariation;

	private ZNetView m_nview;

	private Renderer m_renderer;

	private void Awake()
	{
		m_tameable = GetComponent<Tameable>();
		m_procreation = GetComponent<Procreation>();
		m_materialVariation = GetComponentInChildren<MaterialVariation>();
		m_itemStand = GetComponent<ItemStand>();
		m_nview = GetComponent<ZNetView>();
		m_renderer = GetComponentInChildren<Renderer>();
		m_randomSpeak = GetComponent<RandomSpeak>();
		if ((bool)m_materialVariation)
		{
			InvokeRepeating("UpdateMaterial", UnityEngine.Random.Range(1, m_UpdateRate), m_UpdateRate);
		}
		Tameable tameable = m_tameable;
		tameable.m_tameTextGetter = (Tameable.TextGetter)Delegate.Combine(tameable.m_tameTextGetter, new Tameable.TextGetter(GetStr));
	}

	private void UpdateMaterial()
	{
		if (m_nview == null)
		{
			return;
		}
		int material = m_materialVariation.GetMaterial();
		if (m_randomSpeak != null)
		{
			m_randomSpeak.enabled = material != 5 && material != 6;
		}
		if (!m_nview.IsOwner() || m_renderer.isVisible)
		{
			return;
		}
		List<Player> allPlayers = Player.GetAllPlayers();
		float num = 99999f;
		float num2 = 0.5f;
		if (m_materialVariation.GetMaterial() == 5)
		{
			num2 = 0.07f;
		}
		foreach (Player item in allPlayers)
		{
			float num3 = Utils.DistanceXZ(item.transform.position, base.transform.position);
			if (num > num3)
			{
				num = num3;
			}
			if (num3 > 10f)
			{
				continue;
			}
			SEMan sEMan = item.GetSEMan();
			if (sEMan == null)
			{
				continue;
			}
			if (sEMan.HaveStatusEffect(SEMan.s_statusEffectSoftDeath) && UnityEngine.Random.value > num2)
			{
				SetFace(1);
				return;
			}
			if ((sEMan.HaveStatusEffect(SEMan.s_statusEffectRested) || sEMan.HaveStatusEffect(SEMan.s_statusEffectCampFire) || m_procreation.GetLovePoints() > 2) && UnityEngine.Random.value > num2)
			{
				SetFace(0);
				return;
			}
			if (sEMan.HaveStatusEffect(SEMan.s_statusEffectBurning) || sEMan.HaveStatusEffect(SEMan.s_statusEffectFreezing) || (sEMan.HaveStatusEffect(SEMan.s_statusEffectPoison) && UnityEngine.Random.value > num2))
			{
				SetFace(3);
				return;
			}
			if (sEMan.HaveStatusEffect(SEMan.s_statusEffectEncumbered) && UnityEngine.Random.value > num2)
			{
				SetFace(7);
				return;
			}
			if (sEMan.HaveStatusEffect(SEMan.s_statusEffectSmoked) && UnityEngine.Random.value > num2)
			{
				SetFace(5);
				return;
			}
			if (DateTime.Now - TimeSpan.FromSeconds(m_UpdateRate) < Player.LastEmoteTime)
			{
				if (Player.LastEmote == "cry" && UnityEngine.Random.value > num2)
				{
					SetFace((!(UnityEngine.Random.value > 0.5f)) ? 1 : 3);
					return;
				}
				if ((Player.LastEmote == "cheer" || Player.LastEmote == "toast" || Player.LastEmote == "flex" || Player.LastEmote == "laugh") && UnityEngine.Random.value > num2)
				{
					SetFace((!(UnityEngine.Random.value > 0.5f)) ? 4 : 0);
					return;
				}
				if ((Player.LastEmote == "blowkiss" || Player.LastEmote == "dance" || Player.LastEmote == "shrug" || Player.LastEmote == "roar") && UnityEngine.Random.value > num2)
				{
					SetFace((UnityEngine.Random.value > 0.5f) ? 5 : 7);
					return;
				}
				if ((Player.LastEmote == "kneel" || Player.LastEmote == "bow" || Player.LastEmote == "sit") && UnityEngine.Random.value > num2)
				{
					SetFace((UnityEngine.Random.value > 0.5f) ? 4 : 2);
					return;
				}
			}
		}
		if (UnityEngine.Random.value < 0.1f && (allPlayers.Count == 1 || num > 20f))
		{
			SetFace(UnityEngine.Random.Range(0, m_materialVariation.m_materials.Count));
		}
	}

	public void SetFace(int index)
	{
		m_materialVariation.SetMaterial(index);
		if (m_randomSpeak != null)
		{
			m_randomSpeak.enabled = m_materialVariation.GetMaterial() != 7;
		}
	}

	private string GetStr()
	{
		EnvMan instance = EnvMan.instance;
		float dayFraction = instance.GetDayFraction();
		if (m_deepKnowledge.Count > 0 && UnityEngine.Random.value < 0.1f && (dayFraction <= 0.15f || dayFraction >= 0.85f))
		{
			return m_deepKnowledge[instance.GetDay() % m_deepKnowledge.Count];
		}
		Vector3 position = base.transform.position;
		string text = EnvMan.instance.GetCurrentEnvironment().m_name;
		if (position.x * position.x > 106300000f - position.z * position.z)
		{
			return Encoding.UTF8.GetString(new byte[12]
			{
				102, 97, 114, 32, 111, 117, 116, 32, 100, 117,
				100, 101
			});
		}
		if (position.y > 4000f && position.y < 5090f && (text.GetHashCode() & 0xFFFFFF) == 16001704)
		{
			return Encoding.UTF8.GetString(new byte[9] { 100, 101, 101, 112, 32, 114, 111, 99, 107 });
		}
		return null;
	}

	public string GetHoverName()
	{
		return m_tameable.GetHoverName();
	}

	public string GetHoverText()
	{
		string text = m_tameable.GetHoverText();
		if ((bool)m_itemStand)
		{
			if (m_itemStand.HaveAttachment())
			{
				text = ((!ZInput.IsGamepadActive()) ? (text + Localization.instance.Localize("\n[<color=yellow><b>$ui_hold $KEY_Use</b></color>] $piece_itemstand_take ( " + m_itemStand.m_currentItemName + " )")) : (text + Localization.instance.Localize("\n<b>$ui_hold $KEY_Use</b> $piece_itemstand_take ( " + m_itemStand.m_currentItemName + " )")));
			}
			text += Localization.instance.Localize("\n[<color=yellow><b>1-8</b></color>] $piece_itemstand_attach");
		}
		return text;
	}

	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (Terminal.m_showTests)
		{
			SetFace(UnityEngine.Random.Range(0, m_materialVariation.m_materials.Count));
		}
		if (hold && (bool)m_itemStand && m_itemStand.HaveAttachment())
		{
			return m_itemStand.Interact(user, hold: true, alt);
		}
		return m_tameable.Interact(user, hold, alt);
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (m_FeedItem != null && item.m_shared.m_name == m_FeedItem.m_itemData.m_shared.m_name)
		{
			if (m_materialVariation.GetMaterial() == 7)
			{
				SetFace(2);
			}
			else if (Terminal.m_showTests)
			{
				SetFace(UnityEngine.Random.Range(0, m_materialVariation.m_materials.Count));
			}
			else if (UnityEngine.Random.value < 0.02f)
			{
				SetFace(4);
			}
			user.GetInventory().RemoveItem(item, 1);
			return true;
		}
		return m_itemStand?.UseItem(user, item) ?? false;
	}

	public void OnPlaced()
	{
		if ((bool)Player.m_localPlayer && Player.m_localPlayer.TryGetUniqueKeyValue("Pet", out var value) && int.TryParse(value, out var result) && result >= 0)
		{
			SetFace(result);
			Player.m_localPlayer.RemoveUniqueKeyValue("Pet");
		}
	}

	public void OnRemoved()
	{
		if ((bool)Player.m_localPlayer && m_materialVariation.GetMaterial() != 7)
		{
			Player.m_localPlayer.AddUniqueKeyValue("Pet", m_materialVariation.GetMaterial().ToString());
		}
	}
}
