﻿
using Humanizer;
using System;
using Terraria.Localization;
using Terraria;
using Terraria.ModLoader.UI.Elements;
using static DPSExtreme.CombatTracking.DPSExtremeCombat;
using Microsoft.Xna.Framework;

namespace DPSExtreme.UIElements
{
	internal class UIListDisplay : UICombatInfoDisplay
	{
		DPSExtremeInfoList myInfoList;

		internal void SetInfo(DPSExtremeInfoList aInfo)
		{
			myInfoList = aInfo;
		}

		internal override void Update()
		{
			RecalculateTotals();
			UpdateValues();

			Recalculate();
		}

		internal override void RecalculateTotals()
		{
			myInfoList?.GetMaxAndTotal(out myHighestValue, out myTotal);
		}

		internal override void UpdateValues()
		{
			if (myInfoList == null)
				return;

			UIListDisplayEntry.ourColorCount = 0;

			int entryIndex = 0;

			for (int i = 0; i < myInfoList.Size(); i++)
			{
				int value = myInfoList[i];
				if (value > 0)
				{
					string name = Main.player[i].name;
					if (i >= (int)InfoListIndices.DisconnectedPlayersStart && i <= (int)InfoListIndices.DisconnectedPlayersEnd)
					{
						name = Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("DisconnectedPlayer"));
					}
					else if (i == (int)InfoListIndices.NPCs || i == (int)InfoListIndices.Traps)
					{
						name = Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("TrapsTownNPC"));
					}
					else if (i == (int)InfoListIndices.DOTs)
					{
						name = Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("DamageOverTime"));
					}

					UIListDisplayEntry entry = null;

					if (entryIndex >= _items.Count)
					{
						entry = new UIListDisplayEntry(name, i);
						Add(entry);
					}
					else
					{
						entry = _items[entryIndex] as UIListDisplayEntry;
					}

					entry.SetValues(value, myHighestValue, myTotal);
					entryIndex++;
				}
			}
		}
	}
}
