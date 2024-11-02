using Microsoft.Xna.Framework;
using Terraria;
using System;
using Terraria.GameContent;
using ReLogic.Graphics;
using Microsoft.Xna.Framework.Graphics;

namespace DPSExtreme.UIElements.Displays
{
	internal class UIListDisplayEntry : UIDisplayEntry
	{
		internal float myMax = 100000f;
		internal int myTotal = -1;
		internal int myValue = -1;

		internal int myParticipantIndex = -1;
		internal int myBaseKey = -1;

		protected override int GetEntryWidth()
		{
			return (int)(GetOuterDimensions().Width * (myValue / myMax));
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			DrawSelfBase(spriteBatch);

			string rightText = myValue.ToString();
			if (DPSExtremeUI.instance.myShowPercent && myTotal > 0)
				rightText = $"{myValue} ({String.Format("{0:P0}", (float)myValue / myTotal)})";

			DynamicSpriteFont fontMouseText = FontAssets.MouseText.Value;
			
			Vector2 textBounds = fontMouseText.MeasureString(rightText);
			Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(spriteBatch, fontMouseText, rightText, GetOuterDimensions().ToRectangle().TopRight() + new Vector2(-2, 2), Color.White, 0f,
				new Vector2(1f, 0) * textBounds, new Vector2(1f), -1f, 1.5f);

			if (IsMouseHovering && myParticipantIndex >= 0)
			{
				DPSExtremeUI.instance.myHoveredParticipant = myParticipantIndex;
				Main.hoverItemName = "";
			}
		}

		internal void SetValues(int aValue, float aMax, int aTotal)
		{
			myValue = aValue;
			myMax = aMax;
			myTotal = aTotal;
		}

		public override int CompareTo(object obj)
		{
			UIListDisplayEntry other = obj as UIListDisplayEntry;
			return -myValue.CompareTo(other.myValue);
		}
	}
}

