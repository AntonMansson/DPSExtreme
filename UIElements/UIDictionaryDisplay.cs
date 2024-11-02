using DPSExtreme.Combat.Stats;
using System;
using System.Collections.Generic;
using System.Reflection;
using Terraria.UI;

namespace DPSExtreme.UIElements
{
	internal class UIStatDictionaryDisplay<T> : UICombatInfoDisplay
		where T : IStat, new()
	{
		internal DPSExtremeStatDictionary<int, T> myInfoLookup
		{
			get
			{
				try
				{
					if (myParentDisplay != null)
					{
						UIListDisplay<DPSExtremeStatDictionary<int, T>> parent = myParentDisplay as UIListDisplay<DPSExtremeStatDictionary<int, T>>;
						if (parent != null)
							return parent.myInfoList[myParentDisplay.myBreakdownAccessor];
					}

					return DPSExtremeUI.instance.myDisplayedCombat?.GetInfoContainer(myDisplayMode) as DPSExtremeStatDictionary<int, T>;
				}
				catch (Exception)
				{

					throw;
				}
			}
		}

		internal UIStatDictionaryDisplay(ListDisplayMode aDisplayMode, Func<int, string> aNameCallback) 
			: base(aDisplayMode, typeof(T).GetTypeInfo()) 
		{ 
			myNameCallback = aNameCallback;
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
			if (myIsInBreakdown)
			{
				myBreakdownDisplay.RecalculateTotals();
				return;
			}

			myHighestValue = 0;
			myTotal = 0;

			foreach ((int npcType, T damageInfo) in myInfoLookup)
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
			if (myIsInBreakdown)
			{
				myBreakdownDisplay.UpdateValues();
				return;
			}

			if (!myInfoLookup.HasStats())
			{
				Clear();
				return;
			}

			int entryIndex = 0;

			foreach ((int baseKey, T damageInfo) in myInfoLookup)
			{
				int listMax = 0;
				int listTotal = 0;

				damageInfo.GetMaxAndTotal(out listMax, out listTotal);

				if (listTotal > 0)
				{
					string name = "Missing name callback";
					if (myNameCallback != null)
					{
							name = myNameCallback.Invoke(baseKey);
					}

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

					entry.myColor = DPSExtremeUI.chatColor[Math.Abs(baseKey) % DPSExtremeUI.chatColor.Length];
					entry.myNameText = name;
					entry.myBaseKey = baseKey;
					entry.SetValues(listTotal, myHighestValue, myTotal);
					entryIndex++;
				}
			}

			//In case no new entries were added but they need to be re-sorted
			UpdateOrder();
		}
	}
}
