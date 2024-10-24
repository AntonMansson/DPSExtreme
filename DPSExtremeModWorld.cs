using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using MonoMod.Utils;
using System.IO;
using System.Collections.Generic;

namespace DPSExtreme
{
	// Takes care of regularly sending out DPS values to clients.
	// Do we even need to send?
	internal class DPSExtremeModWorld : ModSystem
	{
		public override void PostUpdateWorld()
		{
			if ((Main.GameUpdateCount % DPSExtreme.UPDATEDELAY) != 0)
				return;

			//Find boss which is closest to dying
			byte bossIndex = 255;
			float maxProgress = -1f;
			for (byte i = 0; i < 200; i++)
			{
				NPC npc = Main.npc[i];
				if (npc.active && npc.boss && (npc.realLife == -1 || npc.realLife == npc.whoAmI))
				{
					//NPC realNPC = npc.realLife >= 0 ? Main.npc[npc.realLife] : npc;
					float deathProgress = 1f - ((float)npc.life / npc.lifeMax);
					if (deathProgress > maxProgress)
					{
						maxProgress = deathProgress;
						bossIndex = i;
					}
				}
			}

			if (Main.netMode == NetmodeID.Server || Main.netMode == NetmodeID.SinglePlayer)
			{
				PushDPSsToClients();

				if (bossIndex != 255)
				{
					PushBossTotalsToClients(bossIndex);
				}
			}
		}

		private void PushDPSsToClients()
		{
			ProtocolPushClientDPSs push = new ProtocolPushClientDPSs();

			for (int i = 0; i < 256; i++)
			{
				if (Main.player[i].active && Main.player[i].accDreamCatcher)
				{
					push.myPlayerCount++;
					push.myPlayerIndices.Add((byte)i);
					push.myPlayerDPSs.Add(DPSExtreme.dpss[i]);
				}
			}

            DPSExtreme.instance.packetHandler.SendProtocol(push);
		}

		private void PushBossTotalsToClients(byte aBossIndex)
		{
			ProtocolPushBossFightStats push = new ProtocolPushBossFightStats();
			push.myBossIsDead = true;
			push.myBossIndex = aBossIndex;
			
			DPSExtremeGlobalNPC bossGlobalNPC = Main.npc[aBossIndex].GetGlobalNPC<DPSExtremeGlobalNPC>();

			for (int i = 0; i < 256; i++)
			{
				if (bossGlobalNPC.damageDone[i] > 0)
				{
					push.myPlayerCount++;

					push.myPlayerIndices.Add((byte)i);
					push.myPlayerDPSs.Add(bossGlobalNPC.damageDone[i]);
				}
			}

			push.myBossDamageTakenFromDOT = bossGlobalNPC.damageDOT;

            DPSExtreme.instance.packetHandler.SendProtocol(push);
		}
	}
}

