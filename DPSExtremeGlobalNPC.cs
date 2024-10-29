using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using Terraria.Localization;
using MonoMod.Cil;

namespace DPSExtreme
{
	internal class DPSExtremeGlobalNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		internal int[] damageDone;
		internal int damageDOT; // damage from damage over time buffs.
		internal bool onDeathBed; // SP only flag for something?

		public DPSExtremeGlobalNPC() {
			damageDone = new int[256];
		}

		public override void Load() {
			Terraria.IL_NPC.UpdateNPC_BuffApplyDOTs += IL_NPC_UpdateNPC_BuffApplyDOTs;
		}

		private void AddDamageReceived(NPC aDamagedNPC, int aDamageSource, int aDamageAmount) {
			DPSExtremeGlobalNPC info = aDamagedNPC.GetGlobalNPC<DPSExtremeGlobalNPC>();

			int npcRemainingHealth = 0;
			int npcMaxHealth = 0;
			aDamagedNPC.GetLifeStats(out npcRemainingHealth, out npcMaxHealth);
			npcRemainingHealth += aDamageAmount; //damage has already been applied when we reach this point. But we're interested in the value pre-damage

			int clampedDamageAmount = Math.Clamp(aDamageAmount, 0, npcRemainingHealth); //Avoid overkill
			info.damageDone[aDamageSource] += clampedDamageAmount;
		}

