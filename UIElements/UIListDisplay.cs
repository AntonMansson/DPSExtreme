
using Terraria.Localization;
using Terraria;

namespace DPSExtreme.UIElements
{
	internal class UIListDisplay : UICombatInfoDisplay
	{
		DPSExtremeInfoList myInfoList
		{
			get 
			{
				if (myInfoOverrideList != null)
					return myInfoOverrideList;

				return DPSExtremeUI.instance.myDisplayedCombat?.GetInfoContainer(myDisplayMode) as DPSExtremeInfoList;
			}
		}

		internal DPSExtremeInfoList myInfoOverrideList = null;

		internal UIListDisplay(ListDisplayMode aDisplayMode) : base(aDisplayMode) { }

		public override void OnActivate()
		{
		}

		public override void OnDeactivate()
		{
			DPSExtremeUI.instance.myRootPanel.RemoveChild(this);
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
						entry = new UIListDisplayEntry(i);
						Add(entry);
					}
					else
					{
						entry = _items[entryIndex] as UIListDisplayEntry;
					}

					entry.myParticipantIndex = i;
					entry.myColor = DPSExtremeUI.chatColor[i % DPSExtremeUI.chatColor.Length];
					entry.myNameText = name;
					entry.SetValues(value, myHighestValue, myTotal);
					entryIndex++;
				}
			}

			//In case no new entries were added but they need to be re-sorted
			UpdateOrder();
		}
	}
}
