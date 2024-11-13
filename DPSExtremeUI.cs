using Microsoft.Xna.Framework;
using Terraria;
using Terraria.UI;
using Terraria.GameContent.UI.Elements;
using Microsoft.Xna.Framework.Graphics;
using DPSExtreme.UIElements;
using ReLogic.Content;
using Terraria.Localization;
using Terraria.ID;
using Terraria.ModLoader.UI;
using DPSExtreme.Combat;
using DPSExtreme.Combat.Stats;
using DPSExtreme.UIElements.Displays;
using DPSExtreme.Config;
using Terraria.ModLoader.Config;

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

				SetupDisplays();
				updateNeeded = true;
			}
		}

		internal UIDragablePanel myRootPanel;
		internal UIText myLabel;

		internal UIListDisplay<StatValue> myNeedDPSAccDisplay;
		internal UISelectDisplayModeDisplay mySelectDisplayModeDisplay;
		internal UICombatHistoryDisplay myCombatHistoryDisplay;

		internal UIListDisplay<StatValue> myDamagePerSecondDisplay;
		internal UIListDisplay<DPSExtremeStatDictionary<int, StatValue>> myDamageDoneDisplay;
		internal UIListDisplay<DPSExtremeStatDictionary<int, DPSExtremeStatDictionary<int, StatValue>>> myDamageTakenDisplay;
		internal UIListDisplay<StatValue> myDeathsDisplay;
		internal UIListDisplay<DPSExtremeStatDictionary<int, StatValue>> myKillsDisplay;
		internal UIListDisplay<DPSExtremeStatDictionary<int, StatValue>> myManaUsedDisplay;
		internal UIListDisplay<DPSExtremeStatDictionary<int, TimeStatValue>> myBuffUptimesDisplay;
		internal UIListDisplay<DPSExtremeStatDictionary<int, TimeStatValue>> myDebuffUptimesDisplay;

		internal UIStatDictionaryDisplay<DPSExtremeStatList<StatValue>> myEnemyDamageTakenDisplay;

		internal ListDisplayMode myPreviousDisplayMode = ListDisplayMode.DamageDone;

		private ListDisplayMode _myDisplayMode;
		internal ListDisplayMode myDisplayMode
		{
			get { return _myDisplayMode; }
			set
			{
				myRootPanel.RemoveChild(myCurrentDisplay);

				myPreviousDisplayMode = _myDisplayMode;
				_myDisplayMode = value;

				myRootPanel.Append(myCurrentDisplay);
				RefreshLabel();
				updateNeeded = true;
			}
		}

		UIDisplay myCurrentDisplay
		{
			get 
			{
				switch (myDisplayMode)
				{
					case ListDisplayMode.NeedAccessory:
						return myNeedDPSAccDisplay;
					case ListDisplayMode.DisplayModeSelect:
						return mySelectDisplayModeDisplay;
					case ListDisplayMode.CombatHistory:
						return myCombatHistoryDisplay;
					case ListDisplayMode.StatDisplaysStart:
					case ListDisplayMode.StatDisplaysEnd:
					case ListDisplayMode.DamagePerSecond:
						return myDamagePerSecondDisplay;
					case ListDisplayMode.DamageDone:
						return myDamageDoneDisplay;
					case ListDisplayMode.DamageTaken:
						return myDamageTakenDisplay;
					case ListDisplayMode.EnemyDamageTaken:
						return myEnemyDamageTakenDisplay;
					case ListDisplayMode.Deaths:
						return myDeathsDisplay;
					case ListDisplayMode.Kills:
						return myKillsDisplay;
					case ListDisplayMode.ManaUsed:
						return myManaUsedDisplay;
					case ListDisplayMode.BuffUptime:
						return myBuffUptimesDisplay;
					case ListDisplayMode.DebuffUptime:
						return myDebuffUptimesDisplay;
					default:
						return null;
				}
			}
			set { myCurrentDisplay = value; }
		}

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
			Color.LightSeaGreen,
			Color.LightSkyBlue,
			Color.LightSteelBlue,
			Color.Linen,
			Color.MediumPurple,
			Color.MediumVioletRed,
			Color.MistyRose,
			Color.Olive,
			Color.Plum,
			Color.Salmon,
			Color.Orange,
			Color.Silver,
			Color.Tan,
			Color.LightYellow
		};

		public DPSExtremeUI(UserInterface ui) : base(ui)
		{
			instance = this;
		}

		Asset<Texture2D> playerBackGroundTexture;
		public override void OnInitialize()
		{
			OnClientConfigLoad();

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
			myRootPanel.Height.Set(170f, 0f);
			myRootPanel.MinHeight.Set(50, 0f);
			myRootPanel.MaxHeight.Set(500, 0f);
			myRootPanel.BackgroundColor = new Color(73, 94, 171);
			myRootPanel.OverflowHidden = true;

			SetupDisplays();

			myLabel = new UIText("", 0.7f);
			//Figure out why tf this doesn't work
			myLabel.DynamicallyScaleDownToWidth = true;
			myLabel.MaxWidth.Set(50, 0);

			myLabel.Left.Pixels = 18;
			myLabel.Top.Pixels = 2;
			myLabel.OnLeftClick += Label_OnLeftClick;
			myLabel.OnRightClick += Label_OnRightClick;
			myRootPanel.Append(myLabel);
			myRootPanel.AddDragTarget(myLabel);

			RefreshLabel();


			var chooseDisplayModeButton = new UIHoverImageButton(DPSExtreme.instance.Assets.Request<Texture2D>("DisplayModeButton", AssetRequestMode.ImmediateLoad), Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("ClickToChangeDisplay")));
			chooseDisplayModeButton.OnLeftClick += (a, b) => 
			{
				if (myDisplayMode != ListDisplayMode.DisplayModeSelect)
					myDisplayMode = ListDisplayMode.DisplayModeSelect;
				else
					myDisplayMode = myPreviousDisplayMode;
			};
			chooseDisplayModeButton.Left.Set(0, 0);
			chooseDisplayModeButton.Top.Pixels = -1;
			chooseDisplayModeButton.Recalculate();
			myRootPanel.Append(chooseDisplayModeButton);

			var combatHistoryButton = new UIHoverImageButton(DPSExtreme.instance.Assets.Request<Texture2D>("HistoryButton", AssetRequestMode.ImmediateLoad), Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey("ShowCombatHistory")));
			combatHistoryButton.OnLeftClick += (a, b) =>
			{
				if (myDisplayMode != ListDisplayMode.CombatHistory)
					myDisplayMode = ListDisplayMode.CombatHistory;
				else
					myDisplayMode = myPreviousDisplayMode;
			};
			combatHistoryButton.Left.Set(-16, 1f);
			combatHistoryButton.Top.Pixels = -1;
			combatHistoryButton.Recalculate();
			myRootPanel.Append(combatHistoryButton);

			ShowTeamDPSPanel = true;
			myDisplayMode = ListDisplayMode.DamageDone;
		}

		internal void SetupDisplays()
		{
			if (myDamageDoneDisplay != null) //Doesn't matter which one, just checking if it's first time we're setting up
				myRootPanel.RemoveChild(myCurrentDisplay);

			myNeedDPSAccDisplay = new UIListDisplay<StatValue>(ListDisplayMode.NeedAccessory);
			myNeedDPSAccDisplay.Add(new UIText(Language.GetText(DPSExtreme.instance.GetLocalizationKey("NoDPSWearDPSMeter"))));

			mySelectDisplayModeDisplay = new UISelectDisplayModeDisplay();
			myCombatHistoryDisplay = new UICombatHistoryDisplay();

			myDamagePerSecondDisplay = new UIListDisplay<StatValue>(ListDisplayMode.DamagePerSecond);
			myDamageDoneDisplay = new UIListDisplay<DPSExtremeStatDictionary<int, StatValue>>(ListDisplayMode.DamageDone);
			myDamageTakenDisplay = new UIListDisplay<DPSExtremeStatDictionary<int, DPSExtremeStatDictionary<int, StatValue>>>(ListDisplayMode.DamageTaken);
			myManaUsedDisplay = new UIListDisplay<DPSExtremeStatDictionary<int, StatValue>>(ListDisplayMode.ManaUsed);
			myBuffUptimesDisplay = new UIListDisplay<DPSExtremeStatDictionary<int, TimeStatValue>>(ListDisplayMode.BuffUptime, StatFormat.Time);
			myDebuffUptimesDisplay = new UIListDisplay<DPSExtremeStatDictionary<int, TimeStatValue>>(ListDisplayMode.DebuffUptime, StatFormat.Time);

			myEnemyDamageTakenDisplay = new UIStatDictionaryDisplay<DPSExtremeStatList<StatValue>>(ListDisplayMode.EnemyDamageTaken);

			myDeathsDisplay = new UIListDisplay<StatValue>(ListDisplayMode.Deaths);
			myKillsDisplay = new UIListDisplay<DPSExtremeStatDictionary<int, StatValue>>(ListDisplayMode.Kills);

			myRootPanel.Append(myCurrentDisplay);
		}

		public void OnClientConfigLoad()
		{
			updateNeeded = true;
		}

		public void OnServerConfigLoad()
		{
			updateNeeded = true;
		}

		internal bool updateNeeded;

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			myRootPanel.Update();

			if (!Main.LocalPlayer.accDreamCatcher && myDisplayMode != ListDisplayMode.NeedAccessory)
			{
				myDisplayMode = ListDisplayMode.NeedAccessory;
			}
			else if (Main.LocalPlayer.accDreamCatcher && myDisplayMode == ListDisplayMode.NeedAccessory)
			{
				myDisplayMode = myPreviousDisplayMode;
				myPreviousDisplayMode = ListDisplayMode.DamageDone; //Just in case
			}

			RefreshLabel(); //Every frame for timer

			if (!updateNeeded)
				return;

			myCurrentDisplay?.Update();

			updateNeeded = false;

			myRootPanel.Recalculate();
		}

		internal void RefreshLabel()
		{
			string title = Language.GetTextValue(DPSExtreme.instance.GetLocalizationKey(myDisplayMode.ToString()));

			if (myDisplayedCombat == null || 
				myDisplayMode < ListDisplayMode.StatDisplaysStart)
			{
				myLabel.SetText(title);
				myLabel.Recalculate();
				return;
			}

			title += " - ";

			if (myCurrentDisplay.myLabelOverride != null)
				title += myCurrentDisplay.myLabelOverride;
			else
				title += myDisplayedCombat.GetTitle();

			if (title.Length > 27)
				title = title.Remove(27);

			if (myCurrentDisplay.myLabelOverride != null)
				title += " " + myDisplayedCombat.myFormattedDuration;

			myLabel.SetText(title);
			myLabel.Recalculate();
		}

		internal void OnEnterWorld()
		{
			myDisplayedCombat = null;
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
			updateNeeded = true;
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			//base.DrawSelf(spriteBatch);

			bool IsPlayer = myHoveredParticipant >= 0 && myHoveredParticipant < (int)InfoListIndices.SupportedPlayerCount;
			bool isNPC = myHoveredParticipant == (int)InfoListIndices.NPCs;
			bool enableHoveredDisplay = false;
			if (enableHoveredDisplay && (IsPlayer || isNPC))
			{
				Rectangle hitbox = myRootPanel.GetOuterDimensions().ToRectangle();
				Rectangle r2 = new Rectangle(hitbox.X + (hitbox.Width / 2) - (58 / 2), hitbox.Y - 58, 58, 58);
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
						//drawNPC.IsABestiaryIconDummy = false;
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

		private void Label_OnLeftClick(UIMouseEvent evt, UIElement listeningElement)
		{
			if (myRootPanel.dragging)
				return;

			myDisplayMode = (ListDisplayMode)(((int)myDisplayMode + 1) % (int)ListDisplayMode.StatDisplaysEnd);

			if (myDisplayMode <= ListDisplayMode.StatDisplaysStart)
				myDisplayMode = ListDisplayMode.StatDisplaysStart + 1;
		}

		private void Label_OnRightClick(UIMouseEvent evt, UIElement listeningElement)
		{
			if (myDisplayMode < ListDisplayMode.StatDisplaysStart || myDisplayMode > ListDisplayMode.StatDisplaysEnd)
				return;

			int next = ((int)myDisplayMode - 1);

			if (next <= (int)ListDisplayMode.StatDisplaysStart)
				next = (int)ListDisplayMode.StatDisplaysEnd - 1;

			myDisplayMode = (ListDisplayMode)next;
		}
	}
}

