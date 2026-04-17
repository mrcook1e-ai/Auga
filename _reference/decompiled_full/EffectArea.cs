using System;
using System.Collections.Generic;
using UnityEngine;

public class EffectArea : MonoBehaviour, IMonoUpdater
{
	[Flags]
	public enum Type : byte
	{
		None = 0,
		Heat = 1,
		Fire = 2,
		PlayerBase = 4,
		Burning = 8,
		Teleport = 0x10,
		NoMonsters = 0x20,
		WarmCozyArea = 0x40,
		PrivateProperty = 0x80
	}

	private KeyValuePair<Bounds, EffectArea> noMonsterArea;

	private KeyValuePair<Bounds, EffectArea> noMonsterCloseToArea;

	private KeyValuePair<Bounds, EffectArea> burnCloseToArea;

	[BitMask(typeof(Type))]
	public Type m_type;

	public string m_statusEffect = "";

	public bool m_playerOnly;

	private int m_statusEffectHash;

	private Collider m_collider;

	private int m_collisions;

	private List<Character> m_collidedWithCharacter = new List<Character>();

	private bool m_isHeatType;

	private static int s_characterMask = 0;

	private static readonly List<EffectArea> s_allAreas = new List<EffectArea>();

	private static readonly List<KeyValuePair<Bounds, EffectArea>> s_noMonsterAreas = new List<KeyValuePair<Bounds, EffectArea>>();

	private static readonly List<KeyValuePair<Bounds, EffectArea>> s_noMonsterCloseToAreas = new List<KeyValuePair<Bounds, EffectArea>>();

	private static readonly List<KeyValuePair<Bounds, EffectArea>> s_BurningAreas = new List<KeyValuePair<Bounds, EffectArea>>();

	private static Collider[] m_tempColliders = new Collider[128];

	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	private void Awake()
	{
		if (!string.IsNullOrEmpty(m_statusEffect))
		{
			m_statusEffectHash = m_statusEffect.GetStableHashCode();
		}
		if (s_characterMask == 0)
		{
			s_characterMask = LayerMask.GetMask("character_trigger");
		}
		m_collider = GetComponent<Collider>();
		m_collider.isTrigger = true;
		if ((m_type & Type.NoMonsters) != Type.None)
		{
			noMonsterArea = new KeyValuePair<Bounds, EffectArea>(m_collider.bounds, this);
			s_noMonsterAreas.Add(noMonsterArea);
			Bounds bounds = m_collider.bounds;
			bounds.Expand(new Vector3(15f, 15f, 15f));
			noMonsterCloseToArea = new KeyValuePair<Bounds, EffectArea>(bounds, this);
			s_noMonsterCloseToAreas.Add(noMonsterCloseToArea);
		}
		if ((m_type & Type.Burning) != Type.None)
		{
			Bounds bounds2 = m_collider.bounds;
			bounds2.Expand(new Vector3(0.25f, 0.25f, 0.25f));
			burnCloseToArea = new KeyValuePair<Bounds, EffectArea>(bounds2, this);
			s_BurningAreas.Add(burnCloseToArea);
		}
		m_isHeatType = m_type.HasFlag(Type.Heat);
		s_allAreas.Add(this);
	}

	private void OnDestroy()
	{
		s_allAreas.Remove(this);
		if (s_noMonsterAreas.Contains(noMonsterArea))
		{
			s_noMonsterAreas.Remove(noMonsterArea);
		}
		if (s_noMonsterCloseToAreas.Contains(noMonsterCloseToArea))
		{
			s_noMonsterCloseToAreas.Remove(noMonsterCloseToArea);
		}
		if (s_BurningAreas.Contains(burnCloseToArea))
		{
			s_BurningAreas.Remove(burnCloseToArea);
		}
	}

	protected virtual void OnEnable()
	{
		Instances.Add(this);
	}

	protected virtual void OnDisable()
	{
		Instances.Remove(this);
	}

