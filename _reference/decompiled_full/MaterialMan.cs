using System;
using System.Collections.Generic;
using UnityEngine;

public class MaterialMan : MonoBehaviour
{
	private class PropertyContainer
	{
		private List<Renderer> m_assignedRenderers = new List<Renderer>();

		private Dictionary<int, ShaderPropertyBase> m_shaderProperties = new Dictionary<int, ShaderPropertyBase>();

		private MaterialPropertyBlock m_propertyBlock;

		public Action<PropertyContainer> MarkDirty;

		public PropertyContainer(GameObject go, MaterialPropertyBlock block)
		{
			MeshRenderer[] componentsInChildren = go.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
			if (componentsInChildren != null)
			{
				m_assignedRenderers.AddRange(componentsInChildren);
			}
			SkinnedMeshRenderer[] componentsInChildren2 = go.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
			if (componentsInChildren2 != null)
			{
				m_assignedRenderers.AddRange(componentsInChildren2);
			}
			m_propertyBlock = block;
		}

		public void UpdateBlock()
		{
			m_propertyBlock.Clear();
			foreach (KeyValuePair<int, ShaderPropertyBase> shaderProperty in m_shaderProperties)
			{
				if (shaderProperty.Value.PropertyType == typeof(int))
				{
					m_propertyBlock.SetInt(shaderProperty.Key, (shaderProperty.Value as ShaderProperty<int>).Get());
				}
				else if (shaderProperty.Value.PropertyType == typeof(float))
				{
					m_propertyBlock.SetFloat(shaderProperty.Key, (shaderProperty.Value as ShaderProperty<float>).Get());
				}
				else if (shaderProperty.Value.PropertyType == typeof(float[]))
				{
					m_propertyBlock.SetFloatArray(shaderProperty.Key, (shaderProperty.Value as ShaderProperty<float[]>).Get());
				}
				else if (shaderProperty.Value.PropertyType == typeof(Color))
				{
					m_propertyBlock.SetColor(shaderProperty.Key, (shaderProperty.Value as ShaderProperty<Color>).Get());
				}
				else if (shaderProperty.Value.PropertyType == typeof(Vector4))
				{
					m_propertyBlock.SetVector(shaderProperty.Key, (shaderProperty.Value as ShaderProperty<Vector4>).Get());
				}
				else if (shaderProperty.Value.PropertyType == typeof(Vector4[]))
				{
					m_propertyBlock.SetVectorArray(shaderProperty.Key, (shaderProperty.Value as ShaderProperty<Vector4[]>).Get());
				}
				else if (shaderProperty.Value.PropertyType == typeof(Vector3))
				{
					m_propertyBlock.SetVector(shaderProperty.Key, (shaderProperty.Value as ShaderProperty<Vector3>).Get());
				}
				else if (shaderProperty.Value.PropertyType == typeof(Vector2))
				{
					m_propertyBlock.SetVector(shaderProperty.Key, (shaderProperty.Value as ShaderProperty<Vector2>).Get());
				}
				else if (shaderProperty.Value.PropertyType == typeof(ComputeBuffer))
				{
					m_propertyBlock.SetBuffer(shaderProperty.Key, (shaderProperty.Value as ShaderProperty<ComputeBuffer>).Get());
				}
			}
			for (int num = m_assignedRenderers.Count - 1; num >= 0; num--)
			{
				if (m_assignedRenderers[num] == null)
				{
					m_assignedRenderers.RemoveAt(num);
				}
				else
				{
					m_assignedRenderers[num].SetPropertyBlock(m_propertyBlock);
				}
			}
		}

		public void ResetValue(int nameID)
		{
			m_shaderProperties.Remove(nameID);
			MarkDirty?.Invoke(this);
		}

		public void SetValue<T>(int nameID, T value)
		{
			if (m_shaderProperties.ContainsKey(nameID))
			{
				(m_shaderProperties[nameID] as ShaderProperty<T>).Set(value);
			}
			else
			{
				m_shaderProperties.Add(nameID, new ShaderProperty<T>(nameID, value));
			}
			MarkDirty?.Invoke(this);
		}
	}

	private abstract class ShaderPropertyBase
	{
		public readonly int NameID;

		public abstract Type PropertyType { get; }

		protected ShaderPropertyBase(int nameID)
		{
			NameID = nameID;
		}
	}

	private class ShaderProperty<T> : ShaderPropertyBase
	{
		private T _value;

		public override Type PropertyType => typeof(T);

		public ShaderProperty(int nameID, T value)
			: base(nameID)
		{
			_value = value;
		}

		public void Set(T value)
		{
			_value = value;
		}

		public T Get()
		{
			return _value;
		}
	}

	private static MaterialMan s_instance;

	private Dictionary<int, PropertyContainer> m_blocks = new Dictionary<int, PropertyContainer>();

	private List<PropertyContainer> m_containersToUpdate = new List<PropertyContainer>();

	private MaterialPropertyBlock m_propertyBlock;

	public static MaterialMan instance => s_instance;

	private void Awake()
	{
		if (s_instance == null)
		{
			s_instance = this;
		}
		else
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	private void Start()
	{
		m_propertyBlock = new MaterialPropertyBlock();
	}

	private void Update()
	{
		for (int num = m_containersToUpdate.Count - 1; num >= 0; num--)
		{
			m_containersToUpdate[num].UpdateBlock();
			m_containersToUpdate.RemoveAt(num);
		}
	}

	private void RegisterRenderers(GameObject gameObject)
	{
		if (!m_blocks.ContainsKey(gameObject.GetInstanceID()))
		{
			gameObject.AddComponent<MaterialManNotifier>();
			PropertyContainer propertyContainer = new PropertyContainer(gameObject, m_propertyBlock);
			propertyContainer.MarkDirty = (Action<PropertyContainer>)Delegate.Combine(propertyContainer.MarkDirty, new Action<PropertyContainer>(QueuePropertyUpdate));
			m_blocks.Add(gameObject.GetInstanceID(), propertyContainer);
		}
	}

	public void UnregisterRenderers(GameObject gameObject)
	{
		if (!m_blocks.TryGetValue(gameObject.GetInstanceID(), out var value))
		{
			ZLog.LogError("Can't unregister renderer for " + gameObject.name);
			return;
		}
		PropertyContainer propertyContainer = value;
		propertyContainer.MarkDirty = (Action<PropertyContainer>)Delegate.Remove(propertyContainer.MarkDirty, new Action<PropertyContainer>(QueuePropertyUpdate));
		if (m_containersToUpdate.Contains(value))
		{
			m_containersToUpdate.Remove(value);
		}
		m_blocks.Remove(gameObject.GetInstanceID());
	}

	private void QueuePropertyUpdate(PropertyContainer p)
	{
		if (!m_containersToUpdate.Contains(p))
		{
			m_containersToUpdate.Add(p);
		}
	}

	public void SetValue<T>(GameObject go, int nameID, T value)
	{
		RegisterRenderers(go);
		m_blocks[go.GetInstanceID()].SetValue(nameID, value);
	}

	public void ResetValue(GameObject go, int nameID)
	{
		if (m_blocks.ContainsKey(go.GetInstanceID()))
		{
			m_blocks[go.GetInstanceID()].ResetValue(nameID);
		}
	}
}
