using Terraria.UI;

namespace DPSExtreme.UIElements.Displays
{
	

	internal abstract class UICombatInfoDisplay : UIDisplay
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

		internal UICombatInfoDisplay(ListDisplayMode aDisplayMode)
			: base(aDisplayMode)
		{
			myClickEntryCallback += OnClickBaseEntry;
			myEntryCreator = () => { return new UIStatDisplayEntry(); };
		}

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

			UIStatDisplayEntry entry = listeningElement as UIStatDisplayEntry;

			if (entry.myBaseKey != -1)
				myBreakdownAccessor = entry.myBaseKey;
			else if (entry.myParticipantIndex != -1)
				myBreakdownAccessor = entry.myParticipantIndex;

			Clear();
			Add(myBreakdownDisplay);

			if (myNameCallback != null)
				myLabelOverride = myNameCallback(myBreakdownAccessor);

			DPSExtremeUI.instance.RefreshLabel();
			DPSExtremeUI.instance.updateNeeded = true;
		}

		protected void OnRightClickBreakdownDisplay(UIMouseEvent evt, UIElement listeningElement)
		{
			myBreakdownDisplay.Clear();
			Remove(myBreakdownDisplay);
			
			myLabelOverride = null;
			myBreakdownAccessor = -1;

			DPSExtremeUI.instance.RefreshLabel();
			DPSExtremeUI.instance.updateNeeded = true;
		}
	}
}
