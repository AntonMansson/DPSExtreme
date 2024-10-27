using System;
using Terraria;
using Terraria.ID;
using static DPSExtreme.CombatTracking.DPSExtremeCombat;

namespace DPSExtreme.CombatTracking
{
	internal class DPSExtremeCombatTracker
	{
		const int ourHistorySize = 5;
		const int ourGenericCombatTimeout = 5;

		private int myCurrentHistoryIndex = 0;
		private DPSExtremeCombat[] myCombatHistory = new DPSExtremeCombat[ourHistorySize];
		internal DPSExtremeCombat myActiveCombat = null;

		internal int myLastFrameInvasionType = InvasionID.None;

		internal DPSExtremeCombat GetCombatHistory(int aIndex)
		{
			return myCombatHistory[(aIndex + myCurrentHistoryIndex) % ourHistorySize];
		}

		internal void Update()
		{
			UpdateInvasionCheckStart();
			UpdateInvasionCheckEnd();
			myLastFrameInvasionType = Main.invasionType;

			UpdateAllBossesDeadCheck();
			UpdateGenericCombatTimeoutCheck();
		}

		void UpdateInvasionCheckStart()
		{
			if (Main.invasionType == InvasionID.None)
				return;

			if (myLastFrameInvasionType != InvasionID.None)
				return;

			//Wait for invasion delay?

			ProtocolPushStartCombat push = new ProtocolPushStartCombat();
			push.myCombatType = CombatType.Invasion;
			push.myBossOrInvasionType = Main.invasionType;

			DPSExtreme.instance.packetHandler.SendProtocol(push);
		}

		void UpdateInvasionCheckEnd()
		{
			if (myActiveCombat == null)
				return;

			if ((myActiveCombat.myCombatTypeFlags & CombatType.Invasion) == 0)
				return;

			if (Main.invasionType != InvasionID.None)
				return;

			ProtocolPushEndCombat push = new ProtocolPushEndCombat();
			push.myCombatType = CombatType.Invasion;
			DPSExtreme.instance.packetHandler.SendProtocol(push);
		}

		private void UpdateAllBossesDeadCheck()
		{
			if (myActiveCombat == null)
				return;

			if (((myActiveCombat.myCombatTypeFlags & CombatType.BossFight) == 0))
				return;

			bool bossAlive = false;

			foreach (NPC npc in Main.ActiveNPCs)
			{
				if (!npc.boss)
					continue;

				bossAlive = true;
			}

			if (bossAlive)
				return;

			ProtocolPushEndCombat push = new ProtocolPushEndCombat();
			push.myCombatType = CombatType.BossFight;
			DPSExtreme.instance.packetHandler.SendProtocol(push);
		}

		void UpdateGenericCombatTimeoutCheck()
		{
			if (myActiveCombat == null)
				return;

			if (((myActiveCombat.myCombatTypeFlags & CombatType.Generic) == 0))
				return;

			TimeSpan elapsedSinceLastActivity = DateTime.Now - myActiveCombat.myLastActivityTime;

			if (elapsedSinceLastActivity.TotalSeconds < ourGenericCombatTimeout)
				return;

			ProtocolPushEndCombat push = new ProtocolPushEndCombat();
			push.myCombatType = CombatType.Generic;
			DPSExtreme.instance.packetHandler.SendProtocol(push);
		}

		internal void StartCombat(CombatType aCombatType, int aBossOrInvasionType = -1)
		{
			if (myActiveCombat != null)
			{
				UpgradeCombat(aCombatType, aBossOrInvasionType);
				return;
			}

			if (Main.netMode == NetmodeID.SinglePlayer)
				Main.NewText(String.Format("Started combat of type: {0}", aCombatType.ToString()));

			myActiveCombat = new DPSExtremeCombat(aCombatType, aBossOrInvasionType);

			DPSExtremeUI.instance.OnCombatStarted(myActiveCombat);
		}

		//Boss fight starts during an invastion etc
		internal void UpgradeCombat(CombatType aCombatType, int aBossOrInvasionType = -1)
		{
			int oldHighestCombat = (int)myActiveCombat.myHighestCombatType;
			myActiveCombat.myHighestCombatType = (CombatType)Math.Max((int)myActiveCombat.myHighestCombatType, (int)aCombatType);
			myActiveCombat.myCombatTypeFlags |= aCombatType;

			if ((int)myActiveCombat.myHighestCombatType > oldHighestCombat)
			{
				Main.NewText(String.Format("Upgraded combat from {0} to {1}", ((CombatType)oldHighestCombat).ToString(), aCombatType.ToString()));

				if (aCombatType == CombatType.Invasion || aCombatType == CombatType.BossFight)
				{
					myActiveCombat.myBossOrInvasionType = aBossOrInvasionType;
				}

				DPSExtremeUI.instance.OnCombatUpgraded(myActiveCombat);
			}

			//pass upgrade to clients
		}

		internal void EndCombat(CombatType aCombatType)
		{
			if (myActiveCombat == null)
				return;

			myActiveCombat.myCombatTypeFlags &= ~aCombatType;

			//if combat contains both boss + invasion, wait for both to finish before ending
			if (myActiveCombat.myCombatTypeFlags > CombatType.Generic)
				return;

			if (Main.netMode == NetmodeID.SinglePlayer)
				Main.NewText(String.Format("Ended combat"));

			myCurrentHistoryIndex++;

			myActiveCombat.SendStats();
			myActiveCombat.PrintStats();
			myActiveCombat = null;

			DPSExtremeUI.instance.OnCombatEnded();
		}
	}
}
