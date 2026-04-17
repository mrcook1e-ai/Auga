using System;
using Splatform;
using UnityEngine;
using UserManagement;

public static class RelationsManager
{
	public const string c_AuthorHostPlaceholder = "host";

	public static bool PlatformRequiresTextFiltering()
	{
		if (PlatformManager.DistributionPlatform.Platform == "Xbox")
		{
			return true;
		}
		if (PlatformManager.DistributionPlatform.HardwareInfoProvider != null && PlatformManager.DistributionPlatform.HardwareInfoProvider.HardwareInfo.m_category == HardwareCategory.Console)
		{
			return true;
		}
		return false;
	}

	public static bool FilterTextCommunicationSentToUser(PlatformUserID recipient)
	{
		if (!PlatformRequiresTextFiltering())
		{
			return false;
		}
		if (recipient == PlatformManager.DistributionPlatform.LocalUser.PlatformUserID)
		{
			return false;
		}
		IRelationsProvider relationsProvider = PlatformManager.DistributionPlatform.RelationsProvider;
		if (relationsProvider == null)
		{
			return true;
		}
		if (relationsProvider.IsFriend(recipient))
		{
			return false;
		}
		return true;
	}

	public static void CheckPermissionAsync(PlatformUserID user, Permission permission, bool isSender, CheckPermissionCompletedHandler completedHandler)
	{
		if (!user.IsValid)
		{
			ZLog.LogError($"Failed to check permission {permission}: UserID was invalid");
			completedHandler(RelationsManagerPermissionResult.Error);
			return;
		}
		if (user == PlatformManager.DistributionPlatform.LocalUser.PlatformUserID)
		{
			completedHandler(RelationsManagerPermissionResult.Granted);
			return;
		}
		if (!isSender && MuteList.Contains(user))
		{
			switch (permission)
			{
			case Permission.CommunicateWithUsingText:
			case Permission.ViewUserGeneratedContent:
				completedHandler(RelationsManagerPermissionResult.Denied);
				return;
			default:
				throw new NotImplementedException($"Permission {permission} has not been implemented!");
			case Permission.PlayMultiplayerWith:
				break;
			}
		}
		if (!TryCheckEquivalentPrivilege(permission, out var result))
		{
			ZLog.LogError($"Failed to check permission {permission} for user {user}: Equivalent privilege check failed");
			completedHandler(RelationsManagerPermissionResult.Error);
		}
		else if (!result)
		{
			ZLog.Log($"Permission {permission} was denied for user {user}: Equivalent privilege was denied");
			completedHandler?.Invoke(RelationsManagerPermissionResult.Denied);
		}
		else if (PlatformManager.DistributionPlatform.RelationsProvider == null)
		{
			completedHandler?.Invoke(PlatformRequiresTextFiltering() ? RelationsManagerPermissionResult.GrantedRequiresFiltering : RelationsManagerPermissionResult.Granted);
		}
		else
		{
			PlatformManager.DistributionPlatform.RelationsProvider.GetUserProfileAsync(user, OnGetUserProfileCompleted, OnGetUserProfileFailed);
		}
		void OnGetUserProfileCompleted(IUserProfile profile)
		{
			PermissionResult permissionResult = profile.CheckPermission(permission);
			if (permissionResult.IsError())
			{
				ZLog.LogError($"Failed to check permission {permission} for user {user}: {permissionResult}");
				completedHandler(RelationsManagerPermissionResult.Error);
			}
			else
			{
				RelationsManagerPermissionResult result2;
				if (permissionResult == PermissionResult.Granted)
				{
					result2 = (FilterTextCommunicationSentToUser(user) ? RelationsManagerPermissionResult.GrantedRequiresFiltering : RelationsManagerPermissionResult.Granted);
				}
				else
				{
					ZLog.Log($"Permission {permission} was denied for user {user}: {permissionResult}");
					result2 = RelationsManagerPermissionResult.Denied;
				}
				completedHandler(result2);
			}
		}
		void OnGetUserProfileFailed(PlatformUserID userId, GetUserProfileFailReason reason)
		{
			switch (reason)
			{
			case GetUserProfileFailReason.DifferentPlatformsNotAvailable:
			case GetUserProfileFailReason.SamePlatformNotAvailable:
				completedHandler(FilterTextCommunicationSentToUser(userId) ? RelationsManagerPermissionResult.GrantedRequiresFiltering : RelationsManagerPermissionResult.Granted);
				break;
			case GetUserProfileFailReason.InvalidID:
			case GetUserProfileFailReason.Error:
				ZLog.LogError($"Failed to get user profile for user {userId}: {reason}, {permission} permission check for user {userId} will fail.");
				completedHandler(RelationsManagerPermissionResult.Error);
				break;
			default:
				ZLog.LogError($"GetUserProfileFailReason {reason} not implemented!");
				completedHandler(RelationsManagerPermissionResult.Error);
				break;
			}
		}
	}

	private static bool TryCheckEquivalentPrivilege(Permission permission, out bool result)
	{
		Privilege privilege;
		switch (permission)
		{
		case Permission.PlayMultiplayerWith:
			privilege = Privilege.OnlineMultiplayer;
			break;
		case Permission.CommunicateWithUsingText:
			privilege = Privilege.TextCommunication;
			break;
		case Permission.ViewUserGeneratedContent:
			privilege = Privilege.ViewUserGeneratedContent;
			break;
		default:
			ZLog.LogError($"Failed to check equivalent privilege for permission {permission}: There is no equivalent privilege");
			result = false;
			return false;
		}
		PrivilegeResult privilegeResult = PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(privilege);
		if (privilegeResult.IsError())
		{
			ZLog.LogError($"Failed to check privilege {privilege}: {privilegeResult}");
			result = false;
			return false;
		}
		result = privilegeResult.IsGranted();
		return true;
	}

	public static bool UpdateAuthorIfHost(string authorString, ref string resolvedAuthor)
	{
		if (authorString != "host")
		{
			return false;
		}
		if (!ZNet.instance.IsCurrentServerDedicated())
		{
			return false;
		}
		if (ZNet.instance.GetPlayerList().Count <= 0)
		{
			return false;
		}
		PlatformUserID id = ZNet.instance.GetPlayerList()[0].m_userInfo.m_id;
		if (!id.IsValid)
		{
			Debug.LogWarning("Server host lacked valid ID while trying to resolve unclaimed object authorship.");
			return false;
		}
		Debug.Log("There was an update from a placeholder PlatformUserID to the following:" + id.ToString());
		resolvedAuthor = id.ToString();
		return true;
	}

	public static bool IsBlocked(PlatformUserID user)
	{
		if (PlatformManager.DistributionPlatform.RelationsProvider == null)
		{
			return false;
		}
		return PlatformManager.DistributionPlatform.RelationsProvider.IsBlocked(user);
	}
}
