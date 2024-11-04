using System;
using Terraria.ID;
using Terraria;
using Terraria.Localization;

namespace DPSExtreme.Combat.Stats
{
	internal struct DamageSource
	{
		internal enum SourceType
		{
			NPC,
			NPCEnd = Projectile - 1,

			Projectile = 20000,
			ProjectileEnd = Item - 1,

			Item = 30000,
			ItemEnd = DOT - 1,

			DOT = 39000,
			DOTEnd = Other - 1,

			Other = 40000,
			Unknown = Other + 1,
		}

		private SourceType _mySourceType = SourceType.Unknown;
		internal SourceType mySourceType
		{
			get => _mySourceType;
			set
			{
				_myDamageCauserAbility -= (int)_mySourceType; //Remove old source type addition
				_mySourceType = value;
				myDamageCauserAbility = _myDamageCauserAbility; //Add new one
			}
		}

		internal int myDamageCauserId = -1;  //NPC id/Player index etc

		private int _myDamageCauserAbility = -1;
		internal int myDamageCauserAbility //Projectile/Item id etc
		{
			get 
			{
				switch (mySourceType)
				{
					case SourceType.NPC:
					case SourceType.Projectile:
					case SourceType.Item:
					case SourceType.DOT:
					case SourceType.Other:
						return _myDamageCauserAbility;
					default:
						return -1;
				}
			}
			set 
			{
				switch (mySourceType)
				{
					case SourceType.NPC:
						_myDamageCauserAbility = value + (int)SourceType.Projectile; //Projectile is intended. Bit hacky for "Melee"
						break;
					case SourceType.Projectile:
						_myDamageCauserAbility = value + (int)SourceType.Projectile;
						break;
					case SourceType.Item:
						_myDamageCauserAbility = value + (int)SourceType.Item;
						break;
					case SourceType.DOT:
						_myDamageCauserAbility = value + (int)SourceType.DOT;
						break;
					case SourceType.Other:
						_myDamageCauserAbility = (int)SourceType.Other;
						break;
					default:
						_myDamageCauserAbility = -1;
						Main.NewText("Invalid source");
						break;
				}
			}
		}

		public DamageSource(SourceType aSourceType)
		{
			mySourceType = aSourceType;
		}

		public static string GetAbilityName(int aAbilityId)
		{
			if (IsInSourceTypeRange(SourceType.NPC, aAbilityId))
				return Lang.GetNPCNameValue(aAbilityId);

			if (aAbilityId == (int)SourceType.Projectile) //ProjectileID.None passed. Kinda hacky, Let's see if it causes problems
				return Language.GetTextValue("Melee");

			if (IsInSourceTypeRange(SourceType.Projectile, aAbilityId))
				return Lang.GetProjectileName(aAbilityId - (int)SourceType.Projectile).Value;

			if (IsInSourceTypeRange(SourceType.Item, aAbilityId))
				return Lang.GetItemNameValue(aAbilityId - (int)SourceType.Item);

			if (IsInSourceTypeRange(SourceType.Other, aAbilityId))
				return Language.GetTextValue("Other");


			return $"Accessor {aAbilityId} not in any range";
		}

		public static bool IsInSourceTypeRange(SourceType aSourceType, int aAbilityId)
		{
			object? parseResult;
			bool parseSuccess = Enum.TryParse(typeof(SourceType), aSourceType.ToString() + "End", true, out parseResult);

			SourceType end = parseSuccess ? (SourceType)parseResult : aSourceType;

			return aAbilityId >= (int)aSourceType && aAbilityId <= (int)end;
		}
	}

	internal class DPSExtremeStatsHandler
	{
		internal void AddDealtDamage(NPC aDamagedNPC, DamageSource aDamageSource, int aDamage)
		{
			NPC realDamagedNPC = aDamagedNPC;
			if (aDamagedNPC.realLife >= 0)
				realDamagedNPC = Main.npc[aDamagedNPC.realLife];

			int npcRemainingHealth = 0;
			int npcMaxHealth = 0;
			realDamagedNPC.GetLifeStats(out npcRemainingHealth, out npcMaxHealth);

			if (Main.netMode != NetmodeID.Server) //Damage has not been applied yet on server
				npcRemainingHealth += aDamage; //damage has already been applied when we reach this point. But we're interested in the value pre-damage

			//Not sure why this happens. Seems like there are multiple hits in a single frame for massive overkills
			if (npcRemainingHealth < 0)
				return;

			int clampedDamageAmount = Math.Clamp(aDamage, 0, npcRemainingHealth); //Avoid overkill

			//Merge all penguin ids into single id etc
			int consolidatedType = NPCID.FromLegacyName(Lang.GetNPCNameValue(realDamagedNPC.type));
			int npcType = consolidatedType > 0 ? consolidatedType : realDamagedNPC.type;

			if (DPSExtreme.instance.combatTracker.myActiveCombat == null)
			{
				DPSExtreme.instance.Logger.Warn("DPSExtreme: Adding damage without active combat");
				Main.NewText("DPSExtreme: Adding damage without active combat");
				return;
			}

			DPSExtreme.instance.combatTracker.myActiveCombat.myEnemyDamageTaken[npcType][aDamageSource.myDamageCauserId] += clampedDamageAmount;

			if (Main.netMode == NetmodeID.Server &&
				aDamageSource.myDamageCauserId < (int)InfoListIndices.SupportedPlayerCount) //MP clients sync their local damage so that we can include item/proj type
			{
				return;
			}

			DPSExtreme.instance.combatTracker.myActiveCombat.myDamageDone[aDamageSource.myDamageCauserId][aDamageSource.myDamageCauserAbility] += clampedDamageAmount;
		}

		internal void AddDamageTaken(Player aDamagedPlayer, DamageSource aDamageSource, int aDamage)
		{
			if (aDamagedPlayer.statLife <= 0)
				return;

			if (DPSExtreme.instance.combatTracker.myActiveCombat == null)
			{
				DPSExtreme.instance.Logger.Warn("DPSExtreme: Adding damage taken without active combat");
				Main.NewText("DPSExtreme: Adding damage taken without active combat");
				return;
			}

			int clampedDamageAmount = Math.Clamp(aDamage, 0, aDamagedPlayer.statLife); //Avoid overkill

			DPSExtreme.instance.combatTracker.myActiveCombat.myDamageTaken[aDamagedPlayer.whoAmI][aDamageSource.myDamageCauserId][aDamageSource.myDamageCauserAbility] += clampedDamageAmount;
		}

		internal void AddDeath(Player aKilledPlayer)
		{
			if (DPSExtreme.instance.combatTracker.myActiveCombat == null)
			{
				DPSExtreme.instance.Logger.Warn("DPSExtreme: Adding death without active combat");
				Main.NewText("DPSExtreme: Adding death without active combat");
				return;
			}

			DPSExtreme.instance.combatTracker.myActiveCombat.myDeaths[aKilledPlayer.whoAmI] += 1;
		}

		internal void AddKill(NPC aKilledNPC, int aKiller)
		{
			if (DPSExtreme.instance.combatTracker.myActiveCombat == null)
			{
				DPSExtreme.instance.Logger.Warn("DPSExtreme: Adding kill without active combat");
				Main.NewText("DPSExtreme: Adding kill without active combat");
				return;
			}

			DPSExtreme.instance.combatTracker.myActiveCombat.myKills[aKiller][aKilledNPC.type] += 1;
		}
	}
}
