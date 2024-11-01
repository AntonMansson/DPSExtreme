using System;
using System.Reflection;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;

namespace DPSExtreme.UIElements
{
	internal enum ListDisplayMode
	{
		NeedAccessory,
		DamageDone,
		DamagePerSecond,
		EnemyDamageTaken,
		Count
	}

	internal abstract class UICombatInfoDisplay : UIGrid
	{
		internal int myHighestValue = -1;
		internal int myTotal = 0;

		protected UICombatInfoDisplay myBreakdownDisplay = null;
		internal int myBreakdownAccessor = -1;
		protected bool myIsInBreakdown
		{
			get { return myBreakdownAccessor != -1; }
		}

		protected UICombatInfoDisplay myParentDisplay = null;

		protected ListDisplayMode myDisplayMode;
		internal string myLabelOverride = null;
		protected Func<int, string> myNameCallback;

		internal UICombatInfoDisplay(ListDisplayMode aDisplayMode, TypeInfo aStatInfo)
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

		internal abstract void Update();
		internal abstract void RecalculateTotals();
		internal abstract void UpdateValues();

		internal UICombatInfoDisplay AddBreakdown(UICombatInfoDisplay aDisplay)
		{
			aDisplay.myParentDisplay = this;
			myBreakdownDisplay = aDisplay;
			myBreakdownDisplay.OnRightClick += OnRightClickBreakdownDisplay;

			return aDisplay;
		}

		protected void OnClickBaseEntry(UIMouseEvent evt, UIElement listeningElement)
		{
			if (myBreakdownDisplay == null)
				return;

			Clear();
			Add(myBreakdownDisplay);

			UIListDisplayEntry entry = listeningElement as UIListDisplayEntry;
			if (entry.myBaseKey != -1)
				myBreakdownAccessor = entry.myBaseKey;

			if (entry.myParticipantIndex != -1)
				myBreakdownAccessor = entry.myParticipantIndex;

			if (myNameCallback != null)
				myLabelOverride = myNameCallback(myBreakdownAccessor);

			DPSExtremeUI.instance.RefreshLabel();
			DPSExtremeUI.instance.updateNeeded = true;
		}

		protected void OnRightClickBreakdownDisplay(UIMouseEvent evt, UIElement listeningElement)
		{
			Remove(myBreakdownDisplay);
			
			myLabelOverride = null;
			myBreakdownAccessor = -1;

			DPSExtremeUI.instance.RefreshLabel();
			DPSExtremeUI.instance.updateNeeded = true;
		}

		protected void OnScroll(UIMouseEvent evt, UIElement listeningElement)
		{
			DPSExtremeUI.instance.updateNeeded = true;
		}
	}
}
