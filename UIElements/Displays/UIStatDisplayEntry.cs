using Terraria;
using System;
using Microsoft.Xna.Framework.Graphics;
using DPSExtreme.Combat.Stats;
using DPSExtreme.Config;

namespace DPSExtreme.UIElements.Displays
{
	internal class UIStatDisplayEntry : UIDisplayEntry
	{
		internal float myMax = 100000f;
		internal int myTotal = -1;
		internal int myValue = -1;

		internal int myParticipantIndex = -1;
		internal int myBaseKey = -1;

		protected override int GetEntryWidth()
		{
			return (int)(GetOuterDimensions().Width * (myValue / (float)myTotal));
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			bool drawRightText = true;

			if (myFormat == StatFormat.Time &&
				(myParentDisplay as UICombatInfoDisplay).myBreakdownDisplay != null)
			{
				drawRightText = false;
			}

			if (drawRightText)
			{
				myRightText = StatValue.FormatStatNumber(myValue, myFormat);

				if (DPSExtremeClientConfig.Instance.ShowPercentages && myTotal > 0)
					myRightText = $"{StatValue.FormatStatNumber(myValue, myFormat)} ({String.Format("{0:P0}", (float)myValue / myTotal)})";
			}
			
			DrawSelfBase(spriteBatch);

			if (IsMouseHovering && myParticipantIndex >= 0)
			{
				DPSExtremeUI.instance.myHoveredParticipant = myParticipantIndex;
				Main.hoverItemName = "";
			}
		}

		internal void SetValues(int aValue, int aMax, int aTotal)
		{
			myValue = aValue;
			myMax = aMax;
			myTotal = aTotal;
		}

		public override int CompareTo(object obj)
		{
			UIStatDisplayEntry other = obj as UIStatDisplayEntry;
			return -myValue.CompareTo(other.myValue);
		}
	}
}

