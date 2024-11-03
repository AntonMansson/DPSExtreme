﻿
using DPSExtreme.Combat;
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

			for (int i = DPSExtremeCombatTracker.ourHistorySize - 1; i >= 0; i--)
			{
				DPSExtremeCombat combat = DPSExtreme.instance.combatTracker.GetCombatHistory(i);

				if (combat == null)
					continue;

				UISelectionDisplayEntry entry = CreateEntry(entryIndex) as UISelectionDisplayEntry;
				entry.myColor = DPSExtremeUI.chatColor[Math.Abs(i) % DPSExtremeUI.chatColor.Length];
				entry.myNameText = myNameCallback != null ? myNameCallback(i) : "No name callback";
				entry.myIndex = i;
				entryIndex++;
			}

			Recalculate();
		}

		protected override void OnSelect(int aSelectedIndex)
		{
			DPSExtremeCombat combat = DPSExtreme.instance.combatTracker.GetCombatHistory(aSelectedIndex);

			if (combat == null)
				return;

			DPSExtremeUI.instance.myDisplayedCombat = combat;
			DPSExtremeUI.instance.myDisplayMode = DPSExtremeUI.instance.myPreviousDisplayMode;

		}
	}
}
