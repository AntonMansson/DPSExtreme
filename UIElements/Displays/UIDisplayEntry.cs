using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;

namespace DPSExtreme.UIElements.Displays
{
	internal class UIDisplayEntry : UIElement
	{
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
		internal Color myColor;

		public UIDisplayEntry()
		{
			Width.Percent = 1f;
			Height.Pixels = 25f;

			Recalculate();
		}

		protected virtual int GetEntryWidth()
		{
			return (int)GetOuterDimensions().Width;
		}

		protected void DrawSelfBase(SpriteBatch spriteBatch)
		{
			Rectangle hitbox = GetOuterDimensions().ToRectangle();
			hitbox.Width = GetEntryWidth();

			Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, hitbox, myColor/** 0.6f*/);
			hitbox = GetInnerDimensions().ToRectangle();

			Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.MouseText.Value, myNameText, hitbox.TopLeft() + new Vector2(4, 3), Color.White, 0f,
				new Vector2(0, 0), new Vector2(.9f), -1f, 1.5f);
		}
	}
}
