using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualBasic;
using System.IO;

namespace DPSExtreme.Combat.Stats
{
	internal interface IStat
	{
		public abstract bool HasStats();
		public abstract void GetMaxAndTotal(out int aMax, out int aTotal);

		public abstract void ToStream(BinaryWriter aWriter);
		public abstract void FromStream(BinaryReader aReader);
	}

	internal struct StatValue : IStat
	{
		internal int myValue = 0;

		public bool IsStatValue = true;

		public StatValue()
		{

		}

		public StatValue(int aValue)
		{
			myValue = aValue;
		}

		public bool HasStats() { return myValue > 0; }

		public void GetMaxAndTotal(out int aMax, out int aTotal)
		{
			aMax = myValue;
			aTotal = myValue;
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
}
