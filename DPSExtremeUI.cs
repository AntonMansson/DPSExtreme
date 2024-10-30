using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using System;
using Terraria.UI;
using Terraria.GameContent.UI.Elements;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using DPSExtreme.UIElements;
using ReLogic.Content;
using Terraria.Localization;
using Terraria.ID;
using DPSExtreme.CombatTracking;
using Terraria.ModLoader.UI;

namespace DPSExtreme
{
	internal class DPSExtremeUI : UIModState
	{
		internal static DPSExtremeUI instance;

		private DPSExtremeCombat _myDisplayedCombat;
		internal DPSExtremeCombat myDisplayedCombat
		{
			get { return _myDisplayedCombat; }
			set
			{
				_myDisplayedCombat = value;

				myDamagePerSecondDisplay.SetInfo(_myDisplayedCombat.myDPSList);
				myDamageDoneDisplay.SetInfo(_myDisplayedCombat.myDamageDoneList);
				myEnemyDamageTakenDisplay.SetInfo(_myDisplayedCombat.myEnemyDamageTaken);
			}
		}

		internal UIDragablePanel myRootPanel;
		internal UIText myLabel;

		internal UIListDisplay myDamagePerSecondDisplay = new UIListDisplay();
		internal UIListDisplay myDamageDoneDisplay = new UIListDisplay();
		internal UIBreakdownableDisplay myEnemyDamageTakenDisplay = new UIBreakdownableDisplay();

		private ListDisplayMode _myDisplayMode;
		internal ListDisplayMode myDisplayMode
		{
			get { return _myDisplayMode; }
			set
			{
				myRootPanel.RemoveChild(myCurrentDisplay);

				_myDisplayMode = value;

				myRootPanel.Append(myCurrentDisplay);
				RefreshLabel();
				updateNeeded = true;
			}
		}

		UICombatInfoDisplay myCurrentDisplay
		{
			get 
			{
				switch (myDisplayMode)
				{
					case ListDisplayMode.DamageDone:
						return myDamageDoneDisplay;
					case ListDisplayMode.DamagePerSecond:
						return myDamagePerSecondDisplay;
					case ListDisplayMode.EnemyDamageTaken:
						return myEnemyDamageTakenDisplay;
					case ListDisplayMode.Count:
					default:
						return null;
				}
			}
			set { myCurrentDisplay = value; }
		}

		internal bool myShowPercent = true;
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
			myRootPanel.Width.Set(250f, 0f);
			myRootPanel.MinWidth.Set(50f, 0f);
			myRootPanel.MaxWidth.Set(500f, 0f);
			myRootPanel.Height.Set(175f, 0f);
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

			myEnemyDamageTakenDisplay.myNameCallback = (int aNpcType) => { return Lang.GetNPCNameValue(aNpcType); };

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

			//TODO Do in constructor
			myRootPanel.AddDragTarget(myDamagePerSecondDisplay);
			myRootPanel.AddDragTarget(myDamageDoneDisplay);
			myRootPanel.AddDragTarget(myEnemyDamageTakenDisplay);

			var type = Assembly.GetAssembly(typeof(Mod)).GetType("Terraria.ModLoader.UI.Elements.UIGrid");
			FieldInfo loadModsField = type.GetField("_innerList", BindingFlags.Instance | BindingFlags.NonPublic);
			myRootPanel.AddDragTarget((UIElement)loadModsField.GetValue(myDamagePerSecondDisplay)); // list._innerList
			myRootPanel.AddDragTarget((UIElement)loadModsField.GetValue(myDamageDoneDisplay));
			myRootPanel.AddDragTarget((UIElement)loadModsField.GetValue(myEnemyDamageTakenDisplay));

			var scrollbar = new InvisibleFixedUIScrollbar(userInterface);
			scrollbar.SetView(100f, 1000f);
			scrollbar.Height.Set(0, 1f);
			scrollbar.Left.Set(-20, 1f);
			//myRootPanel.Append(scrollbar);
			myDamagePerSecondDisplay.SetScrollbar(scrollbar);

			scrollbar = new InvisibleFixedUIScrollbar(userInterface);
			scrollbar.SetView(100f, 1000f);
			scrollbar.Height.Set(0, 1f);
			scrollbar.Left.Set(-20, 1f);
			//myRootPanel.Append(scrollbar);
			myDamageDoneDisplay.SetScrollbar(scrollbar);

			ShowTeamDPSPanel = true;
			myDisplayMode = ListDisplayMode.DamageDone;
		}

		internal bool updateNeeded;

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			if (!updateNeeded) 
				return;

			if (!Main.LocalPlayer.accDreamCatcher)
			{
				UIText t = new UIText(Language.GetText(DPSExtreme.instance.GetLocalizationKey("NoDPSWearDPSMeter")));
				myCurrentDisplay?.Clear();
				myCurrentDisplay?.Add(t);
				myRootPanel.AddDragTarget(t);

				return;
			}

			myCurrentDisplay?.Update();

			updateNeeded = false;

			myRootPanel.Recalculate();
		}

		internal void RefreshLabel()
		{
			string title = Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey(myDisplayMode.ToString()));

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
							title += Language.GetTextValue("Bestiary_Invasions.Goblins");
							break;
						case DPSExtremeCombat.InvasionType.SnowLegion:
							title += Language.GetTextValue("Bestiary_Invasions.FrostLegion");
							break;
						case DPSExtremeCombat.InvasionType.PirateInvasion:
							title += Language.GetTextValue("Bestiary_Invasions.Pirates");
							break;
						case DPSExtremeCombat.InvasionType.MartianMadness:
							title += Language.GetTextValue("Bestiary_Invasions.Martian");
							break;
						case DPSExtremeCombat.InvasionType.PumpkinMoon:
							title += Language.GetTextValue("Bestiary_Invasions.PumpkinMoon");
							break;
						case DPSExtremeCombat.InvasionType.FrostMoon:
							title += Language.GetTextValue("Bestiary_Invasions.FrostMoon");
							break;
						case DPSExtremeCombat.InvasionType.OldOnesArmy:
							title += Language.GetTextValue("Bestiary_Invasions.OldOnesArmy");
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
							title += Language.GetTextValue("Bestiary_Events.BloodMoon");
							break;
						case DPSExtremeCombat.EventType.Eclipse:
							title += Language.GetTextValue("Bestiary_Events.Eclipse");
							break;
						case DPSExtremeCombat.EventType.SlimeRain:
							title += Language.GetTextValue("Bestiary_Events.SlimeRain");
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
					Main.PlayerRenderer.DrawPlayer(Main.Camera, Main.player[myHoveredParticipant], Main.screenPosition + r2.Center.ToVector2() + new Vector2(-10, -21), 0, Vector2.Zero);
				}
			}

			myHoveredParticipant = -1;

			if (myLabel.IsMouseHovering)
			{
				string hoverText = Language.GetText(DPSExtreme.instance.GetLocalizationKey("ClickToChangeDisplay")).Value;

				float mouseTextPulse = Main.mouseTextColor / 255f;
				UICommon.TooltipMouseText($"[c/{Utils.Hex3(Colors.RarityYellow * mouseTextPulse)}:{hoverText}]");
			}
		}

		private void Label_OnClick(UIMouseEvent evt, UIElement listeningElement)
		{
			myDisplayMode = (ListDisplayMode)(((int)myDisplayMode + 1) % (int)ListDisplayMode.Count);
		}
	}
}