		private void IL_NPC_UpdateNPC_BuffApplyDOTs(MonoMod.Cil.ILContext il) {
			// with realLife, worm npc take more damage. Eater of worlds doesn't use realLife, each takes damage individually. Eventually need to account for this?

			var c = new ILCursor(il);

			c.GotoNext( // I guess before CombatText.NewText would also work...
				MoveType.After,
				i => i.MatchLdsfld<Main>(nameof(Main.npc)),
				i => i.MatchLdloc(18),
				i => i.MatchLdelemRef(),
				i => i.MatchDup(),
				i => i.MatchLdfld(typeof(NPC), nameof(NPC.life)),
				i => i.MatchLdloc(0),
				i => i.MatchSub(),
				i => i.MatchStfld(typeof(NPC), nameof(NPC.life))
			);
			c.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_S, (byte)18);
			c.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_0);
			c.EmitDelegate<Action<int, int>>((int whoAmI, int damage) => {
				// whoAmI already accounts for realLife
				DPSExtremeGlobalNPC info = Main.npc[whoAmI].GetGlobalNPC<DPSExtremeGlobalNPC>();
				info.damageDOT += damage;
				//Main.NewText($"Detected DOT: {Main.npc[whoAmI].FullName}, {damage}");
			});

			c.GotoNext(
				MoveType.After,
				i => i.MatchLdsfld<Main>(nameof(Main.npc)),
				i => i.MatchLdloc(19),
				i => i.MatchLdelemRef(),
				i => i.MatchDup(),
				i => i.MatchLdfld(typeof(NPC), nameof(NPC.life)),
				i => i.MatchLdcI4(1),
				i => i.MatchSub(),
				i => i.MatchStfld(typeof(NPC), nameof(NPC.life))
			);
			c.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_S, (byte)19);
			c.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_1);
			c.EmitDelegate<Action<int, int>>((int whoAmI, int damage) => {
				// whoAmI already accounts for realLife
				DPSExtremeGlobalNPC info = Main.npc[whoAmI].GetGlobalNPC<DPSExtremeGlobalNPC>();
				info.damageDOT += damage;
				//Main.NewText($"Detected DOT: {Main.npc[whoAmI].FullName}, {damage}");
			});
		}

		//public override GlobalNPC Clone()
		//{
		//	try
		//	{
		//		DPSExtremeGlobalNPC clone = (DPSExtremeGlobalNPC)base.Clone();
		//		clone.damageDone = new int[256];
		//		return clone;
		//	}
		//	catch (Exception e)
		//	{
		//		//ErrorLogger.Log("Clone" + e.Message);
		//	}
		//	return null;
		//}

		// question, in MP, is this called before or after last hit?
		public override void OnKill(NPC npc) {
			try {
				//System.Console.WriteLine("NPCLoot");

				if (npc.boss) {
					if (Main.netMode == NetmodeID.SinglePlayer) {
						onDeathBed = true;
					}
					else {
						SendStats(npc);
					}
				}
			}
			catch (Exception) {
				//ErrorLogger.Log("NPCLoot" + e.Message);
			}
		}

		void SendStats(NPC npc) {
			try {
				//System.Console.WriteLine("SendStats");

				StringBuilder sb = new StringBuilder();
				sb.Append(Language.GetText(DPSExtreme.instance.GetLocalizationKey("DamageStatsForNPC")).Format(Lang.GetNPCNameValue(npc.type)));
				for (int i = 0; i < 256; i++) {
					int playerDamage = damageDone[i];
					if (playerDamage > 0) {
						if (i == 255) {
							sb.Append(string.Format("{0}: {1}, ", Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("TrapsTownNPC")), playerDamage));
						}
						else {
							sb.Append(string.Format("{0}: {1}, ", Main.player[i].name, playerDamage));
						}
					}
				}
				if (damageDOT > 0) {
					sb.Append(string.Format("{0}: {1}, ", Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("DamageOverTime")), damageDOT));
				}
				sb.Length -= 2; // removes last ,
				Color messageColor = Color.Orange;

				ProtocolPushBossFightStats push = new ProtocolPushBossFightStats();
				push.myBossIsDead = true;
				push.myBossIndex = (byte)npc.whoAmI;

				DPSExtremeGlobalNPC bossGlobalNPC = npc.GetGlobalNPC<DPSExtremeGlobalNPC>();
				for (int i = 0; i < 256; i++) {
					if (bossGlobalNPC.damageDone[i] > 0) {
						push.myPlayerCount++;

						push.myPlayerIndices.Add((byte)i);
						push.myPlayerDPSs.Add(bossGlobalNPC.damageDone[i]);
					}
				}

				push.myBossDamageTakenFromDOT = bossGlobalNPC.damageDOT;
				// No need to send DOT dps.

				DPSExtreme.instance.packetHandler.SendProtocol(push);

				if (Main.netMode == NetmodeID.Server) {
					ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(sb.ToString()), messageColor);

					Dictionary<byte, int> stats = new Dictionary<byte, int>();
					for (int i = 0; i < 256; i++) {
						if (bossGlobalNPC.damageDone[i] > -1) {
							stats[(byte)i] = bossGlobalNPC.damageDone[i];
						}
					}
					// DOT can't be in simple boss stats it seems, would need to adjust call.
					DPSExtreme.instance.InvokeOnSimpleBossStats(stats);
				}
				else if (Main.netMode == NetmodeID.SinglePlayer) {
					Main.NewText(sb.ToString(), messageColor);
				}
			}
			catch (Exception) {
				//ErrorLogger.Log("SendStats" + e.Message);
			}
		}

		// Things like townNPC and I think traps will trigger this in Server. In SP, all is done here.
		public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone) {
			try {
				//System.Console.WriteLine("OnHitByItem " + player.whoAmI);

				NPC damagedNPC = npc;
				if (npc.realLife >= 0) {
					damagedNPC = Main.npc[damagedNPC.realLife];
				}

				AddDamageReceived(damagedNPC, player.whoAmI, damageDone);

				DPSExtremeGlobalNPC info = damagedNPC.GetGlobalNPC<DPSExtremeGlobalNPC>();

				if (info.onDeathBed) // oh wait, is this the same as .active in this case? probably not.
				{
					info.SendStats(damagedNPC);
					info.onDeathBed = false; // multiple things can hit while on deathbed.
				}

				//damageDone[player.whoAmI] += damage;
				//if (onDeathBed)
				//{
				//	SendStats(npc);
				//}
			}
			catch (Exception) {
				//ErrorLogger.Log("OnHitByItem" + e.Message);
			}
		}

		// Things like townNPC and I think traps will trigger this in Server. In SP, all is done here.
		public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone) {
			//TODO, owner could be -1?
			try {
				//System.Console.WriteLine("OnHitByProjectile " + projectile.owner);

				NPC damagedNPC = npc;
				if (npc.realLife >= 0) {
					damagedNPC = Main.npc[damagedNPC.realLife];
				}

				int projectileOwner = projectile.owner;

				/*Temp hack to assign npc projectiles to npc table. Necessary for them to appear in list on SP clients
				whoIsMyParent could be used to diffirentiate between individual npcs in the future. And could also seperate other damage sources like traps apart from npcs*/
				if (projectile.GetGlobalProjectile<DPSExtremeModProjectile>().whoIsMyParent != -1)
					projectileOwner = 255;

				AddDamageReceived(damagedNPC, projectileOwner, damageDone);

				DPSExtremeGlobalNPC info = damagedNPC.GetGlobalNPC<DPSExtremeGlobalNPC>();

				if (info.onDeathBed) {
					info.SendStats(damagedNPC);
					info.onDeathBed = false;
				}

				//damageDone[projectile.owner] += damage;
				//if (onDeathBed)
				//{
				//	SendStats(npc);
				//}
			}
			catch (Exception) {
				//ErrorLogger.Log("OnHitByProjectile" + e.Message);
			}
		}
	}
}

