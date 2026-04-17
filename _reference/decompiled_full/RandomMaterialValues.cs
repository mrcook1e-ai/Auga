using System;
using System.Collections.Generic;
using UnityEngine;

public class RandomMaterialValues : MonoBehaviour
{
	[Serializable]
	public abstract class MaterialVariationProperty<T>
	{
		public List<string> m_propertyNames;

		protected System.Random m_random;

		public abstract T GetValue(int seed);
	}

	[Serializable]
	public class VectorVariationProperty : MaterialVariationProperty<Vector4>
	{
		public Vector4 m_minimum;

		public Vector4 m_maximum;

		public override Vector4 GetValue(int seed)
		{
			m_random = new System.Random(seed);
			return new Vector4
			{
				x = Mathf.Lerp(m_minimum.x, m_maximum.x, (float)m_random.NextDouble()),
				y = Mathf.Lerp(m_minimum.y, m_maximum.y, (float)m_random.NextDouble()),
				z = Mathf.Lerp(m_minimum.z, m_maximum.z, (float)m_random.NextDouble()),
				w = Mathf.Lerp(m_minimum.w, m_maximum.w, (float)m_random.NextDouble())
			};
		}
	}

	public List<VectorVariationProperty> m_vectorProperties = new List<VectorVariationProperty>();

	private ZNetView m_nview;

	private int m_randomSeed = -1;

	private string m_matName;

	private bool m_isSet;

	private Piece m_piece;

	private int m_checks;

	private static readonly string s_randSeedString = "RandMatSeed";

	private void Start()
	{
		m_nview = GetComponentInParent<ZNetView>();
		m_piece = GetComponentInParent<Piece>();
		if (!m_nview)
		{
			ZLog.LogError("Missing nview on '" + base.transform.gameObject.name + "'");
		}
		InvokeRepeating("CheckMaterial", 0f, 0.2f);
	}

	private void CheckMaterial()
	{
		if (((!m_isSet && m_randomSeed < 0) || (m_isSet && (!m_piece || !Player.IsPlacementGhost(m_piece.gameObject)))) && (bool)m_nview && m_nview.GetZDO() != null)
		{
			m_randomSeed = m_nview.GetZDO().GetInt(s_randSeedString, -1);
			if (m_randomSeed < 0 && m_nview.IsOwner())
			{
				m_nview.GetZDO().Set(s_randSeedString, UnityEngine.Random.Range(0, 12345));
			}
			if (m_randomSeed >= 0)
			{
				for (int i = 0; i < m_vectorProperties.Count; i++)
				{
					VectorVariationProperty vectorVariationProperty = m_vectorProperties[i];
					foreach (string propertyName in vectorVariationProperty.m_propertyNames)
					{
						MaterialMan.instance.SetValue(base.gameObject, Shader.PropertyToID(propertyName), vectorVariationProperty.GetValue(m_randomSeed + i));
					}
				}
				m_isSet = true;
			}
		}
		m_checks++;
		if (m_checks >= 5)
		{
			CancelInvoke("CheckMaterial");
		}
	}
}