	private void OnTriggerEnter(Collider other)
	{
		m_collisions++;
		if (m_isHeatType || m_statusEffectHash != 0)
		{
			Character component = other.GetComponent<Character>();
			if ((bool)component && component.IsOwner() && (!m_playerOnly || component.IsPlayer()) && !m_collidedWithCharacter.Contains(component))
			{
				m_collidedWithCharacter.Add(component);
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		m_collisions--;
		Character component = other.GetComponent<Character>();
		if (component != null)
		{
			m_collidedWithCharacter.Remove(component);
		}
	}

	public void CustomFixedUpdate(float deltaTime)
	{
		if (m_collisions <= 0 || m_collidedWithCharacter.Count == 0 || ZNet.instance == null)
		{
			return;
		}
		foreach (Character item in m_collidedWithCharacter)
		{
			if (m_statusEffectHash != 0)
			{
				item.GetSEMan().AddStatusEffect(m_statusEffectHash, resetTime: true);
			}
			if (m_isHeatType)
			{
				item.OnNearFire(base.transform.position);
			}
		}
	}

	public float GetRadius()
	{
		Collider collider = m_collider;
		if (!(collider is SphereCollider { radius: var radius }))
		{
			if (!(collider is CapsuleCollider { radius: var radius2 }))
			{
				return m_collider.bounds.size.magnitude;
			}
			return radius2;
		}
		return radius;
	}

	public static EffectArea IsPointInsideNoMonsterArea(Vector3 p)
	{
		foreach (KeyValuePair<Bounds, EffectArea> s_noMonsterArea in s_noMonsterAreas)
		{
			if (s_noMonsterArea.Key.Contains(p))
			{
				return s_noMonsterArea.Value;
			}
		}
		return null;
	}

	public static EffectArea IsPointCloseToNoMonsterArea(Vector3 p)
	{
		foreach (KeyValuePair<Bounds, EffectArea> s_noMonsterCloseToArea in s_noMonsterCloseToAreas)
		{
			if (s_noMonsterCloseToArea.Key.Contains(p))
			{
				return s_noMonsterCloseToArea.Value;
			}
		}
		return null;
	}

	public static EffectArea IsPointInsideArea(Vector3 p, Type type, float radius = 0f)
	{
		if (type == Type.Burning && radius.Equals(0.25f))
		{
			return GetBurningAreaPointPlus025(p);
		}
		int num = Physics.OverlapSphereNonAlloc(p, radius, m_tempColliders, s_characterMask);
		for (int i = 0; i < num; i++)
		{
			EffectArea component = m_tempColliders[i].GetComponent<EffectArea>();
			if ((bool)component && (component.m_type & type) != Type.None)
			{
				return component;
			}
		}
		return null;
	}

	public static bool IsPointPlus025InsideBurningArea(Vector3 p)
	{
		foreach (KeyValuePair<Bounds, EffectArea> s_BurningArea in s_BurningAreas)
		{
			if (s_BurningArea.Key.Contains(p))
			{
				return true;
			}
		}
		return false;
	}

	private static EffectArea GetBurningAreaPointPlus025(Vector3 p)
	{
		foreach (KeyValuePair<Bounds, EffectArea> s_BurningArea in s_BurningAreas)
		{
			if (s_BurningArea.Key.Contains(p))
			{
				return s_BurningArea.Value;
			}
		}
		return null;
	}

	public static int GetBaseValue(Vector3 p, float radius)
	{
		int num = 0;
		int num2 = Physics.OverlapSphereNonAlloc(p, radius, m_tempColliders, s_characterMask);
		for (int i = 0; i < num2; i++)
		{
			EffectArea component = m_tempColliders[i].GetComponent<EffectArea>();
			if ((bool)component && (component.m_type & Type.PlayerBase) != Type.None)
			{
				num++;
			}
		}
		return num;
	}

	public static List<EffectArea> GetAllAreas()
	{
		return s_allAreas;
	}
}
