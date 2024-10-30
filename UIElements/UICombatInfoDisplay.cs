using Terraria.ModLoader.UI.Elements;

namespace DPSExtreme.UIElements
{
	internal enum ListDisplayMode
	{
		DamageDone,
		DamagePerSecond,
		EnemyDamageTaken,
		Count
	}

	internal abstract class UICombatInfoDisplay : UIGrid
	{
		internal int myHighestValue = -1;
		internal int myTotal = 0;

		protected ListDisplayMode myDisplayMode;
		internal string myLabelOverride = null;

		protected UICombatInfoDisplay(ListDisplayMode aDisplayMode)
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
		}

		internal abstract void Update();
		internal abstract void RecalculateTotals();
		internal abstract void UpdateValues();
	}
}
