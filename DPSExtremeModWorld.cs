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

			DPSExtreme.instance.combatTracker.Update();

			if (DPSExtreme.instance.combatTracker.myActiveCombat == null)
				return;

			if (Main.netMode == NetmodeID.Server || Main.netMode == NetmodeID.SinglePlayer)
			{
				ProtocolPushClientDPSs push = new ProtocolPushClientDPSs();
				push.myDamagePerSecond = DPSExtreme.instance.combatTracker.myActiveCombat.myDamagePerSecond;

				DPSExtreme.instance.packetHandler.SendProtocol(push);
				DPSExtreme.instance.combatTracker.myActiveCombat.SendStats();
			}
		}
	}
}

