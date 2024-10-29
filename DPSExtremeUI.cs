using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using System;
using Terraria.UI;
using Terraria.ModLoader.UI.Elements;
using Terraria.GameContent.UI.Elements;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using DPSExtreme.UIElements;
using ReLogic.Content;
using Terraria.Localization;
using Terraria.ID;
using DPSExtreme.CombatTracking;

namespace DPSExtreme
{
	internal class DPSExtremeUI : UIModState
	{
		internal static DPSExtremeUI instance;

		internal DPSExtremeCombat myDisplayedCombat = null;

		internal UIDragablePanel myRootPanel;
		internal UIText myLabel;
		internal UIGrid myDPSDisplay;
		internal UIGrid myDamageDealtDisplay;

		internal bool myShowPercent = true;
		internal bool myShowDPSPanel = false;
		internal int myHoveredParticipant = -1;

		private bool showTeamDPSPanel;
		public bool ShowTeamDPSPanel
		{
			get { return showTeamDPSPanel; }
			set
			{
				if (value)
				{
					Append(myRootPanel);
				}
				else
				{
					RemoveChild(myRootPanel);
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
			myRootPanel = new UIDragablePanel();
			myRootPanel.SetPadding(6);
			myRootPanel.Left.Set(-310f, 0f);
			myRootPanel.HAlign = 1;
			myRootPanel.Top.Set(90f, 0f);
			myRootPanel.Width.Set(415f, 0f);
			myRootPanel.MinWidth.Set(50f, 0f);
			myRootPanel.MaxWidth.Set(500f, 0f);
			myRootPanel.Height.Set(350, 0f);
			myRootPanel.MinHeight.Set(50, 0f);
			myRootPanel.MaxHeight.Set(300, 0f);
			myRootPanel.BackgroundColor = new Color(73, 94, 171);

			myLabel = new UIText("", 0.8f);
			//Figure out why tf this doesn't work
			myLabel.DynamicallyScaleDownToWidth = true;
			myLabel.MaxWidth.Set(50, 0);

			myLabel.OnLeftClick += Label_OnClick;
			myRootPanel.Append(myLabel);
			myRootPanel.AddDragTarget(myLabel);

			RefreshLabel();

			//var togglePercentButton = new UIHoverImageButton(Main.itemTexture[ItemID.SuspiciousLookingEye], "Toggle %");
			var togglePercentButton = new UIHoverImageButton(DPSExtreme.instance.Assets.Request<Texture2D>("PercentButton", AssetRequestMode.ImmediateLoad), Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("TogglePercent")));
			togglePercentButton.OnLeftClick += (a, b) => myShowPercent = !myShowPercent;
			togglePercentButton.Left.Set(-24, 1f);
			togglePercentButton.Top.Pixels = -4;
			//toggleCompletedButton.Top.Pixels = spacing;
			myRootPanel.Append(togglePercentButton);

			var labelDimensions = myLabel.GetInnerDimensions();
			int top = (int)labelDimensions.Height + 4;

			myDPSDisplay = new UIGrid();
			myDPSDisplay.Width.Set(0, 1f);
			myDPSDisplay.Height.Set(-top, 1f);
			myDPSDisplay.Top.Set(top, 0f);
			myDPSDisplay.ListPadding = 0f;

			if (myShowDPSPanel)
				myRootPanel.Append(myDPSDisplay);

			myRootPanel.AddDragTarget(myDPSDisplay);

			var type = Assembly.GetAssembly(typeof(Mod)).GetType("Terraria.ModLoader.UI.Elements.UIGrid");
			FieldInfo loadModsField = type.GetField("_innerList", BindingFlags.Instance | BindingFlags.NonPublic);
			myRootPanel.AddDragTarget((UIElement)loadModsField.GetValue(myDPSDisplay)); // list._innerList

			myDamageDealtDisplay = new UIGrid();
			myDamageDealtDisplay.Width.Set(0, 1f);
			myDamageDealtDisplay.Height.Set(-top, 1f);
			myDamageDealtDisplay.Top.Set(top, 0f);
			myDamageDealtDisplay.ListPadding = 0f;

			if (!myShowDPSPanel)
				myRootPanel.Append(myDamageDealtDisplay);

			myRootPanel.AddDragTarget(myDamageDealtDisplay);
			myRootPanel.AddDragTarget((UIElement)loadModsField.GetValue(myDamageDealtDisplay));

			var scrollbar = new InvisibleFixedUIScrollbar(userInterface);
			scrollbar.SetView(100f, 1000f);
			scrollbar.Height.Set(0, 1f);
			scrollbar.Left.Set(-20, 1f);
			//myRootPanel.Append(scrollbar);
			myDPSDisplay.SetScrollbar(scrollbar);

			scrollbar = new InvisibleFixedUIScrollbar(userInterface);
			scrollbar.SetView(100f, 1000f);
			scrollbar.Height.Set(0, 1f);
			scrollbar.Left.Set(-20, 1f);
			//myRootPanel.Append(scrollbar);
			myDamageDealtDisplay.SetScrollbar(scrollbar);

			//updateNeeded = true;
		}

		internal bool updateNeeded;

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
			//myHoveredParticipant = -1;
			if (!updateNeeded) { return; }
			updateNeeded = false;
			UpdateDamageLists();
		}

