using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;

namespace DPSExtreme.CombatTracking
{
	internal class DPSExtremeCombatTracker
	{
		internal DPSExtremeCombat[] myCombatHistory = new DPSExtremeCombat[5];
		internal DPSExtremeCombat myActiveCombat = null;

		internal int myLastFrameInvasionType = InvasionID.None;

		internal void Update()
		{
			UpdateInvasionCheckStart();
			UpdateInvasionCheckEnd();
			myLastFrameInvasionType = Main.invasionType;
		}

		void UpdateInvasionCheckStart()
		{
			if (Main.invasionType == InvasionID.None)
				return;

			if (myLastFrameInvasionType != InvasionID.None)
				return;

			//Do this through protocol for MP support
			StartCombat(DPSExtremeCombat.CombatType.Invasion, Main.invasionType);
		}

		void UpdateInvasionCheckEnd()
		{
			if (myLastFrameInvasionType != InvasionID.None)
				return;

			if (Main.invasionType == InvasionID.None)
				return;

			//Do this through protocol for MP support
			EndCombat(DPSExtremeCombat.CombatType.Invasion, Main.invasionType);
		}

		internal void StartCombat(DPSExtremeCombat.CombatType aCombatType, int aBossOrInvasionType = -1)
		{
			if (myActiveCombat != null)
			{
				//Possibly upgrade type?
				return;
			}

			if (Main.netMode == NetmodeID.SinglePlayer)
				Main.NewText(String.Format("Started combat of type: {0}", aCombatType.ToString()));

			//Look into starting invasion combats.
			//	And think about upgrading combat types.
			//	What happens if we have a general combat and boss spawns. New combat or just upgrade?

			myActiveCombat = new DPSExtremeCombat(aCombatType, aBossOrInvasionType);

			DPSExtremeUI.instance.OnCombatStarted(myActiveCombat);
		}
	}
}
