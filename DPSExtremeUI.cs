﻿using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using System;
using Terraria.UI;
using Terraria.ModLoader.UI.Elements;
using Terraria.GameContent.UI.Elements;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Graphics;
using DPSExtreme.UIElements;
using ReLogic.Content;
using Terraria.Localization;
using Terraria.ID;
using DPSExtreme.CombatTracking;
using static System.Net.Mime.MediaTypeNames;

namespace DPSExtreme
{
	internal class DPSExtremeUI : UIModState
	{
		internal static DPSExtremeUI instance;

		DPSExtremeCombat myDisplayedCombat = null;

		internal UIDragablePanel teamDPSPanel;
		internal UIText label;
		internal UIGrid dpsList;
		internal UIGrid bossList;

		internal bool showPercent = true;
		internal bool showDPSPanel = true;
		internal int drawPlayer = -1;

		private bool showTeamDPSPanel;
		public bool ShowTeamDPSPanel
		{
			get { return showTeamDPSPanel; }
			set
			{
				if (value)
				{
					Append(teamDPSPanel);
				}
				else
				{
					RemoveChild(teamDPSPanel);
				}
				showTeamDPSPanel = value;
				if (value)
					updateNeeded = true;
			}
		}

		internal static Color[] chatColor = new Color[]{
			Color.LightBlue,
			Color.LightCoral,
			Color.LightCyan,
			Color.LightGoldenrodYellow,
			Color.LightGray,
			Color.LightPink,
			Color.LightSkyBlue,
			Color.LightYellow
		};

		public DPSExtremeUI(UserInterface ui) : base(ui)
		{
			instance = this;
		}

		Asset<Texture2D> playerBackGroundTexture;
		public override void OnInitialize()
		{
			playerBackGroundTexture = Main.Assets.Request<Texture2D>("Images/UI/PlayerBackground");

			//TODO: Save window position etc
			teamDPSPanel = new UIDragablePanel();
			teamDPSPanel.SetPadding(6);
			teamDPSPanel.Left.Set(-310f, 0f);
			teamDPSPanel.HAlign = 0.75f;
			teamDPSPanel.Top.Set(90f, 0f);
			teamDPSPanel.Width.Set(415f, 0f);
			teamDPSPanel.MinWidth.Set(50f, 0f);
			teamDPSPanel.MaxWidth.Set(500f, 0f);
			teamDPSPanel.Height.Set(350, 0f);
			teamDPSPanel.MinHeight.Set(50, 0f);
			teamDPSPanel.MaxHeight.Set(300, 0f);
			teamDPSPanel.BackgroundColor = new Color(73, 94, 171);

			label = new UIText(Language.GetText(DPSExtreme.instance.GetLocalizationKey("DPS")), 0.8f);
			label.OverflowHidden = true;
			label.DynamicallyScaleDownToWidth = true;
			label.MaxWidth.Set(50, 0);

			label.OnLeftClick += Label_OnClick;
			teamDPSPanel.Append(label);
			teamDPSPanel.AddDragTarget(label);

			//var togglePercentButton = new UIHoverImageButton(Main.itemTexture[ItemID.SuspiciousLookingEye], "Toggle %");
			var togglePercentButton = new UIHoverImageButton(DPSExtreme.instance.Assets.Request<Texture2D>("PercentButton", AssetRequestMode.ImmediateLoad), Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("TogglePercent")));
			togglePercentButton.OnLeftClick += (a, b) => showPercent = !showPercent;
			togglePercentButton.Left.Set(-24, 1f);
			togglePercentButton.Top.Pixels = -4;
			//toggleCompletedButton.Top.Pixels = spacing;
			teamDPSPanel.Append(togglePercentButton);

			var labelDimensions = label.GetInnerDimensions();
			int top = (int)labelDimensions.Height + 4;

			dpsList = new UIGrid();
			dpsList.Width.Set(0, 1f);
			dpsList.Height.Set(-top, 1f);
			dpsList.Top.Set(top, 0f);
			dpsList.ListPadding = 0f;
			teamDPSPanel.Append(dpsList);
			teamDPSPanel.AddDragTarget(dpsList);

			var type = Assembly.GetAssembly(typeof(Mod)).GetType("Terraria.ModLoader.UI.Elements.UIGrid");
			FieldInfo loadModsField = type.GetField("_innerList", BindingFlags.Instance | BindingFlags.NonPublic);
			teamDPSPanel.AddDragTarget((UIElement)loadModsField.GetValue(dpsList)); // list._innerList

