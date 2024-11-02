using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria;

namespace DPSExtreme.Combat.Stats
{
	internal class DPSExtremeStatsHandler
	{
		internal void AddDealtDamage(NPC aDamagedNPC, int aDamageDealer, int aItemOrProjectileType, int aDamage)
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

			DPSExtreme.instance.combatTracker.myActiveCombat.myEnemyDamageTaken[npcType][aDamageDealer] += clampedDamageAmount;

			if (Main.netMode != NetmodeID.Server) //MP clients sync their local damage so that we can include item/proj type
				DPSExtreme.instance.combatTracker.myActiveCombat.myDamageDone[aDamageDealer][aItemOrProjectileType] += clampedDamageAmount;
		}
	}
}
