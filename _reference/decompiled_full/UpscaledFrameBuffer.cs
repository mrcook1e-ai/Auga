using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[RequireComponent(typeof(Camera))]
public class UpscaledFrameBuffer : MonoBehaviour
{
	public static bool m_autoTargetResolution = false;

	public static uint m_targetResolutionVertical = uint.MaxValue;

	public static UpscalingAlgorithm m_upscalingAlgorithm = UpscalingAlgorithm.Bilinear;

	private Camera m_camera;

	private Camera m_clearCamera;

	private RenderTexture m_renderTexture;

	private bool m_isUsingScaledRendering;

	private List<FrameBufferScaler> m_subscribers = new List<FrameBufferScaler>();

	public static bool AutomaticRenderScaleSupported()
	{
		return true;
	}

	public void Subscribe(FrameBufferScaler subscriber)
	{
		m_subscribers.Add(subscriber);
		if (m_renderTexture != null)
		{
			subscriber.OnBufferCreated(this, m_renderTexture);
		}
		UpdateCameraTarget();
	}

	public void Unsubscribe(FrameBufferScaler subscriber)
	{
		m_subscribers.Remove(subscriber);
		if (m_renderTexture != null)
		{
			subscriber.OnBufferDestroyed(this);
		}
		UpdateCameraTarget();
	}

	private void Update()
	{
		UpdateCameraTarget();
	}

	private void CreateClearCamera()
	{
		GameObject gameObject = new GameObject();
		gameObject.transform.parent = base.transform;
		m_clearCamera = gameObject.AddComponent<Camera>();
		m_clearCamera.cullingMask = 0;
		m_clearCamera.allowHDR = false;
		m_clearCamera.allowMSAA = false;
		m_clearCamera.renderingPath = RenderingPath.Forward;
		m_clearCamera.clearFlags = CameraClearFlags.Color;
		m_clearCamera.backgroundColor = Color.black;
	}

	private void DestroyClearCameraIfExists()
	{
		if (!(m_clearCamera == null))
		{
			Object.Destroy(m_clearCamera.gameObject);
		}
	}

	private void UpdateCurrentRenderScale()
	{
		if (m_autoTargetResolution)
		{
			uint num = 96u;
			if (Screen.dpi <= (float)num)
			{
				m_targetResolutionVertical = uint.MaxValue;
			}
			else
			{
				m_targetResolutionVertical = (uint)(Screen.height * (int)num) / (uint)Screen.dpi;
			}
		}
	}

	private static Resolution GetHighestSupportedResolution()
	{
		Resolution[] resolutions = Screen.resolutions;
		int num = 0;
		int num2 = resolutions[num].width * resolutions[num].height;
		for (int i = 1; i < resolutions.Length; i++)
		{
			int num3 = resolutions[i].width * resolutions[i].height;
			if (num3 > num2)
			{
				num = i;
				num2 = num3;
			}
		}
		return resolutions[num];
	}

	private void UpdateCameraTarget()
	{
		if (m_camera == null)
		{
			m_camera = GetComponent<Camera>();
		}
		UpdateCurrentRenderScale();
		bool flag = m_targetResolutionVertical < Screen.height && m_subscribers.Count > 0;
		if (flag)
		{
			if (!m_isUsingScaledRendering)
			{
				CreateClearCamera();
			}
			ReassignTextureIfNeeded();
		}
		else if (m_isUsingScaledRendering)
		{
			ReleaseTextureIfExists();
			DestroyClearCameraIfExists();
		}
		m_isUsingScaledRendering = flag;
	}

	private FilterMode GetFilterModeFromUpscalingMode()
	{
		UpscalingAlgorithm upscalingAlgorithm = m_upscalingAlgorithm;
		if (upscalingAlgorithm != UpscalingAlgorithm.Bilinear && upscalingAlgorithm == UpscalingAlgorithm.NearestNeighbor)
		{
			return FilterMode.Point;
		}
		return FilterMode.Bilinear;
	}

	private void ReassignTextureIfNeeded()
	{
		Vector2Int vector2Int = new Vector2Int((int)((uint)((int)m_targetResolutionVertical * Screen.width) / (uint)Screen.height), (int)m_targetResolutionVertical);
		if (vector2Int.x < 8 || vector2Int.y < 8)
		{
			vector2Int = ((vector2Int.y >= vector2Int.x) ? new Vector2Int(8, (int)((uint)(8 * Screen.height) / (uint)Screen.width)) : new Vector2Int((int)((uint)(8 * Screen.width) / (uint)Screen.height), 8));
		}
		if (m_renderTexture == null || vector2Int != new Vector2Int(m_renderTexture.width, m_renderTexture.height) || m_renderTexture.filterMode != GetFilterModeFromUpscalingMode())
		{
			RecreateAndAssignRenderTexture(vector2Int);
		}
	}

	private void RecreateAndAssignRenderTexture(Vector2Int viewportResolution)
	{
		ReleaseTextureIfExists();
		m_renderTexture = new RenderTexture(viewportResolution.x, viewportResolution.y, 24, DefaultFormat.HDR);
		m_renderTexture.Create();
		m_renderTexture.filterMode = GetFilterModeFromUpscalingMode();
		m_camera.targetTexture = m_renderTexture;
		for (int i = 0; i < m_subscribers.Count; i++)
		{
			m_subscribers[i].OnBufferCreated(this, m_renderTexture);
		}
	}

	private void ReleaseTextureIfExists()
	{
		if (!(m_renderTexture == null))
		{
			for (int i = 0; i < m_subscribers.Count; i++)
			{
				m_subscribers[i].OnBufferDestroyed(this);
			}
			m_camera.targetTexture = null;
			m_renderTexture.Release();
			m_renderTexture = null;
		}
	}

	private void OnDestroy()
	{
		ReleaseTextureIfExists();
		DestroyClearCameraIfExists();
	}
}
