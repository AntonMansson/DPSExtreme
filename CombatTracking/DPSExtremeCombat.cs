using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria;

namespace DPSExtreme.CombatTracking
{
	internal partial class DPSExtremeCombat
	{
		internal enum CombatType
		{
			BossFight,
			Invasion,
			General
		}

		internal CombatType myCombatType;
		internal int myBossOrInvasionType;

		DateTime myStartTime;

		public Dictionary<int, DPSExtremeInfoList<DamageDealtInfo>> myDamageDealtPerNPCType = new Dictionary<int, DPSExtremeInfoList<DamageDealtInfo>>();

		internal DPSExtremeInfoList<DamageDealtInfo> myTotalDamageDealtList = new DPSExtremeInfoList<DamageDealtInfo>();
		internal DPSExtremeInfoList<DamageDealtInfo> myDPSList = new DPSExtremeInfoList<DamageDealtInfo>();

		public DPSExtremeCombat(CombatType aCombatType, int aBossOrInvasionType)
		{
			myCombatType = aCombatType;
			myBossOrInvasionType = aBossOrInvasionType;
			myStartTime = DateTime.Now;
		}

		internal void AddDealtDamage(NPC aDamagedNPC, int aDamageDealer, int aDamage)
		{
			int npcRemainingHealth = 0;
			int npcMaxHealth = 0;
			aDamagedNPC.GetLifeStats(out npcRemainingHealth, out npcMaxHealth);
			npcRemainingHealth += aDamage; //damage has already been applied when we reach this point. But we're interested in the value pre-damage

			int clampedDamageAmount = Math.Clamp(aDamage, 0, npcRemainingHealth); //Avoid overkill

			if (!myDamageDealtPerNPCType.ContainsKey(aDamagedNPC.type))
				myDamageDealtPerNPCType.Add(aDamagedNPC.type, new DPSExtremeInfoList<DamageDealtInfo>());

			myDamageDealtPerNPCType[aDamagedNPC.type][aDamageDealer].myDamage += clampedDamageAmount;
			myTotalDamageDealtList[aDamageDealer].myDamage += clampedDamageAmount;
		}

		internal void SendStats()
		{
			try
			{
				ProtocolPushCombatStats push = new ProtocolPushCombatStats();
				push.myCombatIsActive = true;
				push.myTotalDamageDealtList = myTotalDamageDealtList;

				DPSExtreme.instance.packetHandler.SendProtocol(push);

				if (Main.netMode == NetmodeID.Server)
				{
					Dictionary<byte, int> stats = new Dictionary<byte, int>();
					for (int i = 0; i < 256; i++)
					{
						if (myTotalDamageDealtList[i].myDamage > -1)
						{
							stats[(byte)i] = myTotalDamageDealtList[i].myDamage;
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
				DamageDealtInfo damageInfo = myTotalDamageDealtList[i];

				if (damageInfo.myDamage > 0)
				{
					if (i == (int)InfoListIndices.NPCs)
					{
						sb.Append(string.Format("{0}: {1}, ", Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("TrapsTownNPC")), damageInfo.myDamage));
					}
					if (i == (int)InfoListIndices.DOTs)
					{
						sb.Append(string.Format("{0}: {1}, ", Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("DamageOverTime")), damageInfo.myDamage));
					}
					else
					{
						sb.Append(string.Format("{0}: {1}, ", Main.player[i].name, damageInfo.myDamage));
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
