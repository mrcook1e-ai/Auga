using System;
using System.Collections.Generic;
using UnityEngine;

public class Feast : MonoBehaviour, Hoverable, Interactable
{
	[Serializable]
	public class FeastLevel
	{
		public GameObject m_onAboveEquals;

		public GameObject m_onBelow;

		public float m_threshold;

		public float m_thresholdBelowMax;
	}

	public int m_eatStacks = 5;

	public float m_useDistance = 2f;

	public ItemDrop m_foodItem;

	public List<FeastLevel> m_feastParts = new List<FeastLevel>();

	public EffectList m_eatEffect = new EffectList();

	private ZNetView m_nview;

	private void Start()
	{
		m_nview = GetComponent<ZNetView>();
		if ((bool)m_nview)
		{
			m_nview.Register("RPC_TryEat", RPC_TryEat);
			m_nview.Register("RPC_OnEat", RPC_OnEat);
			m_nview.Register("RPC_EatConfirmation", RPC_EatConfirmation);
		}
		UpdateVisual();
		if (!m_foodItem)
		{
			m_foodItem = base.gameObject.GetComponent<ItemDrop>();
		}
		if (!m_foodItem)
		{
			ZLog.LogError("Feast created without separate food item or being a food itself!");
		}
	}

	public void UpdateVisual()
	{
		float stackPercentige = GetStackPercentige();
		for (int num = m_feastParts.Count - 1; num >= 0; num--)
		{
			FeastLevel feastLevel = m_feastParts[num];
			if ((bool)feastLevel.m_onAboveEquals)
			{
				feastLevel.m_onAboveEquals.SetActive(stackPercentige >= feastLevel.m_threshold);
			}
			if ((bool)feastLevel.m_onBelow)
			{
				feastLevel.m_onBelow.SetActive(stackPercentige < feastLevel.m_threshold && stackPercentige >= feastLevel.m_thresholdBelowMax);
			}
		}
	}

	private void RPC_TryEat(long sender)
	{
		if (!m_nview.IsOwner())
		{
			return;
		}
		int stack = GetStack();
		ZLog.Log($"We eat a stack - starting with {stack}");
		if (stack > 0)
		{
			if (stack <= 1)
			{
				m_nview.GetZDO().Set(ZDOVars.s_value, -1);
			}
			else
			{
				m_nview.GetZDO().Set(ZDOVars.s_value, stack - 1);
			}
			ZLog.Log($"Stack is now {GetStack()}");
			m_nview.InvokeRPC(ZNetView.Everybody, "RPC_OnEat");
			m_nview.InvokeRPC(sender, "RPC_EatConfirmation");
			UpdateVisual();
		}
	}

	private void RPC_EatConfirmation(long sender)
	{
		if ((bool)m_foodItem.m_itemData.m_shared.m_consumeStatusEffect)
		{
			Player.m_localPlayer.GetSEMan().AddStatusEffect(m_foodItem.m_itemData.m_shared.m_consumeStatusEffect, resetTime: true);
		}
		if (m_foodItem.m_itemData.m_shared.m_food > 0f)
		{
			Player.m_localPlayer.EatFood(m_foodItem.m_itemData);
		}
	}

	public void RPC_OnEat(long sender)
	{
		m_eatEffect.Create(base.transform.position, base.transform.rotation);
		UpdateVisual();
	}

	public int GetStack()
	{
		if ((bool)m_nview && m_nview.IsValid())
		{
			return m_nview.GetZDO().GetInt(ZDOVars.s_value, m_eatStacks);
		}
		return m_eatStacks;
	}

	public float GetStackPercentige()
	{
		return (float)Mathf.Max(GetStack(), 0) / (float)m_eatStacks;
	}

	private bool InUseDistance(Humanoid human)
	{
		return Vector3.Distance(human.transform.position, base.transform.position) < m_useDistance;
	}

	public string GetHoverText()
	{
		int stack = GetStack();
		if (stack <= 0)
		{
			return "";
		}
		if (!InUseDistance(Player.m_localPlayer))
		{
			return Localization.instance.Localize("<color=#888888>$piece_toofar</color>");
		}
		return Localization.instance.Localize(GetHoverName() + $"\n[<color=yellow><b>$KEY_Use</b></color>] $item_eat ( {stack}/{m_eatStacks} )");
	}

	public string GetHoverName()
	{
		return m_foodItem.m_itemData.m_shared.m_name;
	}

	public bool Interact(Humanoid human, bool hold, bool alt)
	{
		if (hold)
		{
			return false;
		}
		Player player = human as Player;
		if (!player || !InUseDistance(player))
		{
			return false;
		}
		if (GetStack() <= 0)
		{
			return false;
		}
		if (!player.CanConsumeItem(m_foodItem.m_itemData, checkWorldLevel: true))
		{
			return false;
		}
		m_nview.InvokeRPC("RPC_TryEat");
		return true;
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}
}
