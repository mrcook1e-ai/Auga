using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
	public enum TextType
	{
		Normal,
		Resistant,
		Weak,
		Immune,
		Heal,
		TooHard,
		Blocked,
		Bonus
	}

	private class WorldTextInstance
	{
		public Vector3 m_worldPos;

		public GameObject m_gui;

		public float m_timer;

		public TMP_Text m_textField;

		public float m_duration;
	}

	private static DamageText m_instance;

	public float m_textDuration = 1.5f;

	public float m_maxTextDistance = 30f;

	public int m_largeFontSize = 16;

	public int m_smallFontSize = 8;

	public float m_smallFontDistance = 10f;

	public GameObject m_worldTextBase;

	private List<WorldTextInstance> m_worldTexts = new List<WorldTextInstance>();

	public static DamageText instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		ZRoutedRpc.instance.Register<ZPackage>("RPC_DamageText", RPC_DamageText);
	}

	private void LateUpdate()
	{
		UpdateWorldTexts(Time.deltaTime);
	}

	private void UpdateWorldTexts(float dt)
	{
		WorldTextInstance worldTextInstance = null;
		Camera mainCamera = Utils.GetMainCamera();
		foreach (WorldTextInstance worldText in m_worldTexts)
		{
			worldText.m_timer += dt;
			if (worldText.m_timer > worldText.m_duration && worldTextInstance == null)
			{
				worldTextInstance = worldText;
			}
			worldText.m_worldPos.y += dt;
			float f = Mathf.Clamp01(worldText.m_timer / worldText.m_duration);
			Color color = worldText.m_textField.color;
			color.a = 1f - Mathf.Pow(f, 3f);
			worldText.m_textField.color = color;
			Vector3 position = mainCamera.WorldToScreenPointScaled(worldText.m_worldPos);
			if (position.x < 0f || position.x > (float)Screen.width || position.y < 0f || position.y > (float)Screen.height || position.z < 0f)
			{
				worldText.m_gui.SetActive(value: false);
				continue;
			}
			worldText.m_gui.SetActive(value: true);
			worldText.m_gui.transform.position = position;
		}
		if (worldTextInstance != null)
		{
			Object.Destroy(worldTextInstance.m_gui);
			m_worldTexts.Remove(worldTextInstance);
		}
	}

	private void AddInworldText(TextType type, Vector3 pos, float distance, string text, bool mySelf)
	{
		if (!(text == "0") || m_worldTexts.Count <= 200)
		{
			WorldTextInstance worldTextInstance = new WorldTextInstance();
			worldTextInstance.m_duration = m_textDuration;
			worldTextInstance.m_worldPos = pos + Random.insideUnitSphere * 0.5f;
			worldTextInstance.m_gui = Object.Instantiate(m_worldTextBase, base.transform);
			worldTextInstance.m_textField = worldTextInstance.m_gui.GetComponent<TMP_Text>();
			m_worldTexts.Add(worldTextInstance);
			text = Localization.instance.Localize(text);
			Color color = ((mySelf && type <= TextType.Immune) ? ((!(text == "0")) ? new Color(1f, 0f, 0f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f)) : (type switch
			{
				TextType.Normal => new Color(1f, 1f, 1f, 1f), 
				TextType.Resistant => new Color(0.6f, 0.6f, 0.6f, 1f), 
				TextType.Weak => new Color(1f, 1f, 0f, 1f), 
				TextType.Immune => new Color(0.6f, 0.6f, 0.6f, 1f), 
				TextType.TooHard => new Color(0.8f, 0.7f, 0.7f, 1f), 
				TextType.Bonus => new Color(1f, 0.63f, 0.24f, 1f), 
				TextType.Heal => new Color(0.5f, 1f, 0.5f, 0.7f), 
				_ => Color.white, 
			}));
			worldTextInstance.m_textField.color = color;
			if (distance > m_smallFontDistance)
			{
				worldTextInstance.m_textField.fontSize = m_smallFontSize;
			}
			else
			{
				worldTextInstance.m_textField.fontSize = m_largeFontSize;
			}
			switch (type)
			{
			case TextType.TooHard:
				text = Localization.instance.Localize("$msg_toohard");
				break;
			case TextType.Heal:
				text = "+" + text;
				break;
			case TextType.Blocked:
				text = Localization.instance.Localize("$msg_blocked: " + text);
				break;
			case TextType.Bonus:
				worldTextInstance.m_textField.fontSize *= 1.5f;
				worldTextInstance.m_duration = 3f;
				break;
			}
			worldTextInstance.m_textField.text = text;
			worldTextInstance.m_timer = 0f;
		}
	}

	public void ShowText(HitData.DamageModifier type, Vector3 pos, float dmg, bool player = false)
	{
		TextType type2 = TextType.Normal;
		switch (type)
		{
		case HitData.DamageModifier.Normal:
			type2 = TextType.Normal;
			break;
		case HitData.DamageModifier.Immune:
			type2 = TextType.Immune;
			break;
		case HitData.DamageModifier.SlightlyResistant:
			type2 = TextType.Resistant;
			break;
		case HitData.DamageModifier.Resistant:
			type2 = TextType.Resistant;
			break;
		case HitData.DamageModifier.VeryResistant:
			type2 = TextType.Resistant;
			break;
		case HitData.DamageModifier.SlightlyWeak:
			type2 = TextType.Weak;
			break;
		case HitData.DamageModifier.Weak:
			type2 = TextType.Weak;
			break;
		case HitData.DamageModifier.VeryWeak:
			type2 = TextType.Weak;
			break;
		}
		ShowText(type2, pos, dmg, player);
	}

	public void ShowText(TextType type, Vector3 pos, float dmg, bool player = false)
	{
		ShowText(type, pos, dmg.ToString("0.#", CultureInfo.InvariantCulture), player);
	}

	public void ShowText(TextType type, Vector3 pos, string text, bool player = false)
	{
		ZPackage zPackage = new ZPackage();
		zPackage.Write((int)type);
		zPackage.Write(pos);
		zPackage.Write(text);
		zPackage.Write(player);
		ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "RPC_DamageText", zPackage);
	}

	private void RPC_DamageText(long sender, ZPackage pkg)
	{
		Camera mainCamera = Utils.GetMainCamera();
		if ((bool)mainCamera && !Hud.IsUserHidden())
		{
			TextType type = (TextType)pkg.ReadInt();
			Vector3 vector = pkg.ReadVector3();
			string text = pkg.ReadString();
			bool flag = pkg.ReadBool();
			float num = Vector3.Distance(mainCamera.transform.position, vector);
			if (!(num > m_maxTextDistance))
			{
				bool mySelf = flag && sender == ZNet.GetUID();
				AddInworldText(type, vector, num, text, mySelf);
			}
		}
	}
}
