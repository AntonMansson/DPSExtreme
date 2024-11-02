using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
using MonoMod.Utils;
using System.IO;
using System.Collections.Generic;

namespace DPSExtreme
{
	internal class DPSExtremeModWorld : ModSystem
	{
		public override void PostUpdateWorld()
		{
			if ((Main.GameUpdateCount % DPSExtreme.UPDATEDELAY) != 0)
				return;

			DPSExtreme.instance.combatTracker.Update();

			if (DPSExtreme.instance.combatTracker.myActiveCombat == null)
				return;

			UpdateDOTDPS();
			UpdateBroadcasts();
		}

		void UpdateBroadcasts()
		{
			if (Main.netMode != NetmodeID.SinglePlayer && Main.netMode != NetmodeID.Server)
				return;

			ProtocolPushClientDPSs push = new ProtocolPushClientDPSs();
			push.myDamagePerSecond = DPSExtreme.instance.combatTracker.myActiveCombat.myDamagePerSecond;

			DPSExtreme.instance.packetHandler.SendProtocol(push);
			DPSExtreme.instance.combatTracker.myActiveCombat.SendStats();
		}

		void UpdateDOTDPS()
		{
			if (Main.netMode != NetmodeID.SinglePlayer && Main.netMode != NetmodeID.Server)
				return;

			Combat.DPSExtremeCombat activeCombat = DPSExtreme.instance.combatTracker.myActiveCombat;

			//Best-effort DOT DPS approx.
			int totalDotDPS = 0;

			foreach (NPC npc in Main.ActiveNPCs)
			{
				int dotDPS = -1 * npc.lifeRegen / 2;
				totalDotDPS += dotDPS;

				//Since the dot hook doesn't seem to work in SP, add damage here to the best of our abilities
				if (Main.netMode == NetmodeID.SinglePlayer)
				{
					float ratio = DPSExtreme.UPDATEDELAY / 60f;
					//TODO: Handle remainder
					int dealtDamage = (int)(dotDPS * ratio);
					DPSExtreme.instance.combatTracker.myStatsHandler.AddDealtDamage(npc, (int)InfoListIndices.DOTs, -1, dealtDamage);
				}
			}

			activeCombat.myDamagePerSecond[(int)InfoListIndices.DOTs] = totalDotDPS;
		}
	}
}

