using System;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace DPSExtreme.UIElements.Displays
{
	internal enum ListDisplayMode
	{
		NeedAccessory,
		DisplayModeSelect,
		CombatHistory,

		StatDisplaysStart,
		DamagePerSecond,
		DamageDone,
		DamageTaken,
		Deaths,
		Kills,
		EnemyDamageTaken,
		StatDisplaysEnd,

		StatDisplaysCount = StatDisplaysEnd - StatDisplaysStart
	}

	internal abstract class UIDisplay : UIGrid
	{
		protected ListDisplayMode myDisplayMode;

		internal Func<int, string> myNameCallback;
		internal Func<UIDisplayEntry> myEntryCreator;

		internal event MouseEvent myClickEntryCallback;
		internal string myLabelOverride = null;

		internal UIDisplay(ListDisplayMode aDisplayMode)
		{
			myDisplayMode = aDisplayMode;

			Width.Percent = 1f;
			Height.Set(-20, 1f);
			Top.Pixels = 20;
			ListPadding = 0f;

			InvisibleFixedUIScrollbar scrollbar = new InvisibleFixedUIScrollbar(DPSExtremeUI.instance.userInterface);
			scrollbar.SetView(100f, 1000f);
			scrollbar.Height.Set(0, 1f);
			scrollbar.Left.Set(-20, 1f);
			SetScrollbar(scrollbar);

			OnScrollWheel += OnScroll;
		}

		internal UIDisplayEntry CreateEntry(int aIndex)
		{
			if (aIndex >= _items.Count)
			{
				UIDisplayEntry entry = myEntryCreator();
				entry.OnLeftClick += myClickEntryCallback;
				Add(entry);
				return entry;
			}

			return _items[aIndex] as UIDisplayEntry;
		}

		internal abstract void Update();

		protected void OnScroll(UIMouseEvent evt, UIElement listeningElement)
		{
			DPSExtremeUI.instance.updateNeeded = true;
		}
	}
}
