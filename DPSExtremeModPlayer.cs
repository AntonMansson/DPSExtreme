using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameInput;
using System.Collections.Generic;

namespace DPSExtreme
{
	internal class DPSExtremeModPlayer : ModPlayer
	{
		internal static List<int> ourConnectedPlayers = new List<int>();
		public override void PlayerDisconnect()
		{
			if (Main.netMode != NetmodeID.Server)
				return;

			foreach (int playerIndex in ourConnectedPlayers)
			{
				if (Main.player[playerIndex].active)
					continue;

				ourConnectedPlayers.Remove(playerIndex);
				DPSExtreme.instance?.combatTracker.OnPlayerLeft(playerIndex);

				break;
			}
		}

		public override void PostUpdate()
		{
			if (Main.GameUpdateCount % DPSExtreme.UPDATEDELAY != 0)
				return;

			if (DPSExtreme.instance.combatTracker.myActiveCombat == null)
				return;

			if (Player.whoAmI != Main.myPlayer)
				return;

			if (!Player.accDreamCatcher)
				return;

			int dps = Player.getDPS();
			if (!Player.dpsStarted)
				dps = 0;

			ProtocolReqShareCurrentDPS req = new ProtocolReqShareCurrentDPS();
			req.myPlayer = Player.whoAmI;
			req.myDPS = dps;
			req.myDamageDoneBreakdown = DPSExtreme.instance.combatTracker.myActiveCombat.myDamageDone[Player.whoAmI];

			DPSExtreme.instance.packetHandler.SendProtocol(req);
		}

		public override void OnEnterWorld()
		{
			DPSExtreme.instance.combatTracker.OnEnterWorld();
			DPSExtremeUI.instance.OnEnterWorld();
		}

		public override void ProcessTriggers(TriggersSet triggersSet)
		{
			if (DPSExtreme.instance.ToggleTeamDPSHotKey.JustPressed)
			{
				DPSExtremeUI.instance.ShowTeamDPSPanel = !DPSExtremeUI.instance.ShowTeamDPSPanel;
			}
		}
	}
}

