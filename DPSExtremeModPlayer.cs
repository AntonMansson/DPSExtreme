using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameInput;
using System.Collections.Generic;
using static DPSExtreme.Combat.DPSExtremeCombat;
using DPSExtreme.Combat.Stats;
using Terraria.Chat;
using Terraria.Localization;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;

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

		public override void OnHurt(Player.HurtInfo aHurtInfo)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			DamageSource damageSource = new DamageSource();
			if (aHurtInfo.DamageSource.SourceOtherIndex != -1)
			{
				damageSource.mySourceType = DamageSource.SourceType.Other;
				damageSource.myDamageCauserId = (int)DamageSource.SourceType.Other;
				damageSource.myDamageCauserAbility = ProjectileID.None;
			}
			else if (aHurtInfo.DamageSource.SourceNPCIndex != -1)
			{
				damageSource.mySourceType = DamageSource.SourceType.NPC;
				damageSource.myDamageCauserId = Main.npc[aHurtInfo.DamageSource.SourceNPCIndex].type;
				damageSource.myDamageCauserAbility = ProjectileID.None;
			}
			else if (aHurtInfo.DamageSource.SourceProjectileType != -1)
			{
				damageSource.mySourceType = DamageSource.SourceType.Projectile;

				Projectile projectile = Main.projectile[aHurtInfo.DamageSource.SourceProjectileLocalIndex];
				DPSExtremeModProjectile dpsProjectile = projectile.GetGlobalProjectile<DPSExtremeModProjectile>();
				int owner = dpsProjectile.whoIsMyParent;

				if (owner == -1)
				{
					ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral($"No owner found for projectile of type: {projectile.type} and local index {aHurtInfo.DamageSource.SourceProjectileLocalIndex}"), Color.Orange);
					return;
				}

				if (owner == (int)InfoListIndices.Traps)
				{
					damageSource.myDamageCauserId = dpsProjectile.myParentItemType + (int)DamageSource.SourceType.Traps;
					damageSource.myDamageCauserAbility = projectile.type;
				}
				else
				{
					NPC parentNPC = Main.npc[owner];

					if (!parentNPC.active)
					{
						Main.NewText("owner was not npc");
						return;
					}

					damageSource.myDamageCauserId = parentNPC.type;
					damageSource.myDamageCauserAbility = projectile.type;
				}
			}

			DPSExtreme.instance.combatTracker.TriggerCombat(CombatType.Generic);
			DPSExtreme.instance.combatTracker.myStatsHandler.AddDamageTaken(Player, damageSource, aHurtInfo.Damage);
		}

		public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			DPSExtreme.instance.combatTracker.TriggerCombat(CombatType.Generic);
			DPSExtreme.instance.combatTracker.myStatsHandler.AddDeath(Player);
		}

		public override void OnConsumeMana(Item item, int manaConsumed)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			DPSExtreme.instance.combatTracker.TriggerCombat(CombatType.Generic);
			DPSExtreme.instance.combatTracker.myStatsHandler.AddConsumedMana(Player, item, manaConsumed);
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

