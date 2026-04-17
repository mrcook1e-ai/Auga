using System.Collections;
using UnityEngine;

namespace Valheim.UI;

public class GroupElement : RadialMenuElement
{
	protected Coroutine m_colorChangeCoroutine;

	public void Init(IRadialConfig config, IRadialConfig backConfig, RadialBase radial)
	{
		if (config == null)
		{
			base.Name = "";
			base.Interact = null;
		}
		else
		{
			base.Name = config.LocalizedName;
			base.Interact = delegate
			{
				radial.QueuedOpen(config, backConfig);
				return true;
			};
		}
		m_icon.gameObject.SetActive(config.Sprite != null);
		m_icon.sprite = config.Sprite;
	}

	public virtual void ChangeToSelectColor()
	{
		if (m_colorChangeCoroutine != null)
		{
			Hud.instance.StopCoroutine(m_colorChangeCoroutine);
		}
		m_colorChangeCoroutine = Hud.instance.StartCoroutine(ChangeColor(base.BackgroundMaterial.GetColor("_SelectedColor"), 0.1f));
	}

	public virtual void ChangeToDeselectColor()
	{
		if (m_colorChangeCoroutine != null)
		{
			Hud.instance.StopCoroutine(m_colorChangeCoroutine);
		}
		m_colorChangeCoroutine = Hud.instance.StartCoroutine(ChangeColor(Color.white, 0.1f));
	}

	protected IEnumerator ChangeColor(Color targetColor, float speed)
	{
		if (!(m_icon == null))
		{
			float alpha = 0f;
			float duration = 0f;
			Color startColor = m_icon.color;
			while (m_icon != null && duration <= speed + 0.1f)
			{
				m_icon.color = Color.Lerp(startColor, targetColor, alpha);
				duration += Time.deltaTime;
				alpha = Mathf.Clamp01(duration / speed);
				yield return null;
			}
		}
	}

	protected void OnDisable()
	{
		if (m_colorChangeCoroutine != null)
		{
			Hud.instance.StopCoroutine(m_colorChangeCoroutine);
		}
	}
}
