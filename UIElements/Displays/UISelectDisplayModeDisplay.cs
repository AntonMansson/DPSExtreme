
using DPSExtreme.Combat;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace DPSExtreme.UIElements.Displays
{
	internal class UISelectDisplayModeDisplay : UISelectionDisplay
	{
		internal UISelectDisplayModeDisplay()
			: base(ListDisplayMode.DisplayModeSelect, GetEntryName)
		{

		}

		internal static string GetEntryName(int aIndex)
		{
			return ((ListDisplayMode)aIndex).ToString();
		}

		protected override void PopulateEntries()
		{
			int entryIndex = 0;

			for (int i = (int)ListDisplayMode.StatDisplaysStart + 1; i < (int)ListDisplayMode.StatDisplaysEnd; i++)
			{
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
			DPSExtremeUI.instance.myDisplayMode = (ListDisplayMode)aSelectedIndex;
		}
	}
}
