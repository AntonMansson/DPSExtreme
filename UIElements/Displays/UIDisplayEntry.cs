using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;
using System.Collections.Generic;
using DPSExtreme.Combat.Stats;
using ReLogic.Content;
using System;

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

		internal UIDisplay myParentDisplay = null;

		internal string myRightText = string.Empty;
		internal StatFormat myFormat = StatFormat.RawNumber;

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

			myColor.A = ContainsPoint(Main.MouseScreen) ? (byte)(Math.Floor(255f * 0.75f)) : (byte)255; //Kinda inverse but it looked better this way

			Texture2D backdropTex = DPSExtreme.instance.Assets.Request<Texture2D>("DisplayEntry", AssetRequestMode.ImmediateLoad).Value;
			Main.spriteBatch.Draw(backdropTex, hitbox, myColor);
			
			//Parent.Children does NOT get sorted with UpdateOrder. So access the outer _items list which DOES get sorted
			List<UIElement> sortedList = (Parent.Parent as UIGrid)._items;

			int entryIndex = -1;
			int index = 0;
			for (int i = 0; i < sortedList.Count; i++)
            {
				if (sortedList[i] != this)
				{
					index++;
					continue;
				}

				entryIndex = index;
				break;
            }

			DynamicSpriteFont fontMouseText = FontAssets.MouseText.Value;
			float textScale = .9f;

			string leftText = (entryIndex + 1).ToString() + ". " + myNameText;
			Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(spriteBatch, fontMouseText, leftText, GetOuterDimensions().ToRectangle().TopLeft() + new Vector2(4, 3), Color.White, 0f,
				new Vector2(0, 0), new Vector2(textScale), -1f, 1.5f);

			Vector2 rightTextBounds = fontMouseText.MeasureString(myRightText);
			Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(spriteBatch, fontMouseText, myRightText, GetOuterDimensions().ToRectangle().TopRight() + new Vector2(-4, 2), Color.White, 0f,
				new Vector2(1f, 0) * rightTextBounds, new Vector2(textScale), -1f, 1.5f);
		}
	}
}
