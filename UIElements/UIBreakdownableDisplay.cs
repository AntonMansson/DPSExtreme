﻿using System;
using System.Collections.Generic;
using Terraria.UI;

namespace DPSExtreme.UIElements
{
	internal class UIBreakdownableDisplay : UICombatInfoDisplay
	{
		internal Dictionary<int, DPSExtremeInfoList> myInfoLookup;
		internal Func<int, string> myNameCallback;

		UIListDisplay myBreakdownDisplay = null;

		internal void SetInfo(Dictionary<int, DPSExtremeInfoList> aInfoLookup)
		{
			myInfoLookup = aInfoLookup;

			Clear();

			if (myBreakdownDisplay != null)
			{

				Remove(myBreakdownDisplay);
				myBreakdownDisplay = null;
			}
		}

		internal override void Update()
		{
			if (myInfoLookup == null)
				return;

			RecalculateTotals();
			UpdateValues();

			Recalculate();
		}

		internal override void RecalculateTotals()
		{
			if (myBreakdownDisplay != null)
			{
				myBreakdownDisplay.RecalculateTotals();
				return;
			}

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
			if (myBreakdownDisplay != null)
			{
				myBreakdownDisplay.UpdateValues();
				return;
			}

			int entryIndex = 0;

			foreach ((int baseKey, DPSExtremeInfoList damageInfo) in myInfoLookup)
			{
				int listMax = 0;
				int listTotal = 0;

				damageInfo.GetMaxAndTotal(out listMax, out listTotal);

				if (listTotal > 0)
				{
					string name = "Missing name callback";
					if (myNameCallback != null)
						name = myNameCallback.Invoke(baseKey);

					UIListDisplayEntry entry = null;

					if (entryIndex >= _items.Count)
					{
						entry = new UIListDisplayEntry();
						entry.OnLeftClick += OnClickBaseEntry;
						Add(entry);
					}
					else
					{
						entry = _items[entryIndex] as UIListDisplayEntry;
					}

					entry.myColor = DPSExtremeUI.chatColor[baseKey % DPSExtremeUI.chatColor.Length];
					entry.myNameText = name;
					entry.myBaseKey = baseKey;
					entry.SetValues(listTotal, myHighestValue, myTotal);
					entryIndex++;
				}
			}

			//In case no new entries were added but they need to be re-sorted
			UpdateOrder();
		}

		private void OnClickBaseEntry(UIMouseEvent evt, UIElement listeningElement)
		{
			Clear();
			UIListDisplayEntry entry = listeningElement as UIListDisplayEntry;

			myBreakdownDisplay = new UIListDisplay();
			myBreakdownDisplay.SetInfo(myInfoLookup[entry.myBaseKey]);
			myBreakdownDisplay.OnRightClick += OnRightClickBreakdownDisplay;
			Add(myBreakdownDisplay);

			DPSExtremeUI.instance.updateNeeded = true;
		}

		private void OnRightClickBreakdownDisplay(UIMouseEvent evt, UIElement listeningElement)
		{
			Remove(myBreakdownDisplay);
			myBreakdownDisplay = null;

			DPSExtremeUI.instance.updateNeeded = true;
		}
	}
}
