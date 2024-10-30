using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI.Elements;
using static DPSExtreme.CombatTracking.DPSExtremeCombat;

namespace DPSExtreme.UIElements
{
	internal class UIBreakdownableDisplay : UICombatInfoDisplay
	{
		internal Dictionary<int, DPSExtremeInfoList> myInfoLookup;
		internal Func<int, string> myNameCallback;
		int myHighlightedKey = -1;

		internal void SetInfo(Dictionary<int, DPSExtremeInfoList> aInfoLookup)
		{
			myInfoLookup = aInfoLookup;
		}

		internal override void Update()
		{
			if (myInfoLookup == null)
				return;

			Clear();

			RecalculateTotals();
			UpdateValues();

			Recalculate();
		}

		internal override void RecalculateTotals()
		{
			myHighestValue = 0;
			myTotal = 0;

			foreach ((int npcType, DPSExtremeInfoList damageInfo) in myInfoLookup)
			{
				int listMax = 0;
				int listTotal = 0;

				damageInfo.GetMaxAndTotal(out listMax, out listTotal);
				myHighestValue = Math.Max(myHighestValue, listTotal); //Yes, listTotal is correct
				myTotal += listTotal;
			}
		}

		internal override void UpdateValues()
		{
			UIListDisplayEntry.ourColorCount = 0;

			int entryIndex = 0;

			foreach ((int npcType, DPSExtremeInfoList damageInfo) in myInfoLookup)
			{
				int listMax = 0;
				int listTotal = 0;

				damageInfo.GetMaxAndTotal(out listMax, out listTotal);

				if (listTotal > 0)
				{
					string name = "Missing name callback";
					if (myNameCallback != null)
						name = myNameCallback.Invoke(npcType);

					UIListDisplayEntry entry = null;

					if (entryIndex >= _items.Count)
					{
						entry = new UIListDisplayEntry(name);
						Add(entry);
					}
					else
					{
						entry = _items[entryIndex] as UIListDisplayEntry;
					}

					entry.SetValues(listTotal, myHighestValue, myTotal);
					entryIndex++;
				}
			}
		}
	}
}
