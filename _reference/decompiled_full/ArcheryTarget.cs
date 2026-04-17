using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ArcheryTarget : MonoBehaviour, IHitProjectile, Hoverable, Interactable
{
	public string m_name;

	public GameObject m_center;

	public float m_targetSize = 1f;

	public int m_points = 10;

	public int m_scoreListSize = 5;

	public float m_projectileStayTTL = 60f;

	public bool m_killProjectile;

	public float m_raiseSkillMultiplier = 1f;

	public GameObject m_returnPoint;

	public List<ItemDrop> m_returnAmmo = new List<ItemDrop>();

	[Header("Effects")]
	public List<ProjectileTypeEffect> m_projectileHitEffects = new List<ProjectileTypeEffect>();

	public EffectList m_bullsEyeEffect = new EffectList();

	public EffectList m_doubleBullsEyeEffect = new EffectList();

	public EffectList m_fullBullsEyeEffect = new EffectList();

	private Vector3 m_lastHitPos;

	private ZNetView m_nview;

	private WearNTear m_wnt;

	private byte[] m_lastScores;

	private static StringBuilder m_sb = new StringBuilder();

	private void Start()
	{
		m_nview = GetComponentInParent<ZNetView>();
		m_wnt = GetComponentInParent<WearNTear>();
		if ((bool)m_wnt)
		{
			WearNTear wnt = m_wnt;
			wnt.m_onDestroyed = (Action)Delegate.Combine(wnt.m_onDestroyed, new Action(OnDestroyed));
		}
		m_lastScores = new byte[m_scoreListSize];
		m_nview.Register<int, int, Vector3>("RPC_ProjectileHit", RPC_ProjectileHit);
		m_nview.Register("RPC_DropArrows", RPC_DropArrows);
	}

	private void OnDrawGizmos()
	{
		if ((bool)m_center)
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(m_center.transform.position, m_targetSize);
			Gizmos.DrawWireSphere(m_lastHitPos, 0.02f);
			Gizmos.DrawWireSphere(m_lastHitPos, 0.25f);
		}
	}

	public bool OnProjectileHit(Character owner, ItemDrop.ItemData weapon, Projectile projectile, Collider collider, Vector3 hitPoint, bool water, Vector3 normal)
	{
		m_lastHitPos = hitPoint;
		if (m_projectileStayTTL >= 0f)
		{
			projectile.SetStayTTL(m_projectileStayTTL);
		}
		float num = Vector3.Distance(m_center.transform.position, m_lastHitPos) / m_targetSize;
		int num2 = Mathf.Max(0, Mathf.CeilToInt((1f - num) * (float)m_points));
		Player.m_localPlayer.Message(MessageHud.MessageType.Center, num2.ToString());
		int num3 = FindAmmoIndex(projectile);
		if ((bool)m_nview)
		{
			if (m_nview.IsOwner())
			{
				ProjectileHit(num2, num3, hitPoint);
			}
			else
			{
				m_nview.InvokeRPC("RPC_ProjectileHit", num2, num3, hitPoint);
			}
		}
		foreach (ProjectileTypeEffect projectileHitEffect in m_projectileHitEffects)
		{
			if (projectile.m_type.HasFlag(projectileHitEffect.m_type))
			{
				projectileHitEffect.m_effect.Create(hitPoint, base.transform.rotation);
			}
		}
		if (m_raiseSkillMultiplier > 0f && owner != null)
		{
			owner.RaiseSkill(projectile.m_skill, projectile.m_raiseSkillAmount * m_raiseSkillMultiplier * (1f - num));
		}
		return !m_killProjectile;
	}

	private void RPC_ProjectileHit(long sender, int points, int ammoIndex, Vector3 hitPoint)
	{
		ProjectileHit(points, ammoIndex, hitPoint);
	}

	private void ProjectileHit(int points, int ammoIndex, Vector3 hitPoint)
	{
		if (m_nview == null || !m_nview.IsOwner())
		{
			return;
		}
		ZDO zDO = m_nview.GetZDO();
		int num = zDO.GetInt(ZDOVars.s_dataCount);
		zDO.Set(ZDOVars.s_dataCount, num + points);
		int num2 = zDO.GetInt(ZDOVars.s_hitPoint);
		zDO.Set(ZDOVars.s_hitPoint, num2 + 1);
		if (m_scoreListSize > 0)
		{
			m_lastScores = zDO.GetByteArray(ZDOVars.s_data, m_lastScores);
			for (int num3 = m_lastScores.Length - 1; num3 >= 1; num3--)
			{
				m_lastScores[num3] = m_lastScores[num3 - 1];
			}
			m_lastScores[0] = (byte)points;
			zDO.Set(ZDOVars.s_data, m_lastScores);
		}
		zDO.Set(ZDOVars.s_ammoType + ammoIndex, zDO.GetInt(ZDOVars.s_ammoType + ammoIndex) + 1);
		if (points != m_points)
		{
			return;
		}
		bool flag = m_scoreListSize > 0;
		for (int i = 0; i < m_lastScores.Length; i++)
		{
			if (m_lastScores[i] != m_points)
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			m_fullBullsEyeEffect.Create(hitPoint, base.transform.rotation);
		}
		else if (m_scoreListSize > 0 && m_lastScores[1] == m_points)
		{
			m_doubleBullsEyeEffect.Create(hitPoint, base.transform.rotation);
		}
		else
		{
			m_bullsEyeEffect.Create(hitPoint, base.transform.rotation);
		}
	}

	public int FindAmmoIndex(Projectile projectile)
	{
		string prefabName = Utils.GetPrefabName(projectile.name);
		for (int i = 0; i < m_returnAmmo.Count; i++)
		{
			if (m_returnAmmo[i].m_itemData.m_shared.m_attack.m_attackProjectile.name == prefabName)
			{
				return i;
			}
		}
		return -1;
	}

	public string GetHoverText()
	{
		if (m_nview == null || !m_nview.IsValid())
		{
			return "";
		}
		ZDO zDO = m_nview.GetZDO();
		m_sb.Clear();
		m_sb.Append(GetHoverName());
		m_lastScores = zDO.GetByteArray(ZDOVars.s_data, m_lastScores);
		if (m_scoreListSize == 0 || m_lastScores[0] != 0)
		{
			if (m_scoreListSize > 0)
			{
				m_sb.Append("\n$piece_archerytarget_lastscores: ");
				for (int i = 0; i < m_lastScores.Length; i++)
				{
					m_sb.Append(m_lastScores[i]);
					if (i + 1 >= m_lastScores.Length || m_lastScores[i + 1] == 0)
					{
						break;
					}
					m_sb.Append(", ");
				}
			}
			int num = zDO.GetInt(ZDOVars.s_hitPoint);
			if (num > m_scoreListSize)
			{
				m_sb.Append("..");
			}
			m_sb.Append($"\n$piece_archerytarget_total: {zDO.GetInt(ZDOVars.s_dataCount)} ( {num} $piece_archerytarget_hits )\n$piece_archerytarget_reset: [<color=yellow><b>$KEY_Use</b></color>]");
		}
		return Localization.instance.Localize(m_sb.ToString());
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (m_nview.GetZDO().GetInt(ZDOVars.s_dataCount) == 0)
		{
			return false;
		}
		if ((bool)m_nview && m_nview.IsOwner())
		{
			DropArrows();
		}
		else
		{
			RemoveVisualArrows();
			m_nview.InvokeRPC("RPC_DropArrows");
		}
		return true;
	}

	public void RPC_DropArrows(long sender)
	{
		DropArrows();
	}

	public void RemoveVisualArrows()
	{
		Projectile[] array = UnityEngine.Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None);
		foreach (Projectile projectile in array)
		{
			if (Vector3.Distance(projectile.transform.position, m_center.transform.position) < m_targetSize)
			{
				projectile.SetStayTTL(0f);
			}
		}
	}

	public void DropArrows()
	{
		if (m_nview == null || !m_nview.IsOwner())
		{
			return;
		}
		RemoveVisualArrows();
		ZDO zDO = m_nview.GetZDO();
		for (int i = 0; i < m_returnAmmo.Count; i++)
		{
			int num = zDO.GetInt(ZDOVars.s_ammoType + i);
			if (num > 0)
			{
				for (int j = 0; j < num; j++)
				{
					UnityEngine.Object.Instantiate(m_returnAmmo[i], m_returnPoint.transform.position, UnityEngine.Random.rotation);
				}
				zDO.Set(ZDOVars.s_ammoType + i, 0);
			}
		}
		zDO.Set(ZDOVars.s_dataCount, 0);
		zDO.Set(ZDOVars.s_hitPoint, 0);
		if (m_scoreListSize > 0)
		{
			for (int k = 0; k < m_lastScores.Length; k++)
			{
				m_lastScores[k] = 0;
			}
			zDO.Set(ZDOVars.s_data, m_lastScores);
		}
	}

	private void OnDestroyed()
	{
		DropArrows();
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		return false;
	}
}
