using Microsoft.Xna.Framework;
using Terraria;
using System;
using Terraria.GameContent;
using ReLogic.Graphics;
using Microsoft.Xna.Framework.Graphics;

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
			return (int)(GetOuterDimensions().Width * (myValue / myMax));
		}

		private string FormatStatNumber(int aValue)
		{
			if (aValue >= 100000000) 
				return FormatStatNumber(aValue / 1000000) + "M";

			if (aValue >= 100000)
				return FormatStatNumber(aValue / 1000) + "K";

			if (aValue >= 10000)
				return (aValue / 1000D).ToString("0.#") + "K";

			return aValue.ToString("#,0");
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			myRightText = FormatStatNumber(myValue);

			if (DPSExtremeUI.instance.myShowPercent && myTotal > 0)
				myRightText = $"{FormatStatNumber(myValue)} ({String.Format("{0:P0}", (float)myValue / myTotal)})";

			DrawSelfBase(spriteBatch);

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
			UIStatDisplayEntry other = obj as UIStatDisplayEntry;
			return -myValue.CompareTo(other.myValue);
		}
	}
}

