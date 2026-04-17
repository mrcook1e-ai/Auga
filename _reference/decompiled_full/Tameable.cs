using System;
using System.Collections.Generic;
using Splatform;
using UnityEngine;

public class Tameable : MonoBehaviour, Interactable, TextReceiver
{
	public delegate string TextGetter();

	private const float m_playerMaxDistance = 15f;

	private const float m_tameDeltaTime = 3f;

	private static List<Player> s_nearbyPlayers = new List<Player>();

	public float m_fedDuration = 30f;

	public float m_tamingTime = 1800f;

	public bool m_startsTamed;

	public EffectList m_tamedEffect = new EffectList();

	public EffectList m_sootheEffect = new EffectList();

	public EffectList m_petEffect = new EffectList();

	public bool m_commandable;

	public float m_unsummonDistance;

	public float m_unsummonOnOwnerLogoutSeconds;

	public EffectList m_unSummonEffect = new EffectList();

	public Skills.SkillType m_levelUpOwnerSkill;

	public float m_levelUpFactor = 1f;

	public ItemDrop m_saddleItem;

	public Sadle m_saddle;

	public bool m_dropSaddleOnDeath = true;

	public Vector3 m_dropSaddleOffset = new Vector3(0f, 1f, 0f);

	public float m_dropItemVel = 5f;

	public List<string> m_randomStartingName = new List<string>();

	public float m_tamingSpeedMultiplierRange = 60f;

	public float m_tamingBoostMultiplier = 2f;

	public bool m_nameBeforeText = true;

	public string m_tameText = "$hud_tamelove";

	public TextGetter m_tameTextGetter;

	private Character m_character;

	private MonsterAI m_monsterAI;

	private Piece m_piece;

	private ZNetView m_nview;

	private float m_lastPetTime;

