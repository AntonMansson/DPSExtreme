using Microsoft.Xna.Framework;
using Terraria;
using System;
using Terraria.GameContent;
using Terraria.UI;
using ReLogic.Graphics;
using Microsoft.Xna.Framework.Graphics;

namespace DPSExtreme.UIElements.Displays
{
	internal class UIListDisplayEntry : UIElement
	{
		internal float myMax = 100000f;
		internal int myTotal = 1;
		internal int myValue = 1;

		internal string _myNameText;
		internal string myNameText
		{
			get { return _myNameText; }
			set
			{ 
				_myNameText = value;

				DynamicSpriteFont dynamicSpriteFont = FontAssets.MouseText.Value;
				Vector2 textSize = new Vector2(dynamicSpriteFont.MeasureString(_myNameText.ToString()).X, 16f) * 1f;
				MinWidth.Set(textSize.X + PaddingLeft + PaddingRight, 0f);
				MinHeight.Set(textSize.Y + PaddingTop + PaddingBottom, 0f);
			}
		}

		internal int myParticipantIndex = -1;
		internal int myBaseKey = -1;
		internal Color myColor;

		public UIListDisplayEntry()
		{
			PaddingTop = 8f;
			Width.Percent = 1f;
			Height.Pixels = 25f;

			Recalculate();
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			Rectangle hitbox = GetOuterDimensions().ToRectangle();
			hitbox.Width = (int)(hitbox.Width * (myValue / myMax));
			
			Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, hitbox, myColor/** 0.6f*/);
			hitbox = GetInnerDimensions().ToRectangle();

			DynamicSpriteFont fontMouseText = FontAssets.MouseText.Value;
			Vector2 vector = fontMouseText.MeasureString(myNameText);
			int yOffset = -6;
			Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(spriteBatch, fontMouseText, myNameText, hitbox.TopLeft() + new Vector2(4, yOffset), Color.White, 0f,
				new Vector2(0, 0) * vector, new Vector2(.9f), -1f, 1.5f);

			string rightText = myValue.ToString();
			if (DPSExtremeUI.instance.myShowPercent && myTotal > 0)
				rightText = $"{myValue} ({String.Format("{0:P0}", (float)myValue / myTotal)})";
			vector = fontMouseText.MeasureString(rightText);
			Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(spriteBatch, fontMouseText, rightText, hitbox.TopRight() + new Vector2(-2, yOffset), Color.White, 0f,
				new Vector2(1f, 0) * vector, new Vector2(1f), -1f, 1.5f);

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

