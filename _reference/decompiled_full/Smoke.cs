using System.Collections.Generic;
using UnityEngine;

public class Smoke : MonoBehaviour, IMonoUpdater
{
	public Vector3 m_vel = Vector3.up;

	public float m_randomVel = 0.1f;

	public float m_force = 0.1f;

	public float m_ttl = 10f;

	public float m_fadetime = 3f;

	private Rigidbody m_body;

	private float m_time;

	private float m_fadeTimer = -1f;

	private bool m_added;

	private ParticleSystem.Particle m_renderParticle;

	private static readonly List<Smoke> s_smoke = new List<Smoke>();

	public Vector3Int RenderChunk { get; set; } = Vector3Int.zero;

	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();

	private void Awake()
	{
		s_smoke.Add(this);
		m_added = true;
		m_body = GetComponent<Rigidbody>();
		m_body.maxDepenetrationVelocity = 1f;
		m_vel = Vector3.up + Quaternion.Euler(0f, Random.Range(0, 360), 0f) * Vector3.forward * m_randomVel;
		SetupParticle();
	}

	private void SetupParticle()
	{
		float num = Random.Range(0f, 360f);
		m_renderParticle = new ParticleSystem.Particle
		{
			angularVelocity = 0f,
			angularVelocity3D = Vector3.zero,
			axisOfRotation = new Vector3(0f, 0f, 1f),
			position = base.transform.position,
			randomSeed = (uint)Random.Range(int.MinValue, int.MaxValue),
			remainingLifetime = m_ttl + m_fadetime,
			startLifetime = m_ttl,
			rotation = num,
			rotation3D = new Vector3(0f, 0f, num),
			velocity = Vector3.zero
		};
	}

	public ParticleSystem.Particle GetParticleValues()
	{
		if (m_fadeTimer < 0f)
		{
			m_renderParticle.remainingLifetime = m_ttl - m_time;
		}
		else
		{
			m_renderParticle.remainingLifetime = m_fadetime - m_fadeTimer;
		}
		m_renderParticle.position = base.transform.position;
		return m_renderParticle;
	}

	public float GetAlpha()
	{
		float a = Utils.SmoothStep(0f, 1f, Mathf.Clamp01(m_time / 2f));
		float b = Utils.SmoothStep(0f, 1f, 1f - Mathf.Clamp01(m_fadeTimer / m_fadetime));
		return Mathf.Min(a, b);
	}

	private void OnEnable()
	{
		Instances.Add(this);
		SmokeRenderer.Instance.RegisterSmoke(this);
	}

	private void OnDisable()
	{
		SmokeRenderer.Instance.UnregisterSmoke(this);
		Instances.Remove(this);
	}

	private void OnDestroy()
	{
		if (m_added)
		{
			s_smoke.Remove(this);
			m_added = false;
		}
	}

	public void StartFadeOut()
	{
		if (!(m_fadeTimer >= 0f))
		{
			if (m_added)
			{
				s_smoke.Remove(this);
				m_added = false;
			}
			m_renderParticle.startLifetime = m_time + m_fadetime;
			m_fadeTimer = 0f;
		}
	}

	public static int GetTotalSmoke()
	{
		return s_smoke.Count;
	}

	public static void FadeOldest()
	{
		if (s_smoke.Count != 0)
		{
			s_smoke[0].StartFadeOut();
		}
	}

	public static void FadeMostDistant()
	{
		if (s_smoke.Count == 0)
		{
			return;
		}
		Camera mainCamera = Utils.GetMainCamera();
		if (mainCamera == null)
		{
			return;
		}
		Vector3 position = mainCamera.transform.position;
		int num = -1;
		float num2 = 0f;
		for (int i = 0; i < s_smoke.Count; i++)
		{
			float num3 = Vector3.Distance(s_smoke[i].transform.position, position);
			if (num3 > num2)
			{
				num = i;
				num2 = num3;
			}
		}
		if (num != -1)
		{
			s_smoke[num].StartFadeOut();
		}
	}

	public void CustomUpdate(float deltaTime, float time)
	{
		m_time += deltaTime;
		if (m_time > m_ttl && m_fadeTimer < 0f)
		{
			StartFadeOut();
		}
		float num = 1f - Mathf.Clamp01(m_time / m_ttl);
		m_body.mass = num * num;
		Vector3 linearVelocity = m_body.linearVelocity;
		Vector3 vel = m_vel;
		vel.y *= num;
		Vector3 vector = vel - linearVelocity;
		m_body.AddForce(vector * (m_force * deltaTime), ForceMode.VelocityChange);
		if (m_fadeTimer >= 0f)
		{
			m_fadeTimer += deltaTime;
			Mathf.Clamp01(m_fadeTimer / m_fadetime);
			if (m_fadeTimer >= m_fadetime)
			{
				Object.Destroy(base.gameObject);
			}
		}
	}
}
