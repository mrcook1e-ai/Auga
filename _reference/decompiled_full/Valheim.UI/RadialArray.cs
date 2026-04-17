using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Valheim.UI;

[Serializable]
public class RadialArray<T>
{
	private float _segmentSize;

	public T[] GetArray { get; }

	public List<T> GetAsList => GetArray.ToList();

	public int Count { get; }

	public int MaxIndex { get; }

	public RadialArray(List<T> elements, int elementsPerLayer)
		: this(elements.ToArray(), elementsPerLayer)
	{
	}

	public RadialArray(T[] elements, int elementsPerLayer)
	{
		GetArray = elements;
		Count = elements.Length;
		MaxIndex = Count - 1;
		_segmentSize = Mathf.Round(360f / (float)elementsPerLayer);
	}

	public T GetElement(float cursorPos)
	{
		return GetElement(IndexOf(cursorPos));
	}

	public T GetElement(int index)
	{
		if (index >= 0 && index < Count)
		{
			return GetArray[index];
		}
		return default(T);
	}

	public List<T> GetVisisbleElementsAt(float cursorPos, int fadeCount, int showCount, bool doubleSided = true)
	{
		return GetVisisbleElementsAt(IndexOf(cursorPos), fadeCount, showCount, doubleSided);
	}

	public List<T> GetVisisbleElementsAt(int index, int fadeCount, int showCount, bool doubleSided = true)
	{
		List<T> list = new List<T>();
		for (int i = (doubleSided ? (index - fadeCount - showCount) : (index - fadeCount)); i <= index + showCount + fadeCount; i++)
		{
			if (i >= 0)
			{
				if (i >= Count)
				{
					break;
				}
				list.Add(GetArray[i]);
			}
		}
		return list;
	}

	public bool IsVisible(float pos, int fadeIndex, int fadeCount, int showCount, bool doubleSided = true)
	{
		return IsVisible(GetElement(pos), fadeIndex, fadeCount, showCount, doubleSided);
	}

	public bool IsVisible(int index, float fadePos, int fadeCount, int showCount, bool doubleSided = true)
	{
		return IsVisible(index, IndexOf(fadePos), fadeCount, showCount, doubleSided);
	}

	public bool IsVisible(int index, int fadeIndex, int fadeCount, int showCount, bool doubleSided = true)
	{
		return IsVisible(GetElement(index), fadeIndex, fadeCount, showCount, doubleSided);
	}

	public bool IsVisible(T element, int fadeIndex, int fadeCount, int showCount, bool doubleSided = true)
	{
		return GetVisisbleElementsAt(fadeIndex, fadeCount, showCount, doubleSided).Contains(element);
	}

	public int ViableIndex(int index)
	{
		if (index >= 0 && index < Count)
		{
			return index;
		}
		return -1;
	}

	public int IndexOf(float position)
	{
		return Mathf.FloorToInt(UIMath.ClosestSegment(position, _segmentSize) / _segmentSize);
	}

	public int IndexOf(T element)
	{
		return Array.IndexOf(GetArray, element);
	}

	public int BackButtonIndex()
	{
		if (!GetArray.Any((T e) => e is BackElement))
		{
			return -1;
		}
		return Array.IndexOf(GetArray, GetArray.First((T e) => e is BackElement));
	}
}
