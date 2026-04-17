using System.Collections.Generic;
using UnityEngine;

public class WearNTearUpdater : MonoBehaviour
{
	private static readonly int s_ashlandsWearTexture = Shader.PropertyToID("_AshlandsWearTexture");

	private int m_index;

	private float m_sleepUntil;

	private float m_sleepUntilNext;

	public Texture3D m_ashlandsWearTexture;

	private int m_updatesPerFrame = 50;

	private const int c_UpdatesPerFrame = 50;

	private const float c_WearNTearTime = 1f;

	private void Start()
	{
		m_sleepUntilNext = (m_sleepUntil = Time.time + 1f);
	}

	private void Update()
	{
		float time = Time.time;
		float deltaTime = Time.deltaTime;
		if (!(time < m_sleepUntil))
		{
			UpdateWearNTear(deltaTime, time);
		}
	}

	private void UpdateWearNTear(float deltaTime, float time)
	{
		List<WearNTear> allInstances = WearNTear.GetAllInstances();
		if (m_sleepUntilNext.Equals(m_sleepUntil))
		{
			m_sleepUntilNext = time + 1f;
			Shader.SetGlobalTexture(s_ashlandsWearTexture, m_ashlandsWearTexture);
			foreach (WearNTear item in allInstances)
			{
				if (item.enabled)
				{
					item.UpdateCover(deltaTime);
				}
			}
			{
				foreach (WearNTear item2 in allInstances)
				{
					item2.UpdateAshlandsMaterialValues(time);
				}
				return;
			}
		}
		int num = m_index;
		for (int i = 0; i < m_updatesPerFrame; i++)
		{
			if (allInstances.Count == 0)
			{
				break;
			}
			if (num >= allInstances.Count)
			{
				break;
			}
			WearNTear wearNTear = allInstances[num];
			if (wearNTear.enabled)
			{
				wearNTear.UpdateWear(time);
			}
			num++;
		}
		m_index = ((num < allInstances.Count) ? num : 0);
		if (m_index != 0)
		{
			return;
		}
		float num2 = m_sleepUntilNext - time;
		if (!(Utils.Abs(num2) < 0.1f))
		{
			if (num2 < -0.8f)
			{
				m_updatesPerFrame += 20;
			}
			else if (num2 < -0.4f)
			{
				m_updatesPerFrame += 15;
			}
			else if (num2 < -0.2f)
			{
				m_updatesPerFrame += 10;
			}
			else if (num2 < 0f)
			{
				m_updatesPerFrame += 5;
			}
			else if (num2 > 0.8f)
			{
				m_updatesPerFrame -= 20;
			}
			else if (num2 > 0.6f)
			{
				m_updatesPerFrame -= 15;
			}
			else if (num2 > 0.3f)
			{
				m_updatesPerFrame -= 10;
			}
			else if (num2 > 0.2f)
			{
				m_updatesPerFrame -= 5;
			}
		}
		m_sleepUntil = m_sleepUntilNext;
		m_updatesPerFrame = Mathf.Max(m_updatesPerFrame, 5);
		m_updatesPerFrame = Mathf.Min(m_updatesPerFrame, 100);
	}
}
