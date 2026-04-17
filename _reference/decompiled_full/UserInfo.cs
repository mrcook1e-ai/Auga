using Splatform;

public class UserInfo : ISerializableParameter
{
	public string Name;

	public PlatformUserID UserId;

	public static UserInfo GetLocalUser()
	{
		return new UserInfo
		{
			Name = Game.instance.GetPlayerProfile().GetName(),
			UserId = PlatformManager.DistributionPlatform.LocalUser.PlatformUserID
		};
	}

	public void Deserialize(ref ZPackage pkg)
	{
		Name = pkg.ReadString();
		UserId = new PlatformUserID(pkg.ReadString());
	}

	public void Serialize(ref ZPackage pkg)
	{
		pkg.Write(Name);
		pkg.Write(UserId.ToString());
	}

	public string GetDisplayName()
	{
		return CensorShittyWords.FilterUGC(Name, UGCType.CharacterName, UserId, 0L);
	}
}
