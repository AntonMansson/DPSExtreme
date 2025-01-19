using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.UI;

namespace DPSExtreme
{
	internal class UIPlayerDPS : UIElement
	{
		internal float max = 100000f;
		internal int dps = 1; // dps or cumulative damage.
		internal int total = 1;
		internal string hoverText;
		internal int player = 0;
		internal string nameText;

		public UIPlayerDPS(int aPlayer) {
			player = aPlayer;

			nameText = Main.player[player].name;
			if (player >= (int)InfoListIndices.DisconnectedPlayersStart && player <= (int)InfoListIndices.DisconnectedPlayersEnd) {
				nameText = Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("DisconnectedPlayer"));
			}
			else if (player == (int)InfoListIndices.NPCs || player == (int)InfoListIndices.Traps) {
				nameText = Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("TrapsTownNPC"));
			}
			else if (player == (int)InfoListIndices.DOTs) {
				nameText = Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("DamageOverTime"));
			}

			PaddingTop = 8f;

			DynamicSpriteFont dynamicSpriteFont = FontAssets.MouseText.Value;
			Vector2 textSize = new Vector2(dynamicSpriteFont.MeasureString(nameText.ToString()).X, 16f) * 1f;
			MinWidth.Set(textSize.X + PaddingLeft + PaddingRight, 0f);
			MinHeight.Set(textSize.Y + PaddingTop + PaddingBottom, 0f);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch) {
			Rectangle hitbox = GetOuterDimensions().ToRectangle();
			hitbox.Width = (int)(hitbox.Width * (dps / max));
			Color color = DPSExtremeUI.chatColor[(player + DPSExtremeUI.chatColor.Length) % DPSExtremeUI.chatColor.Length];
			Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, hitbox, color /** 0.6f*/);
			hitbox = GetInnerDimensions().ToRectangle();

			//if (Main.rand.NextBool(2))
			//{
			//	Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(spriteBatch, Main.fontMouseText, text, 
			//	hitbox.TopLeft(), Color.White,  
			//	0f, Vector2.Zero, Vector2.One, -1f, 2f);
			//}
			//else
			//{
			//Utils.DrawBorderString(spriteBatch, text, hitbox.TopLeft(), Color.White, 1f, 0f, 0f, -1);
			//}
			//string[] RandomNames = new string[] { "Bob", "Terminator", "TacoBelle", "What Is My Name", "Albert", "jopojelly", "blushie", "jofariden", "someone", "Town/Traps" };

			//if (!Main.player[player].active)
			//	leftText = RandomNames[player % RandomNames.Length];
			color = Color.White;
			DynamicSpriteFont fontMouseText = FontAssets.MouseText.Value;
			Vector2 vector = fontMouseText.MeasureString(nameText);
			int yOffset = -6;
			Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(spriteBatch, fontMouseText, nameText, hitbox.TopLeft() + new Vector2(4, yOffset), color, 0f,
				new Vector2(0, 0) * vector, new Vector2(1f), -1f, 1.5f);

			string rightText = dps.ToString(); // + " dps";
			if (DPSExtremeUI.instance.showPercent && total > 0)
				rightText = $"{dps} ({String.Format("{0:P0}", (float)dps / total)})";
			vector = fontMouseText.MeasureString(rightText);
			Terraria.UI.Chat.ChatManager.DrawColorCodedStringWithShadow(spriteBatch, fontMouseText, rightText, hitbox.TopRight() + new Vector2(-2, yOffset), color, 0f,
				new Vector2(1f, 0) * vector, new Vector2(1f), -1f, 1.5f);

			if (IsMouseHovering && player >= 0) {
				// TODO: IsMouseHovering is false once a second because UpdateDamageLists replaces the UIElement, need to fix that
				DPSExtremeUI.instance.drawPlayer = player;
				Main.hoverItemName = "";
			}
		}

		internal void SetDPS(int dps, float max, int total) {
			this.dps = dps;
			this.max = max;
			this.total = total;
		}

		public override int CompareTo(object obj) {
			UIPlayerDPS other = obj as UIPlayerDPS;
			return -dps.CompareTo(other.dps);
		}
	}
}

