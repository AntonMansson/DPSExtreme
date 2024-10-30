using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria;
using DPSExtreme.UIElements;

namespace DPSExtreme.CombatTracking
{
	internal partial class DPSExtremeCombat
	{
		[Flags]
		internal enum CombatType
		{
			//Order matters. Highest value is considered most important and will be used when displaying title of combat
			Generic = 1 << 0,
			Event = 1 << 1,
			Invasion = 1 << 2,
			BossFight = 1 << 3
		}

		internal enum InvasionType
		{
			None = 0,

			//Matching Terraria.InvasionID
			GoblinArmy = 1,
			SnowLegion = 2,
			PirateInvasion = 3,
			MartianMadness = 4,

			ModdedInvasionsStart = 5,
			//Range for modded invasions
			ModdedInvasionsEnd = 1000,

			//Outside of Terraria.InvasionID. Gets set based on bools
			PumpkinMoon = 1001,
			FrostMoon = 1002,
			OldOnesArmy = 1003,
			End
		}

		internal enum EventType
		{
			BloodMoon = InvasionType.End,
			Eclipse,
			SlimeRain,
			//No support for modded events
		}

		internal CombatType myCombatTypeFlags;
		internal CombatType myHighestCombatType;
		internal int myBossOrInvasionOrEventType = -1;

		internal DateTime myStartTime;
		internal DateTime myLastActivityTime;

		public Dictionary<int, DPSExtremeInfoList> myEnemyDamageTaken = new Dictionary<int, DPSExtremeInfoList>();

		internal DPSExtremeInfoList myDamageDoneList = new DPSExtremeInfoList();
		internal DPSExtremeInfoList myDPSList = new DPSExtremeInfoList();

		public DPSExtremeCombat(CombatType aCombatType, int aBossOrInvasionOrEventType)
		{
			myCombatTypeFlags = aCombatType;
			myHighestCombatType = aCombatType;
			myBossOrInvasionOrEventType = aBossOrInvasionOrEventType;
			myStartTime = DateTime.Now;
			myLastActivityTime = myStartTime;
		}

		internal object GetInfoContainer(ListDisplayMode aDisplayMode)
		{
			switch (aDisplayMode)
			{
				case ListDisplayMode.DamageDone:
					return myDamageDoneList;
				case ListDisplayMode.DamagePerSecond:
					return myDPSList;
				case ListDisplayMode.EnemyDamageTaken:
					return myEnemyDamageTaken;
				case ListDisplayMode.Count:
				default:
					return null;
			}
		}

		internal void AddDealtDamage(NPC aDamagedNPC, int aDamageDealer, int aDamage)
		{
			int npcRemainingHealth = 0;
			int npcMaxHealth = 0;
			aDamagedNPC.GetLifeStats(out npcRemainingHealth, out npcMaxHealth);
			npcRemainingHealth += aDamage; //damage has already been applied when we reach this point. But we're interested in the value pre-damage

			//Not sure why this happens. Seems like there are multiple hits in a single frame for massive overkills
			if (npcRemainingHealth < 0)
				return;

			int clampedDamageAmount = Math.Clamp(aDamage, 0, npcRemainingHealth); //Avoid overkill

			//Merge all penguin ids into single id etc
			int consolidatedType = NPCID.FromLegacyName(Lang.GetNPCNameValue(aDamagedNPC.type));
			int npcType = consolidatedType > 0 ? consolidatedType : aDamagedNPC.type;

			if (!myEnemyDamageTaken.ContainsKey(npcType))
				myEnemyDamageTaken.Add(npcType, new DPSExtremeInfoList());

			myEnemyDamageTaken[npcType][aDamageDealer] += clampedDamageAmount;
			myDamageDoneList[aDamageDealer] += clampedDamageAmount;
		}

		internal void OnPlayerLeft(int aPlayer)
		{
			//Move player's stats into designated part of the buffer for disconnected players
			for (int i = (int)InfoListIndices.DisconnectedPlayersStart; i < (int)InfoListIndices.DisconnectedPlayersEnd; i++)
			{
				if (myDamageDoneList[i] > -1)
					continue;

				ReassignStats(aPlayer, i);
				break;
			}
		}

		internal void ReassignStats(int aFrom, int aTo)
		{
			myDamageDoneList[aTo] = myDamageDoneList[aFrom];

			foreach ((int npcType, DPSExtremeInfoList damageInfo) in myEnemyDamageTaken)
			{
				myEnemyDamageTaken[npcType][aTo] = myEnemyDamageTaken[npcType][aFrom];
			}

			myDPSList[aTo] = myDPSList[aFrom];

			ClearStatsForPlayer(aFrom);
		}

		internal void ClearStatsForPlayer(int aPlayer)
		{
			myDamageDoneList[aPlayer] = -1;

			foreach ((int npcType, DPSExtremeInfoList damageInfo) in myEnemyDamageTaken)
				myEnemyDamageTaken[npcType][aPlayer] = -1;

			myDPSList[aPlayer] = -1;
		}

		internal void SendStats()
		{
			try
			{
				if (Main.netMode != NetmodeID.Server)
					return;

				ProtocolPushCombatStats push = new ProtocolPushCombatStats();
				push.myCombatIsActive = true;
				push.myEnemyDamageTaken = myEnemyDamageTaken;
				push.myDamageDoneList = myDamageDoneList;

				DPSExtreme.instance.packetHandler.SendProtocol(push);

				if (Main.netMode == NetmodeID.Server)
				{
					Dictionary<byte, int> stats = new Dictionary<byte, int>();
					for (int i = 0; i < 256; i++)
					{
						if (myDamageDoneList[i] > -1)
						{
							stats[(byte)i] = myDamageDoneList[i];
						}
					}

					DPSExtreme.instance.InvokeOnSimpleBossStats(stats);
				}

			}
			catch (Exception)
			{
				//ErrorLogger.Log("SendStats" + e.Message);
			}
		}

		internal void PrintStats()
		{
			StringBuilder sb = new StringBuilder();
			//sb.Append(Language.GetText(DPSExtreme.instance.GetLocalizationKey("DamageStatsForNPC")).Format(Lang.GetNPCNameValue(npc.type)));
			// Add DamageStatsForCombat line

			for (int i = 0; i < 256; i++)
			{
				int damage = myDamageDoneList[i];

				if (damage > 0)
				{
					if (i == (int)InfoListIndices.NPCs)
					{
						sb.Append(string.Format("{0}: {1}, ", Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("TrapsTownNPC")), damage));
					}
					if (i == (int)InfoListIndices.DOTs)
					{
						sb.Append(string.Format("{0}: {1}, ", Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("DamageOverTime")), damage));
					}
					else
					{
						sb.Append(string.Format("{0}: {1}, ", Main.player[i].name, damage));
					}
				}
			}

			if (sb.Length > 2)
				sb.Length -= 2; // removes last ,

			Color messageColor = Color.Orange;

			if (Main.netMode == NetmodeID.Server)
			{
				ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(sb.ToString()), messageColor);
			}
			else if (Main.netMode == NetmodeID.SinglePlayer)
			{
				Main.NewText(sb.ToString(), messageColor);
			}
		}
	}
}
