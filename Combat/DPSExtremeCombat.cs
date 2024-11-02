using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria;
using DPSExtreme.Combat.Stats;
using DPSExtreme.UIElements.Displays;

namespace DPSExtreme.Combat
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

		internal DPSExtremeStatDictionary<int, DPSExtremeStatList<StatValue>> myEnemyDamageTaken = new DPSExtremeStatDictionary<int, DPSExtremeStatList<StatValue>>();
		internal DPSExtremeStatList<DPSExtremeStatDictionary<int, StatValue>> myDamageDone = new DPSExtremeStatList<DPSExtremeStatDictionary<int, StatValue>>();
		internal DPSExtremeStatList<StatValue> myDamagePerSecond = new DPSExtremeStatList<StatValue>();
		
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
					return myDamageDone;
				case ListDisplayMode.DamagePerSecond:
					return myDamagePerSecond;
				case ListDisplayMode.EnemyDamageTaken:
					return myEnemyDamageTaken;
				default:
					return null;
			}
		}

		internal void OnPlayerLeft(int aPlayer)
		{
			//Move player's stats into designated part of the buffer for disconnected players
			for (int i = (int)InfoListIndices.DisconnectedPlayersStart; i < (int)InfoListIndices.DisconnectedPlayersEnd; i++)
			{
				if (myDamageDone[i].HasStats())
					continue;

				ReassignStats(aPlayer, i);
				break;
			}
		}

		internal void ReassignStats(int aFrom, int aTo)
		{
			myDamageDone[aTo] = myDamageDone[aFrom];

			foreach ((int npcType, DPSExtremeStatList<StatValue> damageInfo) in myEnemyDamageTaken)
			{
				myEnemyDamageTaken[npcType][aTo] = myEnemyDamageTaken[npcType][aFrom];
			}

			myDamagePerSecond[aTo] = myDamagePerSecond[aFrom];

			ClearStatsForPlayer(aFrom);
		}

		internal void ClearStatsForPlayer(int aPlayer)
		{
			myDamageDone[aPlayer].Clear();

			foreach ((int npcType, DPSExtremeStatList<StatValue> damageInfo) in myEnemyDamageTaken)
				myEnemyDamageTaken[npcType][aPlayer] = -1;

			myDamagePerSecond[aPlayer] = -1;
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
				push.myDamageDone = myDamageDone;

				DPSExtreme.instance.packetHandler.SendProtocol(push);

				if (Main.netMode == NetmodeID.Server)
				{
					Dictionary<byte, int> stats = new Dictionary<byte, int>();
					for (int i = 0; i < 256; i++)
					{
						if (myDamageDone[i].HasStats())
						{
							int max = 0;
							int participantTotal = 0;
							myDamageDone[i].GetMaxAndTotal(out max, out participantTotal);

							stats[(byte)i] = participantTotal;
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
				int max = 0;
				int participantDamage = 0;
				myDamageDone[i].GetMaxAndTotal(out max, out participantDamage);

				if (participantDamage > 0)
				{
					if (i == (int)InfoListIndices.NPCs)
					{
						sb.Append(string.Format("{0}: {1}, ", Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("TrapsTownNPC")), participantDamage));
					}
					if (i == (int)InfoListIndices.DOTs)
					{
						sb.Append(string.Format("{0}: {1}, ", Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("DamageOverTime")), participantDamage));
					}
					else
					{
						sb.Append(string.Format("{0}: {1}, ", Main.player[i].name, participantDamage));
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
