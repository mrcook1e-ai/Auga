using UnityEngine;

public class StaticRotation : MonoBehaviour
{
	public bool m_NotIfBuildGhost = true;

	private ZNetView m_nview;

	private float m_rotation;

	private bool m_disabled;

	private void Start()
	{
		int disabled;
		if (m_NotIfBuildGhost)
		{
			Piece componentInParent = GetComponentInParent<Piece>();
			if ((object)componentInParent != null)
			{
				disabled = (Player.IsPlacementGhost(componentInParent.gameObject) ? 1 : 0);
				goto IL_0021;
			}
		}
		disabled = 0;
		goto IL_0021;
		IL_0021:
		m_disabled = (byte)disabled != 0;
		m_nview = GetComponentInParent<ZNetView>();
		if (!m_nview)
		{
			return;
		}
		ZDO zDO = m_nview.GetZDO();
		if (zDO != null && zDO.IsValid())
		{
			m_rotation = zDO.GetFloat(ZDOVars.s_tiltrot);
			if (m_rotation == 0f)
			{
				m_rotation = base.transform.rotation.eulerAngles.y;
				zDO.Set(ZDOVars.s_tiltrot, m_rotation);
			}
		}
	}

	private void Update()
	{
		if (!m_disabled)
		{
			Vector3 eulerAngles = base.transform.rotation.eulerAngles;
			base.transform.rotation = Quaternion.Euler(eulerAngles.x, m_rotation, eulerAngles.z);
		}
	}
}
