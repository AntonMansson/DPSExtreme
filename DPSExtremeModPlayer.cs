using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameInput;
using Terraria.Localization;

namespace DPSExtreme
{
	internal class DPSExtremeModPlayer : ModPlayer
	{
		public override void PostUpdate()
		{
			if (Main.GameUpdateCount % DPSExtreme.UPDATEDELAY == 0)
			{
				if (Player.whoAmI == Main.myPlayer && Player.accDreamCatcher)
				{
					int dps = Player.getDPS();
					if (!Player.dpsStarted)
						dps = 0;

					ProtocolReqInformServerCurrentDPS req = new ProtocolReqInformServerCurrentDPS();
					req.myPlayer = Player.whoAmI;
					req.myDPS = dps;

                    DPSExtreme.instance.packetHandler.SendProtocol(req);
				}
			}
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

