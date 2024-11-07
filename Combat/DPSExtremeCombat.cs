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
		internal DateTime myEndTime = DateTime.MinValue;

		internal TimeSpan myDuration
		{
			get
			{
				if (myEndTime == DateTime.MinValue)
					return DateTime.Now - myStartTime;

				return myEndTime - myStartTime;
			}
		}

		internal string myFormattedDuration => String.Format("{0:D2}:{1:D2}", (int)Math.Floor(myDuration.TotalMinutes), myDuration.Seconds);

		internal DPSExtremeStatDictionary<int, DPSExtremeStatList<StatValue>> myEnemyDamageTaken = new();

		internal DPSExtremeStatList<DPSExtremeStatDictionary<int, StatValue>> myDamageDone = new();
		internal DPSExtremeStatList<DPSExtremeStatDictionary<int, DPSExtremeStatDictionary<int, StatValue>>> myDamageTaken = new();
		internal DPSExtremeStatList<StatValue> myDeaths = new();
		internal DPSExtremeStatList<DPSExtremeStatDictionary<int, StatValue>> myKills = new();
		internal DPSExtremeStatList<DPSExtremeStatDictionary<int, StatValue>> myManaUsed = new();

		internal DPSExtremeStatList<DPSExtremeStatDictionary<int, TimeStatValue>> myBuffUptimes = new();
		internal DPSExtremeStatList<DPSExtremeStatDictionary<int, TimeStatValue>> myDebuffUptimes = new();

		internal DPSExtremeStatList<StatValue> myDamagePerSecond = new();
		
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
				case ListDisplayMode.DamagePerSecond:
					return myDamagePerSecond;
				case ListDisplayMode.DamageDone:
					return myDamageDone;
				case ListDisplayMode.DamageTaken:
					return myDamageTaken;
				case ListDisplayMode.EnemyDamageTaken:
					return myEnemyDamageTaken;
				case ListDisplayMode.Deaths:
					return myDeaths;
				case ListDisplayMode.Kills:
					return myKills;
				case ListDisplayMode.ManaUsed:
					return myManaUsed;
				case ListDisplayMode.BuffUptime:
					return myBuffUptimes;
				case ListDisplayMode.DebuffUptime:
					return myDebuffUptimes;
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
				push.myDamageTaken = myDamageTaken;
				push.myDeaths = myDeaths;
				push.myKills = myKills;
				push.myManaUsed = myManaUsed;
				push.myBuffUptimes = myBuffUptimes;
				push.myDebuffUptimes = myDebuffUptimes;

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
						sb.Append(string.Format("{0}: {1}, ", Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("TownNPC")), participantDamage));
					}
					else if (i == (int)InfoListIndices.Traps)
					{
						sb.Append(string.Format("{0}: {1}, ", Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("Traps")), participantDamage));
					}
					else if (i == (int)InfoListIndices.DOTs)
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

		internal string GetTitle()
		{
			switch (myHighestCombatType)
			{
				case CombatType.BossFight:
					if (myBossOrInvasionOrEventType > -1)
					{
						string bossName = Lang.GetNPCNameValue(myBossOrInvasionOrEventType);
						return Language.GetText(bossName).Value;
					}
					else
					{
						return Language.GetText(DPSExtreme.instance.GetLocalizationKey("NoBoss")).Value;
					}
				case CombatType.Invasion:
					InvasionType invasionType;
					if (myBossOrInvasionOrEventType >= (int)InvasionType.ModdedInvasionsStart &&
						myBossOrInvasionOrEventType < (int)InvasionType.ModdedInvasionsEnd)
					{
						invasionType = InvasionType.ModdedInvasionsStart;
					}
					else
					{
						invasionType = (InvasionType)myBossOrInvasionOrEventType;
					}

					switch (invasionType)
					{
						case InvasionType.GoblinArmy:
							return Language.GetTextValue("Bestiary_Invasions.Goblins");
						case InvasionType.SnowLegion:
							return Language.GetTextValue("Bestiary_Invasions.FrostLegion");
						case InvasionType.PirateInvasion:
							return Language.GetTextValue("Bestiary_Invasions.Pirates");
						case InvasionType.MartianMadness:
							return Language.GetTextValue("Bestiary_Invasions.Martian");
						case InvasionType.PumpkinMoon:
							return Language.GetTextValue("Bestiary_Invasions.PumpkinMoon");
						case InvasionType.FrostMoon:
							return Language.GetTextValue("Bestiary_Invasions.FrostMoon");
						case InvasionType.OldOnesArmy:
							return Language.GetTextValue("Bestiary_Invasions.OldOnesArmy");
						case InvasionType.ModdedInvasionsStart:
							//TODO: Boss checklist support to fetch name?
							return Language.GetTextValue("Invasion");
						default:
							return Language.GetTextValue("Invasion");
					}
				case CombatType.Event:
					switch ((EventType)myBossOrInvasionOrEventType)
					{
						case EventType.BloodMoon:
							return Language.GetTextValue("Bestiary_Events.BloodMoon");
						case EventType.Eclipse:
							return Language.GetTextValue("Bestiary_Events.Eclipse");
						case EventType.SlimeRain:
							return Language.GetTextValue("Bestiary_Events.SlimeRain");
						default:
							return Language.GetTextValue("Event");
					}
				case CombatType.Generic:
					//Maybe display name of first npc hit?
					return Language.GetTextValue("Combat");
				default:
					return "Unknown combat type";
			}
		}
	}
}
