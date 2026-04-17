using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

public class StartupMessages : MonoBehaviour
{
	private static StartupMessages s_instance;

	private uint m_shownMessages;

	public static StartupMessages Instance => s_instance;

	public bool StartupMessageDisplayed => m_shownMessages != 0;

	private void Awake()
	{
		if ((object)s_instance != null)
		{
			ZLog.LogError("StartupMessages already had instance!");
			Object.DestroyImmediate(this);
		}
		else
		{
			s_instance = this;
		}
	}

	private void OnDestroy()
	{
		if ((object)s_instance == null)
		{
			ZLog.LogWarning("StartupMessages had no instance!");
		}
		else if (s_instance != this)
		{
			ZLog.LogWarning("StartupMessages had a different instance!");
		}
		else
		{
			s_instance = null;
		}
	}

	public void DisplayStartupMessages()
	{
		PrintGPUInfo();
		DisplayWindowsVulkanAMDCrashMessage();
	}

	private void DisplayWindowsVulkanAMDCrashMessage()
	{
		if (GetGPUVendor() == GPUVendor.AMD && SystemInfo.operatingSystemFamily == OperatingSystemFamily.Windows && SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
		{
			m_shownMessages++;
			UnifiedPopup.Push(new WarningPopup("$menu_vulkancrashwarning_header", "$menu_vulkancrashwarning_text", delegate
			{
				UnifiedPopup.Pop();
				m_shownMessages--;
			}));
		}
	}

	private GPUVendor GetGPUVendor()
	{
		switch (SystemInfo.graphicsDeviceVendorID)
		{
		case 4318:
			return GPUVendor.NVIDIA;
		case 4098:
		case 4130:
			return GPUVendor.AMD;
		case 32902:
			return GPUVendor.Intel;
		default:
			return GPUVendor.Unknown;
		}
	}

	private void PrintGPUInfo()
	{
		string text = null;
		string text2;
		switch (SystemInfo.graphicsDeviceVendorID)
		{
		case 4318:
			text2 = "NVIDIA";
			break;
		case 4098:
		case 4130:
			switch (SystemInfo.graphicsDeviceID)
			{
			case 30032:
			case 30033:
			case 30096:
				text = "RDNA4";
				break;
			case 28639:
				text = "GCN4";
				break;
			}
			text2 = "AMD";
			break;
		case 32902:
			text2 = "Intel";
			break;
		default:
			text2 = "Unknown";
			break;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("GPU Device: " + SystemInfo.graphicsDeviceVendorID.ToString("X4") + ":" + SystemInfo.graphicsDeviceID.ToString("X4") + " (" + text2);
		if (text != null)
		{
			stringBuilder.Append(", " + text);
		}
		stringBuilder.Append(")");
		ZLog.Log(stringBuilder.ToString());
	}
}
