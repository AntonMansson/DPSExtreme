using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader.UI.Elements;

namespace DPSExtreme.UIElements
{
	internal enum ListDisplayMode
	{
		DamageDone,
		DamagePerSecond,
		EnemyDamageTaken,
		Count
	}

	internal abstract class UICombatInfoDisplay : UIGrid
	{
		internal int myHighestValue = -1;
		internal int myTotal = 0;

		protected UICombatInfoDisplay()
		{
			Width.Percent = 1f;
			Height.Set(-20, 1f);
			Top.Pixels = 20;
			ListPadding = 0f;
		}

		internal abstract void Update();
		internal abstract void RecalculateTotals();
		internal abstract void UpdateValues();
	}
}
