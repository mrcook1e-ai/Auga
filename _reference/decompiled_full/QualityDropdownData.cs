using System;
using System.Collections.Generic;
using GUIFramework;

public struct QualityDropdownData
{
	public GraphicsSettingInt m_setting;

	public GuiDropdown m_dropdown;

	public ScrollRectEnsureVisible m_scrollRectEnsureVisible;

	public List<int> m_options;

	public int OptionCount
	{
		get
		{
			if (m_options == null)
			{
				return m_dropdown.options.Count;
			}
			return m_options.Count;
		}
	}

	public QualityDropdownData(GraphicsSettingInt setting, GuiDropdown dropdown, ScrollRectEnsureVisible scrollRectEnsureVisible, IReadOnlyCollection<int> options = null)
	{
		m_setting = setting;
		m_dropdown = dropdown ?? throw new ArgumentNullException("dropdown");
		m_scrollRectEnsureVisible = scrollRectEnsureVisible;
		if (options == null)
		{
			m_options = null;
			return;
		}
		if (options.Count != m_dropdown.options.Count)
		{
			throw new ArgumentException();
		}
		m_options = new List<int>(options);
	}

	public int GetValue(int index)
	{
		if (m_options == null)
		{
			return index;
		}
		return m_options[index];
	}
}
