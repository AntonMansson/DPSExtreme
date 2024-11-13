
using DPSExtreme.Combat;
using Microsoft.Xna.Framework;
using System;

namespace DPSExtreme.UIElements.Displays
{
	internal class UICombatHistoryDisplay : UISelectionDisplay
	{
		internal UICombatHistoryDisplay()
			: base(ListDisplayMode.CombatHistory, GetEntryName)
		{

		}

		internal static string GetEntryName(int aIndex)
		{
			DPSExtremeCombat combat = DPSExtreme.instance.combatTracker.GetCombatHistory(aIndex);

			if (combat == null)
				return "null combat";

			return combat.GetTitle();
		}

		protected override void PopulateEntries()
		{
			int entryIndex = 0;

			if (DPSExtreme.instance.combatTracker.myActiveCombat != null)
			{
				UISelectionDisplayEntry entry = CreateEntry(entryIndex) as UISelectionDisplayEntry;
				entry.myColor = Color.Yellow;
				entry.myNameText = "Current combat";
				entry.myRightText = DPSExtreme.instance.combatTracker.myActiveCombat.myFormattedDuration;
				entry.myIndex = entryIndex;
				entryIndex++;
			}

			for (int i = DPSExtremeCombatTracker.ourHistorySize - 1; i >= 0; i--)
			{
				DPSExtremeCombat combat = DPSExtreme.instance.combatTracker.GetCombatHistory(i);

				if (combat == null)
					continue;

				UISelectionDisplayEntry entry = CreateEntry(entryIndex) as UISelectionDisplayEntry;
				entry.myColor = DPSExtremeUI.chatColor[Math.Abs(i) % DPSExtremeUI.chatColor.Length];
				entry.myNameText = myNameCallback != null ? myNameCallback(i) : "No name callback";
				entry.myRightText = combat.myFormattedDuration;
				entry.myIndex = i;
				entryIndex++;
			}

			Recalculate();
		}

		protected override void OnSelect(int aSelectedIndex)
		{
			DPSExtremeUI.instance.myDisplayMode = DPSExtremeUI.instance.myPreviousDisplayMode;

			DPSExtremeCombat combat = DPSExtreme.instance.combatTracker.GetCombatHistory(aSelectedIndex);
			if (combat == null)
				return;

			DPSExtremeUI.instance.myDisplayedCombat = combat;
		}
	}
}
