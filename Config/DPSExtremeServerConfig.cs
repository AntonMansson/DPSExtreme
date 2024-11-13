using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace DPSExtreme.Config
{
	internal class DPSExtremeServerConfig : ModConfig
	{
		public static DPSExtremeServerConfig Instance = null;

		public override ConfigScope Mode => ConfigScope.ServerSide;

		[Header("GeneralConfigHeader")]
		[Increment(5)]
		[Range(10, 120)]
		[DefaultValue(30)]
		[TooltipKey("$Mods.DPSExtreme.Configs.DPSExtremeServerConfig.RefreshRate.Tooltip")]
		public int RefreshRate;

		[Header("CombatConfigHeader")]
		[DefaultValue(false)]
		public bool IgnoreCritters;

		public override void OnChanged()
		{
			if (DPSExtremeUI.instance != null)
				DPSExtremeUI.instance.OnServerConfigLoad();

			if (DPSExtreme.instance != null)
				DPSExtreme.instance.OnServerConfigLoad();
		}
	}
}