	private float m_unsummonTime;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_character = GetComponent<Character>();
		m_monsterAI = GetComponent<MonsterAI>();
		m_piece = GetComponent<Piece>();
		if ((bool)m_character)
		{
			Character character = m_character;
			character.m_onDeath = (Action)Delegate.Combine(character.m_onDeath, new Action(OnDeath));
		}
		if ((bool)m_monsterAI)
		{
			MonsterAI monsterAI = m_monsterAI;
			monsterAI.m_onConsumedItem = (Action<ItemDrop>)Delegate.Combine(monsterAI.m_onConsumedItem, new Action<ItemDrop>(OnConsumedItem));
		}
		if (m_nview.IsValid())
		{
			m_nview.Register<ZDOID, bool>("Command", RPC_Command);
			m_nview.Register<string, string>("SetName", RPC_SetName);
			m_nview.Register("RPC_UnSummon", RPC_UnSummon);
			if (m_saddle != null)
			{
				m_nview.Register("AddSaddle", RPC_AddSaddle);
				m_nview.Register<bool>("SetSaddle", RPC_SetSaddle);
				SetSaddle(HaveSaddle());
			}
			InvokeRepeating("TamingUpdate", 3f, 3f);
		}
		if (m_startsTamed && (bool)m_character)
		{
			m_character.SetTamed(tamed: true);
		}
		if (m_randomStartingName.Count > 0 && m_nview.IsValid() && m_nview.GetZDO().GetString(ZDOVars.s_tamedName).Length == 0)
		{
			SetText(Localization.instance.Localize(m_randomStartingName[UnityEngine.Random.Range(0, m_randomStartingName.Count)]));
		}
	}

	public void Update()
	{
		UpdateSummon();
		UpdateSavedFollowTarget();
	}

	public string GetHoverText()
	{
		if (!m_nview.IsValid())
		{
			return "";
		}
		string text = GetName();
		if (IsTamed())
		{
			if ((bool)m_character)
			{
				text += Localization.instance.Localize(" ( $hud_tame, " + GetStatusString() + " )");
			}
			text += Localization.instance.Localize("\n[<color=yellow><b>$KEY_Use</b></color>] $hud_pet");
			if (ZInput.IsNonClassicFunctionality() && ZInput.IsGamepadActive())
			{
				return text + Localization.instance.Localize("\n[<color=yellow><b>$KEY_AltKeys + $KEY_Use</b></color>] $hud_rename");
			}
			return text + Localization.instance.Localize("\n[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] $hud_rename");
		}
		int tameness = GetTameness();
		if (tameness <= 0)
		{
			return text + Localization.instance.Localize(" ( $hud_wild, " + GetStatusString() + " )");
		}
		return text + Localization.instance.Localize(" ( $hud_tameness  " + tameness + "%, " + GetStatusString() + " )");
	}

	public string GetStatusString()
	{
		if ((bool)m_monsterAI && m_monsterAI.IsAlerted())
		{
			return "$hud_tamefrightened";
		}
		if (IsHungry())
		{
			return "$hud_tamehungry";
		}
		if (!m_character || m_character.IsTamed())
		{
			return "$hud_tamehappy";
		}
		return "$hud_tameinprogress";
	}

	public bool IsTamed()
	{
		if (!m_character || !m_character.IsTamed())
		{
			return m_startsTamed;
		}
		return true;
	}

	public string GetName()
	{
		return Localization.instance.Localize(m_character ? m_character.m_name : (((bool)m_nview && m_nview.IsValid()) ? m_nview.GetZDO().GetString(ZDOVars.s_tamedName, m_piece.m_name) : m_piece.m_name));
	}

	public bool Interact(Humanoid user, bool hold, bool alt)
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		if (hold)
		{
			return false;
		}
		if (alt)
		{
			SetName();
			return true;
		}
		string hoverName = GetHoverName();
		object msg;
		if (IsTamed())
		{
			if (Time.time - m_lastPetTime > 1f)
			{
				m_lastPetTime = Time.time;
				m_petEffect.Create(base.transform.position, base.transform.rotation);
				if (m_commandable)
				{
					Command(user);
					goto IL_00da;
				}
				if (m_tameTextGetter != null)
				{
					string text = m_tameTextGetter();
					if (text != null && text.Length > 0)
					{
						msg = text;
						goto IL_00d3;
					}
				}
				msg = (m_nameBeforeText ? (hoverName + " " + m_tameText) : m_tameText);
				goto IL_00d3;
			}
			return false;
		}
		return false;
		IL_00da:
		return true;
		IL_00d3:
		user.Message(MessageHud.MessageType.Center, (string)msg);
		goto IL_00da;
	}

	public string GetHoverName()
	{
		if (IsTamed())
		{
			string text = GetText().RemoveRichTextTags();
			if (text.Length > 0)
			{
				return text;
			}
			return GetName();
		}
		return GetName();
	}

	private void SetName()
	{
		if (!IsTamed())
		{
			return;
		}
		PrivilegeResult privilegeResult = PlatformManager.DistributionPlatform.PrivilegeProvider.CheckPrivilege(Privilege.ViewUserGeneratedContent);
		if (!privilegeResult.IsGranted())
		{
			if (PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege != null)
			{
				PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege.Open(Privilege.ViewUserGeneratedContent);
				if (!PlatformManager.DistributionPlatform.UIProvider.ResolvePrivilege.IsOpen)
				{
					ZLog.LogError(string.Format("{0} can't resolve the {1} privilege on this platform, which was denied with result {2}. Tameable rename was blocked without meaningful feedback to the user!", "ResolvePrivilegeUI", Privilege.ViewUserGeneratedContent, privilegeResult));
				}
			}
			else
			{
				ZLog.LogError(string.Format("{0} is not available on this platform to resolve the {1} privilege, which was denied with result {2}. Tameable rename was blocked without meaningful feedback to the user!", "ResolvePrivilegeUI", Privilege.ViewUserGeneratedContent, privilegeResult));
			}
		}
		else
		{
			TextInput.instance.RequestText(this, "$hud_rename", 10);
		}
	}

	public string GetText()
	{
		if (!m_nview.IsValid())
		{
			return "";
		}
		string text = m_nview.GetZDO().GetString(ZDOVars.s_tamedName);
		string resolvedAuthor = m_nview.GetZDO().GetString(ZDOVars.s_tamedNameAuthor, null);
		if (m_nview.IsOwner() && RelationsManager.UpdateAuthorIfHost(resolvedAuthor, ref resolvedAuthor))
		{
			m_nview.GetZDO().Set(ZDOVars.s_tamedNameAuthor, resolvedAuthor);
		}
		if (string.IsNullOrEmpty(resolvedAuthor) || resolvedAuthor == "host")
		{
			return CensorShittyWords.FilterUGC(text, UGCType.Text, default(PlatformUserID), 0L);
		}
		return CensorShittyWords.FilterUGC(text, UGCType.Text, new PlatformUserID(resolvedAuthor), 0L);
	}

	public void SetText(string text)
	{
		if (m_nview.IsValid())
		{
			PlatformUserID platformUserID = PlatformManager.DistributionPlatform.LocalUser.PlatformUserID;
			m_nview.InvokeRPC("SetName", text, PlatformManager.DistributionPlatform.LocalUser.IsSignedIn ? platformUserID.ToString() : "host");
		}
	}

	private void RPC_SetName(long sender, string name, string authorId)
	{
		if (m_nview.IsValid() && m_nview.IsOwner() && IsTamed())
		{
			m_nview.GetZDO().Set(ZDOVars.s_tamedName, name);
			m_nview.GetZDO().Set(ZDOVars.s_tamedNameAuthor, authorId);
		}
	}

	public bool UseItem(Humanoid user, ItemDrop.ItemData item)
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		if (m_saddleItem != null && IsTamed() && item.m_shared.m_name == m_saddleItem.m_itemData.m_shared.m_name)
		{
			if (HaveSaddle())
			{
				user.Message(MessageHud.MessageType.Center, GetHoverName() + " $hud_saddle_already");
				return true;
			}
			m_nview.InvokeRPC("AddSaddle");
			user.GetInventory().RemoveOneItem(item);
			user.Message(MessageHud.MessageType.Center, GetHoverName() + " $hud_saddle_ready");
			return true;
		}
		return false;
	}

	private void RPC_AddSaddle(long sender)
	{
		if (m_nview.IsOwner() && !HaveSaddle())
		{
			m_nview.GetZDO().Set(ZDOVars.s_haveSaddleHash, value: true);
			m_nview.InvokeRPC(ZNetView.Everybody, "SetSaddle", true);
		}
	}

	public bool DropSaddle(Vector3 userPoint)
	{
		if (!HaveSaddle())
		{
			return false;
		}
		m_nview.GetZDO().Set(ZDOVars.s_haveSaddleHash, value: false);
		m_nview.InvokeRPC(ZNetView.Everybody, "SetSaddle", false);
		Vector3 flyDirection = userPoint - base.transform.position;
		SpawnSaddle(flyDirection);
		return true;
	}

	private void SpawnSaddle(Vector3 flyDirection)
	{
		Rigidbody component = UnityEngine.Object.Instantiate(m_saddleItem.gameObject, base.transform.TransformPoint(m_dropSaddleOffset), Quaternion.identity).GetComponent<Rigidbody>();
		if ((bool)component)
		{
			Vector3 up = Vector3.up;
			if (flyDirection.magnitude > 0.1f)
			{
				flyDirection.y = 0f;
				flyDirection.Normalize();
				up += flyDirection;
			}
			component.AddForce(up * m_dropItemVel, ForceMode.VelocityChange);
		}
	}

	private bool HaveSaddle()
	{
		if (m_saddle == null)
		{
			return false;
		}
		if (!m_nview.IsValid())
		{
			return false;
		}
		return m_nview.GetZDO().GetBool(ZDOVars.s_haveSaddleHash);
	}

	private void RPC_SetSaddle(long sender, bool enabled)
	{
		SetSaddle(enabled);
	}

	private void SetSaddle(bool enabled)
	{
		ZLog.Log("Setting saddle:" + enabled);
		if (m_saddle != null)
		{
			m_saddle.gameObject.SetActive(enabled);
		}
	}

	private void TamingUpdate()
	{
		if (m_nview.IsValid() && m_nview.IsOwner() && !IsTamed() && !IsHungry() && (bool)m_monsterAI && !m_monsterAI.IsAlerted())
		{
			m_monsterAI.SetDespawnInDay(despawn: false);
			m_monsterAI.SetEventCreature(despawn: false);
			DecreaseRemainingTime(3f);
			if (GetRemainingTime() <= 0f)
			{
				Tame();
			}
			else
			{
				m_sootheEffect.Create(base.transform.position, base.transform.rotation);
			}
		}
	}

	private void Tame()
	{
		Game.instance.IncrementPlayerStat(PlayerStatType.CreatureTamed);
		if (m_nview.IsValid() && m_nview.IsOwner() && (bool)m_monsterAI && (bool)m_character && !IsTamed())
		{
			m_monsterAI.MakeTame();
			m_tamedEffect.Create(base.transform.position, base.transform.rotation);
			Player closestPlayer = Player.GetClosestPlayer(base.transform.position, 30f);
			if ((bool)closestPlayer)
			{
				closestPlayer.Message(MessageHud.MessageType.Center, m_character.m_name + " $hud_tamedone");
			}
		}
	}

	public static void TameAllInArea(Vector3 point, float radius)
	{
		foreach (Character allCharacter in Character.GetAllCharacters())
		{
			if (!allCharacter.IsPlayer())
			{
				Tameable component = allCharacter.GetComponent<Tameable>();
				if ((bool)component)
				{
					component.Tame();
				}
			}
		}
	}

	public void Command(Humanoid user, bool message = true)
	{
		m_nview.InvokeRPC("Command", user.GetZDOID(), message);
	}

	private Player GetPlayer(ZDOID characterID)
	{
		GameObject gameObject = ZNetScene.instance.FindInstance(characterID);
		if ((bool)gameObject)
		{
			return gameObject.GetComponent<Player>();
		}
		return null;
	}

	private void RPC_Command(long sender, ZDOID characterID, bool message)
	{
		Player player = GetPlayer(characterID);
		if (player == null || !m_monsterAI)
		{
			return;
		}
		if ((bool)m_monsterAI.GetFollowTarget())
		{
			m_monsterAI.SetFollowTarget(null);
			m_monsterAI.SetPatrolPoint();
			if (m_nview.IsOwner())
			{
				m_nview.GetZDO().Set(ZDOVars.s_follow, "");
			}
			if (message)
			{
				player.Message(MessageHud.MessageType.Center, GetHoverName() + " $hud_tamestay");
			}
		}
		else
		{
			m_monsterAI.ResetPatrolPoint();
			m_monsterAI.SetFollowTarget(player.gameObject);
			if (m_nview.IsOwner())
			{
				m_nview.GetZDO().Set(ZDOVars.s_follow, player.GetPlayerName());
			}
			if (message)
			{
				player.Message(MessageHud.MessageType.Center, GetHoverName() + " $hud_tamefollow");
			}
			int num = m_nview.GetZDO().GetInt(ZDOVars.s_maxInstances);
			if (num > 0)
			{
				UnsummonMaxInstances(num);
			}
		}
		m_unsummonTime = 0f;
	}

	private void UpdateSavedFollowTarget()
	{
		if (!m_monsterAI || m_monsterAI.GetFollowTarget() != null || !m_nview.IsOwner())
		{
			return;
		}
		string text = m_nview.GetZDO().GetString(ZDOVars.s_follow);
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		foreach (Player allPlayer in Player.GetAllPlayers())
		{
			if (allPlayer.GetPlayerName() == text)
			{
				Command(allPlayer, message: false);
				return;
			}
		}
		if (m_unsummonOnOwnerLogoutSeconds > 0f)
		{
			m_unsummonTime += Time.fixedDeltaTime;
			if (m_unsummonTime > m_unsummonOnOwnerLogoutSeconds)
			{
				UnSummon();
			}
		}
	}

	public bool IsHungry()
	{
		if (!m_character)
		{
			return false;
		}
		if (m_nview == null)
		{
			return false;
		}
		ZDO zDO = m_nview.GetZDO();
		if (zDO == null)
		{
			return false;
		}
		DateTime dateTime = new DateTime(zDO.GetLong(ZDOVars.s_tameLastFeeding, 0L));
		return (ZNet.instance.GetTime() - dateTime).TotalSeconds > (double)m_fedDuration;
	}

	private void ResetFeedingTimer()
	{
		m_nview.GetZDO().Set(ZDOVars.s_tameLastFeeding, ZNet.instance.GetTime().Ticks);
	}

	private void OnDeath()
	{
		ZLog.Log("Valid " + m_nview.IsValid());
		ZLog.Log("On death " + HaveSaddle());
		if (HaveSaddle() && m_dropSaddleOnDeath)
		{
			ZLog.Log("Spawning saddle ");
			SpawnSaddle(Vector3.zero);
		}
	}

	private int GetTameness()
	{
		float remainingTime = GetRemainingTime();
		return (int)((1f - Mathf.Clamp01(remainingTime / m_tamingTime)) * 100f);
	}

	private void OnConsumedItem(ItemDrop item)
	{
		if (IsHungry())
		{
			m_sootheEffect.Create(m_character ? m_character.GetCenterPoint() : base.transform.position, Quaternion.identity);
		}
		ResetFeedingTimer();
	}

	private void DecreaseRemainingTime(float time)
	{
		if (!m_nview.IsValid())
		{
			return;
		}
		float remainingTime = GetRemainingTime();
		s_nearbyPlayers.Clear();
		Player.GetPlayersInRange(base.transform.position, m_tamingSpeedMultiplierRange, s_nearbyPlayers);
		foreach (Player s_nearbyPlayer in s_nearbyPlayers)
		{
			if (s_nearbyPlayer.GetSEMan().HaveStatusAttribute(StatusEffect.StatusAttribute.TamingBoost))
			{
				time *= m_tamingBoostMultiplier;
			}
		}
		remainingTime -= time;
		if (remainingTime < 0f)
		{
			remainingTime = 0f;
		}
		m_nview.GetZDO().Set(ZDOVars.s_tameTimeLeft, remainingTime);
	}

	private float GetRemainingTime()
	{
		if (!m_nview.IsValid())
		{
			return 0f;
		}
		return m_nview.GetZDO().GetFloat(ZDOVars.s_tameTimeLeft, m_tamingTime);
	}

	public bool HaveRider()
	{
		if ((bool)m_saddle)
		{
			return m_saddle.HaveValidUser();
		}
		return false;
	}

	public float GetRiderSkill()
	{
		if ((bool)m_saddle)
		{
			return m_saddle.GetRiderSkill();
		}
		return 0f;
	}

	private void UpdateSummon()
	{
		if (m_nview.IsValid() && m_nview.IsOwner() && m_unsummonDistance > 0f && (bool)m_monsterAI)
		{
			GameObject followTarget = m_monsterAI.GetFollowTarget();
			if ((bool)followTarget && Vector3.Distance(followTarget.transform.position, base.gameObject.transform.position) > m_unsummonDistance)
			{
				UnSummon();
			}
		}
	}

	private void UnsummonMaxInstances(int maxInstances)
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner() || !m_character)
		{
			return;
		}
		GameObject followTarget = m_monsterAI.GetFollowTarget();
		object obj;
		if ((object)followTarget != null)
		{
			Player component = followTarget.GetComponent<Player>();
			if ((object)component != null)
			{
				obj = component.GetPlayerName();
				goto IL_004a;
			}
		}
		obj = null;
		goto IL_004a;
		IL_004a:
		string text = (string)obj;
		if (text == null)
		{
			return;
		}
		List<Character> allCharacters = Character.GetAllCharacters();
		List<BaseAI> list = new List<BaseAI>();
		foreach (Character item in allCharacters)
		{
			if (!(item.m_name == m_character.m_name))
			{
				continue;
			}
			ZNetView component2 = item.GetComponent<ZNetView>();
			object obj2;
			if ((object)component2 != null)
			{
				ZDO zDO = component2.GetZDO();
				if (zDO != null)
				{
					obj2 = zDO.GetString(ZDOVars.s_follow);
					goto IL_00b7;
				}
			}
			obj2 = "";
			goto IL_00b7;
			IL_00b7:
			if ((string)obj2 == text)
			{
				MonsterAI component3 = item.GetComponent<MonsterAI>();
				if ((object)component3 != null)
				{
					list.Add(component3);
				}
			}
		}
		list.Sort((BaseAI a, BaseAI b) => b.GetTimeSinceSpawned().CompareTo(a.GetTimeSinceSpawned()));
		int num = list.Count - maxInstances;
		for (int num2 = 0; num2 < num; num2++)
		{
			list[num2].GetComponent<Tameable>()?.UnSummon();
		}
		if (num > 0 && (bool)Player.m_localPlayer)
		{
			Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$hud_maxsummonsreached");
		}
	}

	private void UnSummon()
	{
		if (m_nview.IsValid())
		{
			m_nview.InvokeRPC(ZNetView.Everybody, "RPC_UnSummon");
		}
	}

	private void RPC_UnSummon(long sender)
	{
		m_unSummonEffect.Create(base.gameObject.transform.position, base.gameObject.transform.rotation);
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			ZNetScene.instance.Destroy(base.gameObject);
		}
	}
}
