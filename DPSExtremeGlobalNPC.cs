﻿using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System;
using MonoMod.Cil;
using DPSExtreme.CombatTracking;

namespace DPSExtreme
{
	internal class DPSExtremeGlobalNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		internal bool onDeathBed; // SP only flag for something?

		public DPSExtremeGlobalNPC()
		{
		}

		public override void Load()
		{
			Terraria.IL_NPC.UpdateNPC_BuffApplyDOTs += IL_NPC_UpdateNPC_BuffApplyDOTs;
		}

		private void IL_NPC_UpdateNPC_BuffApplyDOTs(MonoMod.Cil.ILContext il)
		{
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
			c.EmitDelegate<Action<int, int>>((int whoAmI, int damage) =>
			{
				// whoAmI already accounts for realLife

				NPC npc = Main.npc[whoAmI];
				//TODO Verify that damage has already been applied when we reach this point (otherwise overkill calculation is incorrect)
				DPSExtreme.instance.combatTracker.myActiveCombat.AddDealtDamage(npc, (int)InfoListIndices.DOTs, damage);

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
			c.EmitDelegate<Action<int, int>>((int whoAmI, int damage) =>
			{
				// whoAmI already accounts for realLife
				NPC npc = Main.npc[whoAmI];

				DPSExtreme.instance.combatTracker.myActiveCombat.AddDealtDamage(npc, (int)InfoListIndices.DOTs, damage);

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
		public override void OnKill(NPC npc)
		{
			try
			{
				//System.Console.WriteLine("NPCLoot");

				if (npc.boss)
				{
					if (Main.netMode == NetmodeID.SinglePlayer)
					{
						onDeathBed = true;
					}
					else
					{
						//Rethink how this is handled. Should happen when combat ends, not just when an npc dies
						DPSExtreme.instance.combatTracker.myActiveCombat.SendStats();
						DPSExtreme.instance.combatTracker.myActiveCombat.PrintStats();
					}
				}
			}
			catch (Exception)
			{
				//ErrorLogger.Log("NPCLoot" + e.Message);
			}
		}

		// Things like townNPC and I think traps will trigger this in Server. In SP, all is done here.
		public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
		{
			try
			{
				//System.Console.WriteLine("OnHitByItem " + player.whoAmI);

				NPC damagedNPC = npc;
				if (npc.realLife >= 0)
				{
					damagedNPC = Main.npc[damagedNPC.realLife];
				}

				DPSExtreme.instance.combatTracker.myActiveCombat.AddDealtDamage(damagedNPC, player.whoAmI, damageDone);

				DPSExtremeGlobalNPC info = damagedNPC.GetGlobalNPC<DPSExtremeGlobalNPC>();

				if (info.onDeathBed) // oh wait, is this the same as .active in this case? probably not.
				{
					//Same thing here, should happen OnCombatEnd
					DPSExtreme.instance.combatTracker.myActiveCombat.SendStats();
					info.onDeathBed = false; // multiple things can hit while on deathbed.
				}
			}
			catch (Exception)
			{
				//ErrorLogger.Log("OnHitByItem" + e.Message);
			}
		}

		// Things like townNPC and I think traps will trigger this in Server. In SP, all is done here.
		public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
		{
			//TODO, owner could be -1?
			try
			{
				//System.Console.WriteLine("OnHitByProjectile " + projectile.owner);
				NPC damagedNPC = npc;
				if (npc.realLife >= 0)
				{
					damagedNPC = Main.npc[damagedNPC.realLife];
				}

				int projectileOwner = projectile.owner;

				/*Temp hack to assign npc projectiles to npc table. Necessary for them to appear in list on SP clients
				whoIsMyParent could be used to diffirentiate between individual npcs in the future. And could also seperate other damage sources like traps apart from npcs*/
				if (projectile.GetGlobalProjectile<DPSExtremeModProjectile>().whoIsMyParent != -1)
					projectileOwner = (int)InfoListIndices.NPCs;

				DPSExtreme.instance.combatTracker.myActiveCombat.AddDealtDamage(damagedNPC, projectileOwner, damageDone);

				DPSExtremeGlobalNPC info = damagedNPC.GetGlobalNPC<DPSExtremeGlobalNPC>();

				if (info.onDeathBed)
				{
					//OnCombatEnd
					DPSExtreme.instance.combatTracker.myActiveCombat.SendStats();
					info.onDeathBed = false;
				}
			}
			catch (Exception)
			{
				//ErrorLogger.Log("OnHitByProjectile" + e.Message);
			}
		}
	}
}

