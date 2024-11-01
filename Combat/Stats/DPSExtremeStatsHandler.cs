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
		//For MP clients to store their damage breakdown in before they pass it to server
		internal DPSExtremeStatDictionary<int, StatValue> myDamageDoneBuffer = new DPSExtremeStatDictionary<int, StatValue>();

		internal void OnStartCombat()
		{
			myDamageDoneBuffer = new DPSExtremeStatDictionary<int, StatValue>();
		}

		internal void AddDealtDamage(NPC aDamagedNPC, int aDamageDealer, int aItemOrProjectileType, int aDamage)
		{
			int npcRemainingHealth = 0;
			int npcMaxHealth = 0;
			aDamagedNPC.GetLifeStats(out npcRemainingHealth, out npcMaxHealth);
			npcRemainingHealth += aDamage; //damage has already been applied when we reach this point. But we're interested in the value pre-damage

			//Not sure why this happens. Seems like there are multiple hits in a single frame for massive overkills
			if (npcRemainingHealth < 0)
				return;

			int clampedDamageAmount = Math.Clamp(aDamage, 0, npcRemainingHealth); //Avoid overkill

			//Merge all penguin ids into single id etc
			int consolidatedType = NPCID.FromLegacyName(Lang.GetNPCNameValue(aDamagedNPC.type));
			int npcType = consolidatedType > 0 ? consolidatedType : aDamagedNPC.type;

			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				//Buffered because MP clients send their local DamageDone to server to broadcast since only clients can get info about items/projectiles
				myDamageDoneBuffer[aItemOrProjectileType] += clampedDamageAmount;
				return;
			}
			
			if (DPSExtreme.instance.combatTracker.myActiveCombat == null)
			{
				DPSExtreme.instance.Logger.Warn("DPSExtreme: Adding damage without active combat");
				Main.NewText("DPSExtreme: Adding damage without active combat");
				return;
			}

			DPSExtreme.instance.combatTracker.myActiveCombat.myEnemyDamageTaken[npcType][aDamageDealer] += clampedDamageAmount;
			if (Main.netMode == NetmodeID.SinglePlayer)
			{
				//Buffered to mimic MP client flow
				//If this pattern grows too big consider maybe some solution with BufferedStats<T>
				myDamageDoneBuffer[aItemOrProjectileType] += clampedDamageAmount;
				return;
			}

			DPSExtreme.instance.combatTracker.myActiveCombat.myDamageDone[aDamageDealer][aItemOrProjectileType] += clampedDamageAmount;
		}
	}
}
