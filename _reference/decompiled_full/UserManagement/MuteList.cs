using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Splatform;
using UnityEngine;

namespace UserManagement;

public static class MuteList
{
	private static readonly HashSet<PlatformUserID> _mutedUsers = new HashSet<PlatformUserID>();

	private static bool _hasBeenLoaded;

	private static bool _isLoading;

	private static readonly string _block_list_file_name = "blocked_players";

	private static readonly string _block_list_file_name_noncloud = Path.Combine(Application.persistentDataPath, _block_list_file_name) + ".txt";

	public static bool Contains(PlatformUserID user)
	{
		return _mutedUsers.Contains(user);
	}

	public static void Block(PlatformUserID user)
	{
		if (PlatformManager.DistributionPlatform != null)
		{
			if (!user.IsValid)
			{
				Debug.LogError("User was invalid!");
			}
			else if (user == PlatformManager.DistributionPlatform.LocalUser.PlatformUserID)
			{
				Debug.LogError("Local user was added to the block list! This should never happen! Ignoring.");
			}
			else if (!_mutedUsers.Contains(user))
			{
				_mutedUsers.Add(user);
			}
		}
	}

	public static void Unblock(PlatformUserID user)
	{
		if (_mutedUsers.Contains(user))
		{
			_mutedUsers.Remove(user);
		}
	}

	public static string GetBlockListFileName()
	{
		if (!FileHelpers.CloudStorageEnabled)
		{
			return _block_list_file_name_noncloud;
		}
		return _block_list_file_name;
	}

	public static void Persist()
	{
		if (_mutedUsers.Count > 0)
		{
			byte[] buffer = Encode();
			if (FileHelpers.LocalStorageSupported)
			{
				FileWriter fileWriter = new FileWriter(_block_list_file_name_noncloud, FileHelpers.FileHelperType.Binary, FileHelpers.FileSource.Local);
				Debug.Log("writing banned users to local file storage");
				fileWriter.m_binary.Write(buffer);
				fileWriter.Finish();
			}
			if (FileHelpers.CloudStorageEnabled)
			{
				FileWriter fileWriter2 = new FileWriter(_block_list_file_name, FileHelpers.FileHelperType.Binary, FileHelpers.FileSource.Cloud);
				Debug.Log("Writing banned users to cloud file storage.");
				fileWriter2.m_binary.Write(buffer);
				fileWriter2.Finish();
			}
		}
	}

	public static void Load(Action onLoaded)
	{
		if (_isLoading)
		{
			return;
		}
		if (!_hasBeenLoaded)
		{
			string users = "";
			DateTime lastWriteTime = DateTime.UnixEpoch;
			string users2 = "";
			DateTime lastWriteTime2 = DateTime.UnixEpoch;
			if (FileHelpers.Exists(_block_list_file_name, FileHelpers.FileSource.Cloud) && TryCreateFileReader(onLoaded, _block_list_file_name, FileHelpers.FileSource.Cloud, out var file, out lastWriteTime2))
			{
				TryReadBlockListFromFile(file, _block_list_file_name, out users2);
			}
			if (FileHelpers.Exists(_block_list_file_name_noncloud, FileHelpers.FileSource.Local) && TryCreateFileReader(onLoaded, _block_list_file_name_noncloud, FileHelpers.FileSource.Local, out var file2, out lastWriteTime))
			{
				TryReadBlockListFromFile(file2, _block_list_file_name_noncloud, out users);
			}
			if (!string.IsNullOrEmpty(users) && !string.IsNullOrEmpty(users2))
			{
				Debug.Log($"DateTime for cloudWrite: {lastWriteTime2.ToString()}, for local: {lastWriteTime.ToString()}. DateTime.Compare: {DateTime.Compare(lastWriteTime2, lastWriteTime)}");
				if (DateTime.Compare(lastWriteTime2, lastWriteTime) >= 0)
				{
					BlockUsers(users2);
				}
				else
				{
					BlockUsers(users);
				}
			}
			else if (!string.IsNullOrEmpty(users))
			{
				Debug.Log("there was no cloud users, instead using localusers! " + users);
				BlockUsers(users);
			}
			else if (!string.IsNullOrEmpty(users2))
			{
				Debug.Log("there was no local user, instead using cloud users! " + users2);
				BlockUsers(users2);
			}
			_hasBeenLoaded = true;
			onLoaded?.Invoke();
		}
		else
		{
			onLoaded?.Invoke();
		}
	}

	private static bool TryCreateFileReader(Action onLoaded, string path, FileHelpers.FileSource fileSource, out FileReader file, out DateTime lastWriteTime)
	{
		_isLoading = true;
		try
		{
			file = new FileReader(path, fileSource, FileHelpers.FileHelperType.Stream);
			lastWriteTime = FileHelpers.GetLastWriteTime(path, fileSource);
		}
		catch (Exception ex)
		{
			ZLog.Log("Failed to load: " + path + " (" + ex.Message + ")");
			_isLoading = false;
			_hasBeenLoaded = true;
			onLoaded?.Invoke();
			file = null;
			lastWriteTime = DateTime.UnixEpoch;
			return false;
		}
		return true;
	}

	private static void TryReadBlockListFromFile(FileReader file, string fileName, out string users)
	{
		try
		{
			StreamReader stream = file.m_stream;
			users = stream.ReadToEnd();
			Debug.Log("now getting block list from file " + fileName + ". got these users: " + users);
		}
		catch (Exception ex)
		{
			ZLog.LogError("error loading blocked_players. FileName: " + fileName + ", Error: " + ex.Message);
			file.Dispose();
			users = null;
		}
		file.Dispose();
		_isLoading = false;
	}

	private static byte[] Encode()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (PlatformUserID mutedUser in _mutedUsers)
		{
			stringBuilder.Append(mutedUser).Append('\n');
		}
		return Encoding.UTF8.GetBytes(stringBuilder.ToString());
	}

	private static void BlockUsers(string textUsers)
	{
		_mutedUsers.Clear();
		string[] array = textUsers.Split(new string[3] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
		for (int i = 0; i < array.Length; i++)
		{
			Block(new PlatformUserID(array[i]));
		}
	}
}
