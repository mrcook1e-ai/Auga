using System;
using System.Collections.Generic;
using System.IO;

public class SaveCollection
{
	public readonly SaveDataType m_dataType;

	private List<SaveWithBackups> m_saves = new List<SaveWithBackups>();

	private Dictionary<string, SaveWithBackups> m_savesByName = new Dictionary<string, SaveWithBackups>(StringComparer.OrdinalIgnoreCase);

	private bool m_needsSort;

	private bool m_needsReload = true;

	public SaveWithBackups[] Saves
	{
		get
		{
			EnsureLoadedAndSorted();
			return m_saves.ToArray();
		}
	}

	public SaveCollection(SaveDataType dataType)
	{
		m_dataType = dataType;
	}

	public void Add(SaveWithBackups save)
	{
		m_saves.Add(save);
		SetNeedsSort();
	}

	public void Remove(SaveWithBackups save)
	{
		m_saves.Remove(save);
		SetNeedsSort();
	}

	public void EnsureLoadedAndSorted()
	{
		EnsureLoaded();
		if (m_needsSort)
		{
			Sort();
		}
	}

	private void EnsureLoaded()
	{
		if (m_needsReload)
		{
			Reload();
		}
	}

	public void InvalidateCache()
	{
		m_needsReload = true;
	}

	public bool TryGetSaveByName(string name, out SaveWithBackups save)
	{
		EnsureLoaded();
		return m_savesByName.TryGetValue(name, out save);
	}

	private void Reload()
	{
		m_saves.Clear();
		m_savesByName.Clear();
		List<string> listToAddTo = new List<string>();
		if (FileHelpers.CloudStorageEnabled)
		{
			GetAllFilesInSource(m_dataType, FileHelpers.FileSource.Cloud, ref listToAddTo);
		}
		int count = listToAddTo.Count;
		if (Directory.Exists(SaveSystem.GetSavePath(m_dataType, FileHelpers.FileSource.Local)))
		{
			GetAllFilesInSource(m_dataType, FileHelpers.FileSource.Local, ref listToAddTo);
		}
		int count2 = listToAddTo.Count;
		if (Directory.Exists(SaveSystem.GetSavePath(m_dataType, FileHelpers.FileSource.Legacy)))
		{
			GetAllFilesInSource(m_dataType, FileHelpers.FileSource.Legacy, ref listToAddTo);
		}
		for (int i = 0; i < listToAddTo.Count; i++)
		{
			string text = listToAddTo[i];
			if (!SaveSystem.GetSaveInfo(text, out var saveName, out var _, out var actualFileEnding, out var _))
			{
				continue;
			}
			FileHelpers.FileSource fileSource = SourceByIndexAndEntryCount(count, count2, i);
			if (fileSource != FileHelpers.FileSource.Cloud)
			{
				switch (m_dataType)
				{
				case SaveDataType.World:
					if (actualFileEnding != ".fwl" && actualFileEnding != ".db")
					{
						continue;
					}
					break;
				case SaveDataType.Character:
					if (actualFileEnding != ".fch")
					{
						continue;
					}
					break;
				default:
					ZLog.LogError($"File type filter not implemented for data type {m_dataType}!");
					break;
				}
			}
			if (!m_savesByName.TryGetValue(saveName, out var value))
			{
				value = new SaveWithBackups(saveName, this, SetNeedsSort);
				m_saves.Add(value);
				m_savesByName.Add(saveName, value);
			}
			value.AddSaveFile(text, fileSource);
		}
		m_needsReload = false;
		SetNeedsSort();
		static bool GetAllFilesInSource(SaveDataType dataType, FileHelpers.FileSource source, ref List<string> reference)
		{
			string savePath = SaveSystem.GetSavePath(dataType, source);
			string[] files = FileHelpers.GetFiles(source, savePath);
			if (files == null)
			{
				return false;
			}
			reference.AddRange(files);
			return true;
		}
		static FileHelpers.FileSource SourceByIndexAndEntryCount(int cloudEntries, int localEntries, int num)
		{
			if (num < cloudEntries)
			{
				return FileHelpers.FileSource.Cloud;
			}
			if (num < localEntries)
			{
				return FileHelpers.FileSource.Local;
			}
			return FileHelpers.FileSource.Legacy;
		}
	}

	private void Sort()
	{
		m_saves.Sort(new SaveWithBackupsComparer());
		m_needsSort = false;
	}

	private void SetNeedsSort()
	{
		m_needsSort = true;
	}
}