		internal void UpdateDamageLists()
		{
			//ShowFavoritePanel = favoritedRecipes.Count > 0;
			//	myRootPanel.RemoveAllChildren();

			//UIText label = new UIText("DPS");
			//label.OnClick += Label_OnClick;
			//myRootPanel.Append(label);

			//label.Recalculate();
			var labelDimensions = myLabel.GetInnerDimensions();
			int top = (int)labelDimensions.Height + 4;
			if (myShowDPSPanel)
			{
				myDPSDisplay.Clear();
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
							UIPlayerDPS t = new UIPlayerDPS(i);
							t.SetDPS(playerDPS, max, total);
							t.Recalculate();
							var inner = t.GetInnerDimensions();
							t.Width.Set(250, 0);
							height += (int)(inner.Height + myDPSDisplay.ListPadding);
							width = Math.Max(width, (int)inner.Width);
							myDPSDisplay.Add(t);
							myRootPanel.AddDragTarget(t);
						}
					}

					if (!Main.LocalPlayer.accDreamCatcher)
					{
						UIText t = new UIText(Language.GetText(DPSExtreme.instance.GetLocalizationKey("NoDPSWearDPSMeter")));
						myDPSDisplay.Add(t);
						myRootPanel.AddDragTarget(t);
					}
				}


				myDPSDisplay.Recalculate();
				var fff = myDPSDisplay.GetTotalHeight();

