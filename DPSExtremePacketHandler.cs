using System;
using System.IO;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;

namespace DPSExtreme
{
	internal class DPSExtremePacketHandler
	{
		//Allows us to unify SP & MP code flows into a single function call
		public void SendProtocol(DPSExtremeProtocol aProtocol)
		{
			if (Main.netMode == NetmodeID.SinglePlayer)
			{
				HandleProtocol(aProtocol.GetDelimiter(), aProtocol);
			}
			else
			{
				ModPacket netMessage = DPSExtreme.instance.GetPacket();
				aProtocol.ToStream(netMessage);
				netMessage.Send();
			}
		}

		public bool HandlePacket(BinaryReader reader, int whoAmI)
		{
			DPSExtremeMessageType delimiter = (DPSExtremeMessageType)reader.ReadByte();

			DPSExtremeProtocol protocol = null;

			switch (delimiter)
			{
				case DPSExtremeMessageType.InformServerCurrentDPS:
					{
						protocol = new ProtocolReqInformServerCurrentDPS();
						if (!protocol.FromStream(reader))
							return false;

						break;
					}
				case DPSExtremeMessageType.InformClientsCurrentDPSs:
					{
						protocol = new ProtocolPushClientDPSs();
						if (!protocol.FromStream(reader))
							return false;

						break;
					}
				case DPSExtremeMessageType.InformClientsCurrentCombatTotals:
					{
						protocol = new ProtocolPushCombatStats();
						if (!protocol.FromStream(reader))
							return false;

						break;
					}
				default:
					DPSExtreme.instance.Logger.Warn("DPSExtreme: Unknown Message type: " + delimiter);
					break;
			}

			if (protocol == null)
				DPSExtreme.instance.Logger.Warn("DPSExtreme: null protocol for message type: " + delimiter);

			HandleProtocol(delimiter, protocol);

			return true;
		}

		public bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber)
		{
			try
			{
				if (messageType == MessageID.DamageNPC && Main.netMode == NetmodeID.Server)
				{
					int npcIndex = reader.ReadInt16();
					int damage = reader.Read7BitEncodedInt();
					if (damage < 0)
						return false;
					//ErrorLogger.Log("HijackGetData StrikeNPC: " + npcIndex + " " + damage + " " + playerNumber);

					//System.Console.WriteLine("HijackGetData StrikeNPC: " + npcIndex + " " + damage + " " + playerNumber);
					NPC damagedNPC = Main.npc[npcIndex];
					if (damagedNPC.realLife >= 0)
					{
						damagedNPC = Main.npc[damagedNPC.realLife];
					}

					DPSExtreme.instance.combatTracker.myActiveCombat.AddDealtDamage(damagedNPC, playerNumber, damage);

					// TODO: Reimplement DPS with ring buffer for accurate?  !!! or send 0?
					// TODO: Verify real life adjustment
				}
			}
			catch (Exception)
			{
				//ErrorLogger.Log("HijackGetData StrikeNPC " + e.Message);
			}
			return false;
		}

		private void HandleProtocol(DPSExtremeMessageType aDelimiter, DPSExtremeProtocol aProtocol)
		{
			switch (aDelimiter)
			{
				case DPSExtremeMessageType.InformServerCurrentDPS: HandleInformServerDPSReq(aProtocol as ProtocolReqInformServerCurrentDPS); break;
				case DPSExtremeMessageType.InformClientsCurrentDPSs: HandleClientDPSsPush(aProtocol as ProtocolPushClientDPSs); break;
				case DPSExtremeMessageType.InformClientsCurrentCombatTotals: HandleCombatStatsPush(aProtocol as ProtocolPushCombatStats); break;
				default: DPSExtreme.instance.Logger.Warn("DPSExtreme: Unknown Message type: " + aDelimiter); break;
			}
		}

		public void HandleInformServerDPSReq(ProtocolReqInformServerCurrentDPS aReq)
		{
			DPSExtreme.instance.combatTracker.myActiveCombat.myDPSList[aReq.myPlayer].myDamage = aReq.myDPS;
		}

		public void HandleClientDPSsPush(ProtocolPushClientDPSs aPush)
		{
			DPSExtreme.instance.combatTracker.myActiveCombat.myDPSList = aPush.myDPSList;

			DPSExtremeUI.instance.updateNeeded = true;
		}

		public void HandleCombatStatsPush(ProtocolPushCombatStats aPush)
		{
			CombatTracking.DPSExtremeCombat activeCombat = DPSExtreme.instance.combatTracker.myActiveCombat;

			activeCombat.myDamageDealtPerNPCType = aPush.myDamageDealtPerNPCType;
			activeCombat.myTotalDamageDealtList = aPush.myTotalDamageDealtList;

			//Best-effort DOT DPS approx.
			//TODO: Fix issue with dots appearing before player dpss
			int totalDotDPS = 0;

			foreach (NPC npc in Main.ActiveNPCs)
			{
				int dotDPS = -1 * npc.lifeRegen / 2;
				totalDotDPS += dotDPS;

				//Since the dot hook doesn't seem to work in SP, add damage here to the best of our abilities
				if (Main.netMode == NetmodeID.SinglePlayer)
				{
					if (totalDotDPS > 0 && activeCombat.myTotalDamageDealtList[(int)InfoListIndices.DOTs].myDamage < 0) //Make sure we don't start at -1
						activeCombat.myTotalDamageDealtList[(int)InfoListIndices.DOTs].myDamage = 0;

					float ratio = DPSExtreme.UPDATEDELAY / 60f;
					int dealtDamage = (int)(dotDPS * ratio);
					activeCombat.AddDealtDamage(npc, (int)InfoListIndices.DOTs, dealtDamage);
				}
			}

			if (totalDotDPS > 0)
				activeCombat.myDPSList[(int)InfoListIndices.DOTs].myDamage = totalDotDPS;

			DPSExtremeUI.instance.updateNeeded = true;

			//if (!aPush.myCombatIsActive)
			//{
			//    Dictionary<byte, int> stats = new Dictionary<byte, int>();
			//    for (int i = 0; i < 256; i++)
			//    {
			//        if (DPSExtreme.bossDamage[i] > -1)
			//        {
			//            stats[(byte)i] = DPSExtreme.bossDamage[i];
			//        }
			//    }
			//    DPSExtreme.instance.OnSimpleBossStats?.Invoke(stats);
			//}
		}
	}
}
