﻿using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.IO;
using System;
using Terraria.UI;
using DPSExtreme.UIElements;
using Microsoft.Xna.Framework;
using DPSExtreme.CombatTracking;

// TODO, mod to share fog of war maps
// TODO: Death counter for each boss
// TODO: Track events in some way
// TODO: Track damage per weapon, minion, mounts, etc.
// TODO: Track damage by team
// TODO: Simple way to toggle UI other than hotkey.

/*
 * 
 * A Mod to track damage by player.
 * 
 * Stage 1: Start stop chat commands, dps message after boss kill?
 * Total damage per boss?
 * just share dps values from other players?
 * 
 * Current: No debuff damage is counted
 * 
 * Live DPS in the gui.
 * Toggle by command
 * Non-zero users report every once in a while?
 * draw latest
 * 
 */

// TODO: Graph window.

/* Testing 
 * SP: Brain: works, sometimes overkill damage since going below 0 on hit.
 * ''   ''   Stardust dragon only, works, twins only works. mix, works
 * '' ''   NPC only: no message. NPc and a few hits: 953? --> problems?
 * '' KingSlime: Pirates only: 2019:
 *  '' brain pirates only: 1004, again: damage added together, no 
 * slime top row no message.
 * 2nd row: less than 2000
 * 3rd: little less
 * stylist: no damage! --> unable to capture TownNPC Melee attacks, no hook?
 * 
 * flame trap: 1869 -> DOT?
 * spiky ball: 2000+, works.
 * 
 * skeletron: only head is counted/reported
 * EOW: no message: not .boss until loot I guess.
 * Destroyer: no message? real life not working? -> onhit fixes: mult: deathbed fixes: 1, works. WOF works now too.
 * WOF: no message :(
 * Twins: only 1 message, since boss is set to false in npc loot
 * 
 * 
 * 
 * Limitations: DOT, town melee attacks. EOW. Some manual, like Twins
 */

// Share DPS every 3 seconds
// On boss kill, sync damages 1 last time, send message
// Every 3 seconds, send current boss damages.
//  client sees new boss, switch to that display?

/*
June 29 2022 Testing Bugs:
Events can be tallyed up totalling kills and damage to all event enemies. Boss Checklist integration to get the npc types
After boss dies, shows Zombie for some reason sometimes
Golem still bugged
Switching back to DPS automatically somehow
Track player damage taken maybe? Deaths?
*/

namespace DPSExtreme
{

	// MessageID.StrikeNPC is sent from client to server to inform of damaging an npc.
	// Forwarded to other clients, but other clients don't know who did the damage since that is not part of the message.
	// Server keeps track
	// Sends out post-boss stats
	// Sends out dps on interval

	internal class DPSExtreme : Mod
	{
		internal const int UPDATEDELAY = 60; // 1 seconds. Configurable later?

		internal static DPSExtreme instance;

		internal ModKeybind ToggleTeamDPSHotKey;

		internal DPSExtremeTool dpsExtremeTool;

		internal DPSExtremeCombatTracker combatTracker;
		internal DPSExtremePacketHandler packetHandler;

		private int lastSeenScreenWidth;
		private int lastSeenScreenHeight;

		internal event Action<Dictionary<byte, int>> OnSimpleBossStats;

		// NPCLoader.StrikeNPC doesn't specify which player dealt the damage.

		internal static int bossIndex = -1;
		//internal static bool ShowTeamDPS;

		public override void Load()
		{
			instance = this;
			//ShowTeamDPS = false;
			ToggleTeamDPSHotKey = KeybindLoader.RegisterKeybind(this, "ToggleTeamDPSBossMeter", "F4"); // F4?
		}

		public override void PostSetupContent()
		{
			if (!Main.dedServ)
			{
				dpsExtremeTool = new DPSExtremeTool();
			}

			packetHandler = new DPSExtremePacketHandler();
			combatTracker = new DPSExtremeCombatTracker();
		}

		public override void Unload()
		{
			instance = null;
			ToggleTeamDPSHotKey = null;
		}

		// 255 is server damage, 256 is server whoami

		public override void HandlePacket(BinaryReader reader, int whoAmI)
		{
			packetHandler.HandlePacket(reader, whoAmI);
		}

