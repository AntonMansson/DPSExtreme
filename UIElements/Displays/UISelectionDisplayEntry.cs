
using Microsoft.Xna.Framework.Graphics;
using Terraria.UI;

namespace DPSExtreme.UIElements.Displays
{
	internal class UISelectionDisplayEntry : UIDisplayEntry
	{
		internal int myIndex = -1;

		internal UISelectionDisplayEntry()
		{
			OnRightClick += OnRightClickDisplay;
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			DrawSelfBase(spriteBatch);
		}

		protected void OnRightClickDisplay(UIMouseEvent evt, UIElement listeningElement)
		{
			DPSExtremeUI.instance.myDisplayMode = DPSExtremeUI.instance.myPreviousDisplayMode;
		}
	}
}
