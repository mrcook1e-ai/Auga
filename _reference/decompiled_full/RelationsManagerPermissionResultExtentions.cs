public static class RelationsManagerPermissionResultExtentions
{
	public static bool IsGranted(this RelationsManagerPermissionResult result)
	{
		if (result != RelationsManagerPermissionResult.Granted)
		{
			return result == RelationsManagerPermissionResult.GrantedRequiresFiltering;
		}
		return true;
	}
}
