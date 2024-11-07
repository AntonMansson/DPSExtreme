using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualBasic;
using System.IO;

namespace DPSExtreme.Combat.Stats
{
	enum StatFormat
	{
		RawNumber,
		Time
	}

	internal interface IStatContainer
	{
		public abstract bool HasStats();
		public abstract void GetMaxAndTotal(out int aMax, out int aTotal);

		public abstract void ToStream(BinaryWriter aWriter);
		public abstract void FromStream(BinaryReader aReader);
	}

	internal class StatValue : IStatContainer
	{
		internal int myValue = 0;

		public StatValue()
		{

		}

		public StatValue(int aValue)
		{
			myValue = aValue;
		}

		public bool HasStats() { return myValue > 0; }

		public virtual void GetMaxAndTotal(out int aMax, out int aTotal)
		{
			aMax = myValue;
			aTotal = myValue;
		}

		internal static string FormatStatNumber(int aValue, StatFormat aFormat)
		{
			if (aFormat == StatFormat.RawNumber)
			{
				if (aValue >= 100000000)
					return FormatStatNumber(aValue / 1000000, aFormat) + "M";

				if (aValue >= 100000)
					return FormatStatNumber(aValue / 1000, aFormat) + "K";

				if (aValue >= 10000)
					return (aValue / 1000D).ToString("0.#") + "K";

				return aValue.ToString("#,0");
			}
			else if (aFormat == StatFormat.Time)
			{
				float seconds = (aValue / 60f) % 60;
				int minutes = (aValue / 60) / 60;
				if (minutes == 0)
					return seconds.ToString("#.0");
				else
					return string.Format("{0}:{1}", minutes.ToString(), seconds.ToString("#.0"));
			}

			return "Invalid format";
		}

		public void ToStream(BinaryWriter aWriter)
		{
			aWriter.Write7BitEncodedInt(myValue);
		}

		public void FromStream(BinaryReader aReader)
		{
			myValue = aReader.Read7BitEncodedInt();
		}

		public static implicit operator StatValue(int aValue) { return new StatValue(aValue); }
		public static implicit operator int(StatValue aValue) { return aValue.myValue; }
		public static StatValue operator +(StatValue a, int b) { return new StatValue(a.myValue + b); }
		public static StatValue operator -(StatValue a, int b) { return new StatValue(a.myValue - b); }
		public static bool operator >(StatValue a, int b) { return a.myValue > b; }
		public static bool operator <(StatValue a, int b) { return a.myValue < b; }
	}

	internal class TimeStatValue : StatValue
	{
		//List<> myApplicationTimes
		public TimeStatValue() { }
		public TimeStatValue(int aValue) : base(aValue) { }

		public static TimeStatValue operator +(TimeStatValue a, int b) { return new TimeStatValue(a.myValue + b); }

		public override void GetMaxAndTotal(out int aMax, out int aTotal)
		{
			aMax = (int)DPSExtremeUI.instance.myDisplayedCombat.myDuration.TotalSeconds * 60;
			aTotal = myValue;
		}
	}
}