			bossList = new UIGrid();
			bossList.Width.Set(0, 1f);
			bossList.Height.Set(-top, 1f);
			bossList.Top.Set(top, 0f);
			bossList.ListPadding = 0f;
			//teamDPSPanel.Append(bossList);
			teamDPSPanel.AddDragTarget(bossList);
			teamDPSPanel.AddDragTarget((UIElement)loadModsField.GetValue(bossList));

			var scrollbar = new InvisibleFixedUIScrollbar(userInterface);
			scrollbar.SetView(100f, 1000f);
			scrollbar.Height.Set(0, 1f);
			scrollbar.Left.Set(-20, 1f);
			//teamDPSPanel.Append(scrollbar);
			dpsList.SetScrollbar(scrollbar);

			scrollbar = new InvisibleFixedUIScrollbar(userInterface);
			scrollbar.SetView(100f, 1000f);
			scrollbar.Height.Set(0, 1f);
			scrollbar.Left.Set(-20, 1f);
			//teamDPSPanel.Append(scrollbar);
			bossList.SetScrollbar(scrollbar);

			//updateNeeded = true;
		}

		internal bool updateNeeded;

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			//drawPlayer = -1;
			if (!updateNeeded) { return; }
			updateNeeded = false;
			UpdateDamageLists();
		}

		internal void UpdateDamageLists()
		{
			//ShowFavoritePanel = favoritedRecipes.Count > 0;
			//	teamDPSPanel.RemoveAllChildren();

			//UIText label = new UIText("DPS");
			//label.OnClick += Label_OnClick;
			//teamDPSPanel.Append(label);

			//label.Recalculate();
			var labelDimensions = label.GetInnerDimensions();
			int top = (int)labelDimensions.Height + 4;
			if (showDPSPanel)
			{
				dpsList.Clear();
				int width = 1;
				int height = 0;
				float max = 1f;
				int total = 0;

				if (myDisplayedCombat != null)
				{
					for (int i = 0; i < myDisplayedCombat.myDPSList.Size(); i++)
					{
						int playerDPS = myDisplayedCombat.myDPSList[i].myDamage;
						if (playerDPS > 0)
						{
							max = Math.Max(max, playerDPS);
							total += playerDPS;
						}
					}
				
					for (int i = 0; i < myDisplayedCombat.myDPSList.Size(); i++)
					{
						int playerDPS = myDisplayedCombat.myDPSList[i].myDamage;
						if (playerDPS > 0)
						{
							UIPlayerDPS t = new UIPlayerDPS(i, "", "");
							t.SetDPS(playerDPS, max, total);
							t.Recalculate();
							var inner = t.GetInnerDimensions();
							t.Width.Set(250, 0);
							height += (int)(inner.Height + dpsList.ListPadding);
							width = Math.Max(width, (int)inner.Width);
							dpsList.Add(t);
							teamDPSPanel.AddDragTarget(t);
						}
					}
				
					if (!Main.LocalPlayer.accDreamCatcher)
					{
						UIText t = new UIText(Language.GetText(DPSExtreme.instance.GetLocalizationKey("NoDPSWearDPSMeter")));
						dpsList.Add(t);
						teamDPSPanel.AddDragTarget(t);
					}
				}


				dpsList.Recalculate();
				var fff = dpsList.GetTotalHeight();

				width = 250;
				teamDPSPanel.Height.Pixels = top + /*height*/ fff + teamDPSPanel.PaddingBottom + teamDPSPanel.PaddingTop - dpsList.ListPadding;
				teamDPSPanel.Width.Pixels = width + teamDPSPanel.PaddingLeft + teamDPSPanel.PaddingRight;
				teamDPSPanel.Recalculate();
			}
			else
			{
				bossList.Clear();

				int height = 0;
				int max = 1;
				int total = 0;
				if (myDisplayedCombat != null)
				{
					for (int i = 0; i < myDisplayedCombat.myTotalDamageDealtList.Size(); i++)
					{
						int damageDealt = myDisplayedCombat.myTotalDamageDealtList[i].myDamage;
						if (damageDealt > -1)
						{
							max = Math.Max(max, damageDealt);
							total += damageDealt;
						}
					}

					for (int i = 0; i < myDisplayedCombat.myTotalDamageDealtList.Size(); i++)
					{
						int damageDealt = myDisplayedCombat.myTotalDamageDealtList[i].myDamage;
						if (damageDealt > -1)
						{
							UIPlayerDPS t = new UIPlayerDPS(i, "", "");
							t.SetDPS(damageDealt, max, total);
							t.Recalculate();
							var inner = t.GetInnerDimensions();
							t.Width.Set(250, 0);
							height += (int)(inner.Height + bossList.ListPadding);
							bossList.Add(t);
							teamDPSPanel.AddDragTarget(t);
						}
					}
				}

				bossList.Recalculate();
				var fff = bossList.GetTotalHeight();
				teamDPSPanel.Height.Pixels = top + /*height*/ fff + teamDPSPanel.PaddingBottom + teamDPSPanel.PaddingTop - dpsList.ListPadding;
				teamDPSPanel.Width.Pixels = 250 + teamDPSPanel.PaddingLeft + teamDPSPanel.PaddingRight;
				teamDPSPanel.Recalculate();
			}
		}

		internal void RefreshLabel()
		{
			string title = null;

			if (showDPSPanel)
				title = Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("DPS"));
			else
				title = Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("DamageDone"));

			if (myDisplayedCombat == null)
			{
				label.SetText(title);
				label.Recalculate();
				return;
			}

			title += " - ";

			switch (myDisplayedCombat.myHighestCombatType)
			{
				case DPSExtremeCombat.CombatType.BossFight:
					if (myDisplayedCombat.myBossOrInvasionType > -1)
					{
						string bossName = Lang.GetNPCNameValue(myDisplayedCombat.myBossOrInvasionType);
						title += Language.GetText(bossName);
					}
					else
					{
						title += Language.GetText(DPSExtreme.instance.GetLocalizationKey("NoBoss")).Value;
					}

					break;
				case DPSExtremeCombat.CombatType.Invasion:
					switch (myDisplayedCombat.myBossOrInvasionType)
					{
						case InvasionID.GoblinArmy: title += Language.GetTextValue("Goblins"); break;
						case InvasionID.SnowLegion: title += Language.GetTextValue("FrostLegion"); break;
						case InvasionID.PirateInvasion: title += Language.GetTextValue("Pirates"); break;
						case InvasionID.MartianMadness: title += Language.GetTextValue("Martian"); break;
						default: title += "Invasion"; break; //TODO add to loc
					}
					break;
				case DPSExtremeCombat.CombatType.Generic:
					title += "Fight";
					break;
				default:
					title += "Unknown combat type";
					break;
			}

			label.SetText(title);
			label.Recalculate();
		}

		internal void OnCombatStarted(DPSExtremeCombat aCombat)
		{
			myDisplayedCombat = aCombat; //TODO: Think about what should happen if you are currently viewing history. Setting to decide if we swap instantly or not?
			RefreshLabel();
		}

		internal void OnCombatUpgraded(DPSExtremeCombat aCombat)
		{
			RefreshLabel();
		}

        internal void OnCombatEnded()
        {
            //Should we change combat view here?
            //RefreshLabel();
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			//Figure out why tf label's width can exceed its maxWidth

			//Vector2 topLeft = new Vector2(teamDPSPanel.GetInnerDimensions().X, teamDPSPanel.GetInnerDimensions().Y);
			//Vector2 bottomRight = new Vector2(teamDPSPanel.GetInnerDimensions().Width, teamDPSPanel.GetInnerDimensions().Height) + topLeft;
			//topLeft = Vector2.Transform(topLeft, Main.UIScaleMatrix);
			//Rectangle rectangle = new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)(bottomRight.X - topLeft.X), (int)(bottomRight.Y - topLeft.Y));
			////int num = (int)((float)Main.screenWidth * 1);
			//int num = (int)((float)Main.screenWidth * Main.UIScale);
			////int num2 = (int)((float)Main.screenHeight * 1);
			//int asd = Terraria.GameInput.PlayerInput.RealScreenWidth;
			//int num2 = (int)((float)Main.screenHeight * Main.UIScale);
			//rectangle.X = Utils.Clamp(rectangle.X, 0, num);
			//rectangle.Y = Utils.Clamp(rectangle.Y, 0, num2);
			//rectangle.Width = Utils.Clamp(rectangle.Width, 0, num - rectangle.X);
			//rectangle.Height = Utils.Clamp(rectangle.Height, 0, num2 - rectangle.Y);
			
			//Rectangle scissorRectangle = spriteBatch.GraphicsDevice.ScissorRectangle;

			//int num3 = Utils.Clamp(rectangle.Left, (int)(scissorRectangle.Left), (int)(scissorRectangle .Right));
			//int num4 = Utils.Clamp(rectangle.Top, (int)(scissorRectangle .Top), (int)(scissorRectangle .Bottom));
			//int num5 = Utils.Clamp(rectangle.Right, (int)(scissorRectangle .Left), (int)(scissorRectangle .Right));
			//int num6 = Utils.Clamp(rectangle.Bottom, (int)(scissorRectangle .Top), (int)(scissorRectangle .Bottom));
			////Rectangle clipRectBase = teamDPSPanel.GetClippingRectangle(spriteBatch);
			//Rectangle clipRectBase = new Rectangle(num3, num4, num5 - num3, num6 - num4);

			//Rectangle clipRect = label.GetInnerDimensions().ToRectangle();
			//spriteBatch.Draw(TextureAssets.MagicPixel.Value, clipRectBase, Color.Blue);
			//spriteBatch.Draw(TextureAssets.MagicPixel.Value, clipRect, Color.Red);

			//base.DrawSelf(spriteBatch);

			bool IsPlayer = drawPlayer >= 0 && drawPlayer < (int)InfoListIndices.SupportedPlayerCount;
			bool isNPC = drawPlayer == (int)InfoListIndices.NPCs;
			if (IsPlayer || isNPC)
			{
				Rectangle hitbox = DPSExtremeUI.instance.teamDPSPanel.GetOuterDimensions().ToRectangle();
				Rectangle r2 = new Rectangle(hitbox.X + hitbox.Width / 2 - 58 / 2, hitbox.Y - 58, 58, 58);
				spriteBatch.Draw(playerBackGroundTexture.Value, r2.TopLeft(), Color.White);

				if (isNPC)
				{
					NPC drawNPC = null;
					foreach (NPC npc in Main.ActiveNPCs)
					{
						if (!npc.townNPC)
							continue;

						drawNPC = npc;
						break;
					}

					if (drawNPC != null)
					{
						drawNPC.IsABestiaryIconDummy = true;
						var position = drawNPC.position;
						drawNPC.position = r2.Center.ToVector2() + new Vector2(-10, -21);
						Main.instance.DrawNPCDirect(spriteBatch, drawNPC, drawNPC.behindTiles, Vector2.Zero);
						drawNPC.position = position;
						drawNPC.IsABestiaryIconDummy = false;
					}
				}
				else
				{
					Main.PlayerRenderer.DrawPlayer(Main.Camera, Main.player[drawPlayer], Main.screenPosition + r2.Center.ToVector2() + new Vector2(-10, -21), 0, Vector2.Zero);
				}
			}

			drawPlayer = -1;

			if (label.IsMouseHovering)
			{
				if (showDPSPanel)
					Main.hoverItemName = Language.GetText(DPSExtreme.instance.GetLocalizationKey("ClickToViewBossDamage")).Value;
				else
					Main.hoverItemName = Language.GetText(DPSExtreme.instance.GetLocalizationKey("ClickToViewDPSStats")).Value;

				Item fakeItem = new Item();
				fakeItem.SetDefaults(0, noMatCheck: true);
				string textValue = Main.hoverItemName;
				fakeItem.SetNameOverride(textValue);
				fakeItem.type = ItemID.IronPickaxe;
				fakeItem.scale = 0f;
				fakeItem.rare = ItemRarityID.Yellow;
				fakeItem.value = -1;
				Main.HoverItem = fakeItem;
				Main.instance.MouseText("", 0, 0);
				Main.mouseText = true;
			}
		}

		private void Label_OnClick(UIMouseEvent evt, UIElement listeningElement)
		{
			UIText text = (evt.Target as UIText);
			showDPSPanel = !showDPSPanel;
			if (showDPSPanel)
			{
				RefreshLabel();
				teamDPSPanel.RemoveChild(bossList);
				teamDPSPanel.Append(dpsList);
			}
			else
			{
				RefreshLabel();
				teamDPSPanel.RemoveChild(dpsList);
				teamDPSPanel.Append(bossList);
			}
			updateNeeded = true;
		}
    }
}

