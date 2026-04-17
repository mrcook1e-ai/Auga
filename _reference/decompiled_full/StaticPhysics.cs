using UnityEngine;

public class StaticPhysics : SlowUpdate
{
	public bool m_pushUp = true;

	public bool m_fall = true;

	public bool m_checkSolids;

	public float m_fallCheckRadius;

	private ZNetView m_nview;

	private const float m_fallSpeed = 4f;

	private const float m_fallStep = 0.05f;

	private float m_updateTime;

	private bool m_falling;

	private int m_activeArea;

	public bool IsFalling => m_falling;

	public override void Awake()
	{
		base.Awake();
		m_nview = GetComponent<ZNetView>();
		m_updateTime = Time.time + 20f;
		m_activeArea = ZoneSystem.instance.m_activeArea;
	}

	private bool ShouldUpdate(float time)
	{
		return time > m_updateTime;
	}

	public override void SUpdate(float time, Vector2i referenceZone)
	{
		if (!m_falling && ShouldUpdate(time) && !ZNetScene.OutsideActiveArea(base.transform.position, referenceZone, m_activeArea))
		{
			if (m_fall)
			{
				CheckFall();
			}
			if (m_pushUp)
			{
				PushUp();
			}
		}
	}

	private void CheckFall()
	{
		float fallHeight = GetFallHeight();
		if (base.transform.position.y > fallHeight + 0.05f)
		{
			Fall();
		}
	}

	private float GetFallHeight()
	{
		if (m_checkSolids)
		{
			if (ZoneSystem.instance.GetSolidHeight(base.transform.position, m_fallCheckRadius, out var height, base.transform))
			{
				return height;
			}
			return base.transform.position.y;
		}
		if (ZoneSystem.instance.GetGroundHeight(base.transform.position, out var height2))
		{
			return height2;
		}
		return base.transform.position.y;
	}

	private void Fall()
	{
		m_falling = true;
		base.gameObject.isStatic = false;
		InvokeRepeating("FallUpdate", 0.05f, 0.05f);
	}

	private void FallUpdate()
	{
		float fallHeight = GetFallHeight();
		Vector3 position = base.transform.position;
		position.y -= 0.2f;
		if (position.y <= fallHeight)
		{
			position.y = fallHeight;
			StopFalling();
		}
		base.transform.position = position;
		if ((bool)m_nview && m_nview.IsValid() && m_nview.IsOwner())
		{
			m_nview.GetZDO().SetPosition(base.transform.position);
		}
	}

	private void StopFalling()
	{
		base.gameObject.isStatic = true;
		m_falling = false;
		CancelInvoke("FallUpdate");
	}

	private void PushUp()
	{
		if (ZoneSystem.instance.GetGroundHeight(base.transform.position, out var height) && base.transform.position.y < height - 0.05f)
		{
			base.gameObject.isStatic = false;
			Vector3 position = base.transform.position;
			position.y = height;
			base.transform.position = position;
			base.gameObject.isStatic = true;
			if ((bool)m_nview && m_nview.IsValid() && m_nview.IsOwner())
			{
				m_nview.GetZDO().SetPosition(base.transform.position);
			}
		}
	}
}