		//private void OutData()
		//{
		//	StringBuilder sb = new StringBuilder();
		//	sb.Append("DPS: ");
		//	for (int i = 0; i < 256; i++)
		//	{
		//		int playerDamage = dpss[i];
		//		if (playerDamage > 0)
		//		{
		//			if (i == 255)
		//			{
		//				sb.Append($"Traps/TownNPC: {playerDamage}, ");
		//			}
		//			else
		//			{
		//				sb.Append($"{Main.player[i].name}: {playerDamage}, ");
		//			}
		//		}
		//	}
		//	Main.NewText(sb.ToString());
		//}

		public void UpdateUI(GameTime gameTime)
		{
			dpsExtremeTool?.UIUpdate(gameTime);
		}

		public void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
		{
			int inventoryLayerIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
			if (inventoryLayerIndex != -1)
			{
				layers.Insert(inventoryLayerIndex, new LegacyGameInterfaceLayer(
					"DPSExtreme: Team DPS",
					delegate
					{
						if (lastSeenScreenWidth != Main.screenWidth || lastSeenScreenHeight != Main.screenHeight)
						{
							dpsExtremeTool.ScreenResolutionChanged();
							lastSeenScreenWidth = Main.screenWidth;
							lastSeenScreenHeight = Main.screenHeight;
						}
						dpsExtremeTool.UIDraw();

						//if (!Main.ingameOptionsWindow && !Main.playerInventory/* && !Main.achievementsWindow*/)
						//	if (DPSExtreme.ShowTeamDPS)
						//	{
						//		OutDataNew();
						//	}

						return true;
					},
					InterfaceScaleType.UI)
				);
			}
		}

		//private void OutDataNew()
		//{
		//	int y = 130;
		//	//Main.spriteBatch.DrawString(Main.fontMouseText, "Team DPS: ", new Vector2(20, y), Color.White, 0f, default(Vector2), scale, SpriteEffects.None, 0f);
		//	Main.spriteBatch.DrawString(Main.fontMouseText, "Team DPS: ", new Vector2(20, y), Color.White);
		//	y += 20;
		//	for (int i = 0; i < 256; i++)
		//	{
		//		int playerDamage = dpss[i];
		//		if (playerDamage > 0)
		//		{
		//			if (i == 255)
		//			{
		//				Main.spriteBatch.DrawString(Main.fontMouseText, $"Traps/TownNPC: {playerDamage}", new Vector2(20, y), Color.White);
		//			}
		//			else
		//			{
		//				Main.spriteBatch.DrawString(Main.fontMouseText, $"{Main.player[i].name}: {playerDamage}", new Vector2(20, y), Color.White);
		//			}
		//			y += 20;
		//		}
		//	}
		//}

		public override object Call(params object[] args)
		{
			try
			{
				string message = args[0] as string;
				if (message == "RegisterForSimpleBossDamageStats")
				{
					Action<Dictionary<byte, int>> callback = args[1] as Action<Dictionary<byte, int>>;
					OnSimpleBossStats += callback;
					return "RegisterSuccess";
				}
				else
				{
					Logger.Warn("DPSExtreme Call Error: Unknown Message: " + message);
				}
			}
			catch (Exception e)
			{
				Logger.Warn("DPSExtreme Call Error: " + e.StackTrace + e.Message);
			}
			return "Failure";
		}

		public void RegisterForSimpleBossDamageStats(Action<Dictionary<byte, int>> callback)
		{
			OnSimpleBossStats += callback;
		}

		internal void InvokeOnSimpleBossStats(Dictionary<byte, int> stats)
		{
			OnSimpleBossStats?.Invoke(stats);
		}
	}

	public class DPSExtremeSystem : ModSystem
	{
		public override bool HijackGetData(ref byte messageType, ref BinaryReader reader, int playerNumber) => ModContent.GetInstance<DPSExtreme>().packetHandler.HijackGetData(ref messageType, ref reader, playerNumber);

		public override void UpdateUI(GameTime gameTime) => ModContent.GetInstance<DPSExtreme>().UpdateUI(gameTime);

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) => ModContent.GetInstance<DPSExtreme>().ModifyInterfaceLayers(layers);
	}

	public static class DPSExtremeInterface
	{
		public static void RegisterForSimpleBossDamageStats(Action<Dictionary<byte, int>> callback)
		{
			DPSExtreme.instance.RegisterForSimpleBossDamageStats(callback);
		}
	}
}

