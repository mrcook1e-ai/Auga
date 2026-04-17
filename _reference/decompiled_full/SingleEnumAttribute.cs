using System;
using UnityEngine;

public class SingleEnumAttribute : PropertyAttribute
{
	public readonly Type m_enumType;

	public SingleEnumAttribute(Type enumType)
	{
		m_enumType = enumType;
	}
}