				width = 250;
				myRootPanel.Height.Pixels = top + /*height*/ fff + myRootPanel.PaddingBottom + myRootPanel.PaddingTop - myDPSDisplay.ListPadding;
				myRootPanel.Width.Pixels = width + myRootPanel.PaddingLeft + myRootPanel.PaddingRight;
				myRootPanel.Recalculate();
			}
			else
			{
				myDamageDealtDisplay.Clear();

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
							UIPlayerDPS t = new UIPlayerDPS(i);
							t.SetDPS(damageDealt, max, total);
							t.Recalculate();
							var inner = t.GetInnerDimensions();
							t.Width.Set(250, 0);
							height += (int)(inner.Height + myDamageDealtDisplay.ListPadding);
							myDamageDealtDisplay.Add(t);
							myRootPanel.AddDragTarget(t);
						}
					}
				}

				myDamageDealtDisplay.Recalculate();
				var fff = myDamageDealtDisplay.GetTotalHeight();
				myRootPanel.Height.Pixels = top + /*height*/ fff + myRootPanel.PaddingBottom + myRootPanel.PaddingTop - myDPSDisplay.ListPadding;
				myRootPanel.Width.Pixels = 250 + myRootPanel.PaddingLeft + myRootPanel.PaddingRight;
				myRootPanel.Recalculate();
			}
		}

		internal void RefreshLabel()
		{
			string title = null;

			if (myShowDPSPanel)
				title = Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("DPS"));
			else
				title = Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("DamageDone"));

			if (myDisplayedCombat == null)
			{
				myLabel.SetText(title);
				myLabel.Recalculate();
				return;
			}

			title += " - ";

			switch (myDisplayedCombat.myHighestCombatType)
			{
				case DPSExtremeCombat.CombatType.BossFight:
					if (myDisplayedCombat.myBossOrInvasionOrEventType > -1)
					{
						string bossName = Lang.GetNPCNameValue(myDisplayedCombat.myBossOrInvasionOrEventType);
						title += Language.GetText(bossName);
					}
					else
					{
						title += Language.GetText(DPSExtreme.instance.GetLocalizationKey("NoBoss")).Value;
					}

					break;
				case DPSExtremeCombat.CombatType.Invasion:
					DPSExtremeCombat.InvasionType invasionType;
					if (myDisplayedCombat.myBossOrInvasionOrEventType >= (int)DPSExtremeCombat.InvasionType.ModdedInvasionsStart &&
						myDisplayedCombat.myBossOrInvasionOrEventType < (int)DPSExtremeCombat.InvasionType.ModdedInvasionsEnd)
					{
						invasionType = DPSExtremeCombat.InvasionType.ModdedInvasionsStart;
					}
					else
					{
						invasionType = (DPSExtremeCombat.InvasionType)myDisplayedCombat.myBossOrInvasionOrEventType;
					}

					switch (invasionType)
					{
						case DPSExtremeCombat.InvasionType.GoblinArmy:
							title += Language.GetTextValue("Goblins");
							break;
						case DPSExtremeCombat.InvasionType.SnowLegion:
							title += Language.GetTextValue("FrostLegion");
							break;
						case DPSExtremeCombat.InvasionType.PirateInvasion:
							title += Language.GetTextValue("Pirates");
							break;
						case DPSExtremeCombat.InvasionType.MartianMadness:
							title += Language.GetTextValue("Martian");
							break;
						case DPSExtremeCombat.InvasionType.PumpkinMoon:
							title += Language.GetTextValue("PumpkinMoon");
							break;
						case DPSExtremeCombat.InvasionType.FrostMoon:
							title += Language.GetTextValue("FrostMoon");
							break;
						case DPSExtremeCombat.InvasionType.OldOnesArmy:
							title += Language.GetTextValue("OldOnesArmy");
							break;
						case DPSExtremeCombat.InvasionType.ModdedInvasionsStart:
							//TODO: Boss checklist support to fetch name?
							title += Language.GetTextValue("Invasion");
							break;
						default:
							title += Language.GetTextValue("Invasion");
							break;
					}
					break;
				case DPSExtremeCombat.CombatType.Event:
					switch ((DPSExtremeCombat.EventType)myDisplayedCombat.myBossOrInvasionOrEventType)
					{
						case DPSExtremeCombat.EventType.BloodMoon:
							title += Language.GetTextValue("BloodMoon");
							break;
						case DPSExtremeCombat.EventType.Eclipse:
							title += Language.GetTextValue("Eclipse");
							break;
						case DPSExtremeCombat.EventType.SlimeRain:
							title += Language.GetTextValue("SlimeRain");
							break;
						default:
							title += Language.GetTextValue("Event");
							break;
					}
					break;
				case DPSExtremeCombat.CombatType.Generic:
					//Maybe display name of first npc hit?
					title += Language.GetTextValue("Combat");
					break;
				default:
					title += "Unknown combat type";
					break;
			}

			myLabel.SetText(title);
			myLabel.Recalculate();
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
			//base.DrawSelf(spriteBatch);

			bool IsPlayer = myHoveredParticipant >= 0 && myHoveredParticipant < (int)InfoListIndices.SupportedPlayerCount;
			bool isNPC = myHoveredParticipant == (int)InfoListIndices.NPCs;
			if (IsPlayer || isNPC)
			{
				Rectangle hitbox = DPSExtremeUI.instance.myRootPanel.GetOuterDimensions().ToRectangle();
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
					Main.PlayerRenderer.DrawPlayer(Main.Camera, Main.player[myHoveredParticipant ], Main.screenPosition + r2.Center.ToVector2() + new Vector2(-10, -21), 0, Vector2.Zero);
				}
			}

			myHoveredParticipant = -1;

			if (myLabel.IsMouseHovering)
			{
				if (myShowDPSPanel)
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
			myShowDPSPanel = !myShowDPSPanel;
			if (myShowDPSPanel)
			{
				RefreshLabel();
				myRootPanel.RemoveChild(myDamageDealtDisplay);
				myRootPanel.Append(myDPSDisplay);
			}
			else
			{
				RefreshLabel();
				myRootPanel.RemoveChild(myDPSDisplay);
				myRootPanel.Append(myDamageDealtDisplay);
			}
			updateNeeded = true;
		}
	}
}

