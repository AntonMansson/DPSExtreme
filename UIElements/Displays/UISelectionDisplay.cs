using System;
using Terraria.UI;

namespace DPSExtreme.UIElements.Displays
{
	internal class UISelectionDisplay : UIDisplay
	{
		Action<int> mySelectCallback;

		internal UISelectionDisplay(ListDisplayMode aDisplayMode, Func<int, string> aNameCallback, Action<int> aSelectCallback)
			: base(aDisplayMode) 
		{
			myNameCallback = aNameCallback;
			mySelectCallback = aSelectCallback;

			myClickEntryCallback += OnClickEntry;
			myEntryCreator = () => { return new UISelectionDisplayEntry(); };
		}

		internal override void Update()
		{
			int entryIndex = 0;
			//How to make this loop generic?
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

		protected void OnClickEntry(UIMouseEvent evt, UIElement listeningElement)
		{
			UISelectionDisplayEntry entry = listeningElement as UISelectionDisplayEntry;
			if (entry == null)
				return;

			mySelectCallback(entry.myIndex);
			Clear();
		}
	}
}
