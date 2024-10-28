using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
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
			if (Main.netMode != NetmodeID.SinglePlayer && Main.netMode != NetmodeID.Server)
				return;

			UpdateInvasionCheckStart();
			UpdateInvasionCheckEnd();
			myLastFrameInvasionType = Main.invasionType;

			UpdateAllBossesDeadCheck();
			UpdateGenericCombatTimeoutCheck();
		}

		void UpdateInvasionCheckStart()
		{
			//TODO: Fix issue with combat not being started if invasion is active when world loads

			if (Main.invasionType == InvasionID.None)
				return;

			if (myLastFrameInvasionType != InvasionID.None)
				return;

			//Wait for invasion delay?

			TriggerCombat(CombatType.Invasion, Main.invasionType);
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

			//Needs to happen before it gets sent to clients
			if (Main.netMode == NetmodeID.Server)
				DPSExtreme.instance.packetHandler.HandleEndCombatPush(push);

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

			//Needs to happen before it gets sent to clients
			if (Main.netMode == NetmodeID.Server)
				DPSExtreme.instance.packetHandler.HandleEndCombatPush(push);

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

			//Needs to happen before it gets sent to clients
			if (Main.netMode == NetmodeID.Server)
				DPSExtreme.instance.packetHandler.HandleEndCombatPush(push);

			DPSExtreme.instance.packetHandler.SendProtocol(push);
		}

		//Called when damage is dealt or bosses spawn etc
		internal void TriggerCombat(CombatType aCombatType, int aBossOrInvasionType = -1)
		{
			if (myActiveCombat != null)
			{
				UpgradeCombat(aCombatType, aBossOrInvasionType);
				myActiveCombat.myLastActivityTime = DateTime.Now;
				return;
			}

			ProtocolPushStartCombat push = new ProtocolPushStartCombat();
			push.myCombatType = aCombatType;
			push.myBossOrInvasionType = aBossOrInvasionType;

			if (Main.netMode == NetmodeID.Server)
				DPSExtreme.instance.packetHandler.HandleStartCombatPush(push);

			DPSExtreme.instance.packetHandler.SendProtocol(push);
		}

		//Called from TriggerCombat or from server pushing combat status
		internal void StartCombat(CombatType aCombatType, int aBossOrInvasionType = -1)
		{
			if (myActiveCombat != null)
			{
				UpgradeCombat(aCombatType, aBossOrInvasionType);
				Main.NewText("Upgrade through StartCombat. Should probably never happen");
				return;
			}

			if (Main.netMode == NetmodeID.SinglePlayer || Main.netMode == NetmodeID.MultiplayerClient)
				Main.NewText(String.Format("Started combat of type: {0}", aCombatType.ToString()));
			else if (Main.netMode == NetmodeID.Server)
				ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("Server started combat"), Color.Orange);

			myActiveCombat = new DPSExtremeCombat(aCombatType, aBossOrInvasionType);

			DPSExtremeUI.instance?.OnCombatStarted(myActiveCombat);
		}

		//Boss fight starts during an invastion etc
		internal void UpgradeCombat(CombatType aCombatType, int aBossOrInvasionType = -1)
		{
			int oldHighestCombat = (int)myActiveCombat.myHighestCombatType;
			myActiveCombat.myHighestCombatType = (CombatType)Math.Max((int)myActiveCombat.myHighestCombatType, (int)aCombatType);
			myActiveCombat.myCombatTypeFlags |= aCombatType;

			if ((int)myActiveCombat.myHighestCombatType > oldHighestCombat)
			{
				if (Main.netMode == NetmodeID.SinglePlayer || Main.netMode == NetmodeID.MultiplayerClient)
					Main.NewText(String.Format("Upgraded combat from {0} to {1}", ((CombatType)oldHighestCombat).ToString(), aCombatType.ToString()));
				else if (Main.netMode == NetmodeID.Server)
					ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(String.Format("Server upgraded combat from {0} to {1}", ((CombatType)oldHighestCombat).ToString(), aCombatType.ToString())), Color.Orange);

				if (aCombatType == CombatType.Invasion || aCombatType == CombatType.BossFight)
				{
					myActiveCombat.myBossOrInvasionType = aBossOrInvasionType;
				}

				DPSExtremeUI.instance?.OnCombatUpgraded(myActiveCombat);

				if (Main.netMode == NetmodeID.Server)
				{
					ProtocolPushUpgradeCombat push = new ProtocolPushUpgradeCombat();
					push.myCombatType = myActiveCombat.myHighestCombatType;
					push.myBossOrInvasionType = myActiveCombat.myBossOrInvasionType;

					DPSExtreme.instance.packetHandler.SendProtocol(push);
				}
			}
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
			else if (Main.netMode == NetmodeID.Server)
				ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral("Ended combat"), Color.White);

			myCurrentHistoryIndex++;

			myActiveCombat.SendStats();
			myActiveCombat.PrintStats();
			myActiveCombat = null;

			DPSExtremeUI.instance?.OnCombatEnded();
		}
	}
}
