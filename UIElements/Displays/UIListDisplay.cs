﻿
using Terraria.Localization;
using Terraria;
using DPSExtreme.Combat.Stats;
using System.Reflection;

namespace DPSExtreme.UIElements.Displays
{
	internal class UIListDisplay<T> : UICombatInfoDisplay
		where T : IStatContainer, new()
	{
		internal DPSExtremeStatList<T> myInfoList
		{
			get 
			{
				try
				{
					if (myParentDisplay != null)
					{
						UIStatDictionaryDisplay<DPSExtremeStatList<T>> parent = myParentDisplay as UIStatDictionaryDisplay<DPSExtremeStatList<T>>;
						if (parent != null)
							return parent.myInfoLookup[myParentDisplay.myBreakdownAccessor];
					}

					return DPSExtremeUI.instance.myDisplayedCombat?.GetInfoContainer(myDisplayMode) as DPSExtremeStatList<T>;
				}
				catch (System.Exception)
				{

					throw;
				}
			}
		}

		private string GetName(int aParticipantIndex)
		{
			if (aParticipantIndex < 0)
				return string.Format("Invalid index: {0}", aParticipantIndex);

			if (aParticipantIndex < (int)InfoListIndices.SupportedPlayerCount)
			{
				return Main.player[aParticipantIndex].name;
			}
			else if (aParticipantIndex >= (int)InfoListIndices.DisconnectedPlayersStart && aParticipantIndex <= (int)InfoListIndices.DisconnectedPlayersEnd)
			{
				return Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("DisconnectedPlayer"));
			}
			else if (aParticipantIndex == (int)InfoListIndices.NPCs || aParticipantIndex == (int)InfoListIndices.Traps)
			{
				return Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("TrapsTownNPC"));
			}
			else if (aParticipantIndex == (int)InfoListIndices.DOTs)
			{
				return Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("DamageOverTime"));
			}

			return string.Format("Invalid index: {0}", aParticipantIndex);
		}

		internal UIListDisplay(ListDisplayMode aDisplayMode) 
			: base(aDisplayMode) 
		{
			myNameCallback = GetName;
		}

		internal override void Update()
		{
			if (myInfoList == null)
				return;

			RecalculateTotals();
			UpdateValues();

			Recalculate();
		}

		internal override void RecalculateTotals()
		{
			if (myIsInBreakdown)
			{
				myBreakdownDisplay.RecalculateTotals();
				return;
			}

			myInfoList?.GetMaxAndTotal(out myHighestValue, out myTotal);
		}

		internal override void UpdateValues()
		{
			if (myIsInBreakdown)
			{
				myBreakdownDisplay.UpdateValues();
				return;
			}

			if (!myInfoList.HasStats())
			{
				Clear();
				return;
			}

			int entryIndex = 0;

			for (int i = 0; i < myInfoList.Size(); i++)
			{
				T value = myInfoList[i];
				if (value.HasStats())
				{
					int max = 0;
					int total = 0;
					value.GetMaxAndTotal(out max, out total);

					string name = "Missing name callback";
					if (myNameCallback != null)
						name = myNameCallback.Invoke(i);

					UIListDisplayEntry entry = CreateEntry(entryIndex) as UIListDisplayEntry;
					entry.myParticipantIndex = i;
					entry.myColor = DPSExtremeUI.chatColor[i % DPSExtremeUI.chatColor.Length];
					entry.myNameText = name;
					entry.SetValues(total, myHighestValue, myTotal);
					entryIndex++;
				}
			}

			//In case no new entries were added but they need to be re-sorted
			UpdateOrder();
		}
	}
}
