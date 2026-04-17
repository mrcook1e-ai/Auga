using UnityEngine;
using UnityEngine.UI;

public class CaptionArrow : MonoBehaviour
{
	public float m_fadeTime = 1.5f;

	private Vector3 m_sfxPosition;

	private float m_timer;

	public RawImage m_imageComponent;

	private Color m_color;

	private ClosedCaptions.CaptionType m_type;

	private float m_alpha;

	public AnimationCurve m_distanceCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

	private static readonly int s_CaptionDistance = Shader.PropertyToID("_CaptionDistance");

	public void Setup(ClosedCaptions.CaptionType type, Vector3 position, float distanceFactor = 0f)
	{
		m_alpha = m_imageComponent.color.a;
		m_color = ClosedCaptions.Instance.GetCaptionColor(type);
		m_color.a = m_alpha;
		m_imageComponent.color = m_color;
		m_timer = m_fadeTime;
		m_sfxPosition = position;
		RotateArrow();
		m_imageComponent.material = new Material(m_imageComponent.material);
		m_imageComponent.material.SetFloat(s_CaptionDistance, m_distanceCurve.Evaluate(distanceFactor));
	}

	private void Update()
	{
		m_timer -= Time.deltaTime;
		if (m_timer <= 0f)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		m_color.a = m_alpha * Mathf.Clamp01(m_timer / m_fadeTime);
		m_color.a = Mathf.SmoothStep(0f, 1f, m_color.a);
		m_imageComponent.color = m_color;
		RotateArrow();
	}

	public void RotateArrow()
	{
		Vector3 position = AudioMan.instance.GetActiveAudioListener().transform.position;
		position.y = m_sfxPosition.y;
		Vector3 normalized = Vector3.ProjectOnPlane(Utils.GetMainCamera().transform.forward, Vector3.up).normalized;
		Vector3 to = position.DirTo(m_sfxPosition);
		float num = Vector3.SignedAngle(normalized, to, Vector3.up);
		base.transform.localEulerAngles = new Vector3(0f, 0f, 0f - num);
	}
}
