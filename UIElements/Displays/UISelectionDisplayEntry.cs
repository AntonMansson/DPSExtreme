
using DPSExtreme.Combat;
using Microsoft.Xna.Framework.Graphics;
using System;
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

		protected override int GetEntryWidth()
		{
			if (DPSExtremeUI.instance.myDisplayMode == ListDisplayMode.CombatHistory)
			{
				int longestCombatInSeconds = 0;

				for (int i = 0; i < DPSExtremeCombatTracker.ourHistorySize; i++)
				{
					var combat = DPSExtreme.instance.combatTracker.GetCombatHistory(i);

					if (combat == null)
						continue;

					longestCombatInSeconds = Math.Max(longestCombatInSeconds, (int)combat.myDuration.TotalSeconds);
				}

				DPSExtremeCombat combatHistory = DPSExtreme.instance.combatTracker.GetCombatHistory(myIndex - 1);

				if (combatHistory == null)
					return (int)GetOuterDimensions().Width;

				int combatDuration = (int)combatHistory.myDuration.TotalSeconds;

				return (int)(GetOuterDimensions().Width * (combatDuration / (float)longestCombatInSeconds));
			}

			return (int)GetOuterDimensions().Width;
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
