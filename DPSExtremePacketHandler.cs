using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using System.Runtime.InteropServices.JavaScript;

namespace DPSExtreme
{
	internal class DPSExtremePacketHandler
	{
		//Allows us to unify SP & MP code flows into a single function call
		public void SendProtocol(DPSExtremeProtocol aProtocol)
		{
			if (Main.netMode == NetmodeID.Server)
			{
				ModPacket netMessage = DPSExtreme.instance.GetPacket();
				aProtocol.ToStream(netMessage);
				netMessage.Send();
			}
			else if (Main.netMode == NetmodeID.SinglePlayer)
			{
				HandleProtocol(aProtocol.GetDelimiter(), aProtocol);
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
				case DPSExtremeMessageType.InformClientsCurrentBossTotals:
					{
						protocol = new ProtocolPushBossFightStats();
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

					DPSExtremeGlobalNPC info = damagedNPC.GetGlobalNPC<DPSExtremeGlobalNPC>();
					info.damageDone[playerNumber] += damage;
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
				case DPSExtremeMessageType.InformClientsCurrentBossTotals: HandleBossFightStatsPush(aProtocol as ProtocolPushBossFightStats); break;
				default: DPSExtreme.instance.Logger.Warn("DPSExtreme: Unknown Message type: " + aDelimiter); break;
			}
		}

		public void HandleInformServerDPSReq(ProtocolReqInformServerCurrentDPS aReq)
		{
			DPSExtreme.dpss[aReq.myPlayer] = aReq.myDPS;
		}

		public void HandleClientDPSsPush(ProtocolPushClientDPSs aPush)
		{
			for (int i = 0; i < 256; i++)
				DPSExtreme.dpss[i] = -1;

			for (int i = 0; i < aPush.myPlayerCount; i++)
				DPSExtreme.dpss[aPush.myPlayerIndices[i]] = aPush.myPlayerDPSs[i];

			DPSExtremeUI.instance.updateNeeded = true;
		}

		public void HandleBossFightStatsPush(ProtocolPushBossFightStats aPush)
		{
			bool dead = aPush.myBossIsDead;
			DPSExtreme.bossIndex = aPush.myBossIndex;

			for (int i = 0; i < 256; i++)
				DPSExtreme.bossDamage[i] = -1;

			for (int i = 0; i < aPush.myPlayerCount; i++)
			{
				byte playerIndex = aPush.myPlayerIndices[i];
				int playerdps = aPush.myPlayerDPSs[i];

				DPSExtreme.bossDamage[playerIndex] = playerdps;
			}

			DPSExtreme.bossDamageDOT = aPush.myBossDamageTakenFromDOT;
			DPSExtreme.bossDamageDOTDPS = -1 * Main.npc[DPSExtreme.bossIndex].lifeRegen / 2;
			DPSExtremeUI.instance.updateNeeded = true;
			DPSExtremeUI.instance.bossUpdateNeeded = true;

			//if (dead)
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
