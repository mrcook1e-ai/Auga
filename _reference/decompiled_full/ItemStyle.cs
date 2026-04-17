using UnityEngine;

public class ItemStyle : MonoBehaviour, IEquipmentVisual
{
	public void Setup(int style)
	{
		MaterialMan.instance.SetValue(base.gameObject, ShaderProps._Style, style);
	}
}
