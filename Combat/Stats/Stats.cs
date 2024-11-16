using System;
using System.Collections.Generic;
using System.IO;

namespace DPSExtreme.Combat.Stats
{
	enum StatFormat
	{
		RawNumber,
		Time
	}

	internal class CombatStats
	{
		internal DPSExtremeStatDictionary<int, DPSExtremeStatList<DamageStatValue>> myEnemyDamageTaken = new();

		internal DPSExtremeStatList<DPSExtremeStatDictionary<int, DamageStatValue>> myDamageDone = new();
		internal DPSExtremeStatList<DPSExtremeStatDictionary<int, DPSExtremeStatDictionary<int, DamageStatValue>>> myDamageTaken = new();
		internal DPSExtremeStatList<StatValue> myDeaths = new();
		internal DPSExtremeStatList<DPSExtremeStatDictionary<int, StatValue>> myKills = new();
		internal DPSExtremeStatList<DPSExtremeStatDictionary<int, StatValue>> myManaUsed = new();

		internal DPSExtremeStatList<DPSExtremeStatDictionary<int, TimeStatValue>> myBuffUptimes = new();
		internal DPSExtremeStatList<DPSExtremeStatDictionary<int, TimeStatValue>> myDebuffUptimes = new();

		internal DPSExtremeStatList<StatValue> myDamagePerSecond = new();

		public void ToStream(BinaryWriter aWriter)
		{
			myEnemyDamageTaken.ToStream(aWriter);
			myDamageDone.ToStream(aWriter);
			myDamageTaken.ToStream(aWriter);
			myDeaths.ToStream(aWriter);
			myKills.ToStream(aWriter);
			myManaUsed.ToStream(aWriter);
			myBuffUptimes.ToStream(aWriter);
			myDebuffUptimes.ToStream(aWriter);
		}

		public void FromStream(BinaryReader aReader)
		{
			myEnemyDamageTaken.FromStream(aReader);
			myDamageDone.FromStream(aReader);
			myDamageTaken.FromStream(aReader);
			myDeaths.FromStream(aReader);
			myKills.FromStream(aReader);
			myManaUsed.FromStream(aReader);
			myBuffUptimes.FromStream(aReader);
			myDebuffUptimes.FromStream(aReader);
		}

		internal void ReassignStats(int aFrom, int aTo)
		{
			foreach ((int npcType, DPSExtremeStatList<DamageStatValue> damageInfo) in myEnemyDamageTaken)
			{
				myEnemyDamageTaken[npcType][aTo] = myEnemyDamageTaken[npcType][aFrom];
			}

			myDamagePerSecond[aTo] = myDamagePerSecond[aFrom];
			myDamageDone[aTo] = myDamageDone[aFrom];
			myDamageTaken[aTo] = myDamageTaken[aFrom];
			myDeaths[aTo] = myDeaths[aFrom];
			myKills[aTo] = myKills[aFrom];
			myManaUsed[aTo] = myManaUsed[aFrom];

			myBuffUptimes[aTo] = myBuffUptimes[aFrom];
			myDebuffUptimes[aTo] = myDebuffUptimes[aFrom];

			ClearStatsForPlayer(aFrom);
		}

		internal void ClearStatsForPlayer(int aPlayer)
		{
			myDamageDone[aPlayer].Clear();

			foreach ((int npcType, DPSExtremeStatList<DamageStatValue> damageInfo) in myEnemyDamageTaken)
				myEnemyDamageTaken[npcType][aPlayer] = new();

			myDamagePerSecond[aPlayer] = new();
			myDamageTaken[aPlayer].Clear();
			myDeaths[aPlayer] = new();
			myKills[aPlayer].Clear();
			myManaUsed[aPlayer].Clear();

			myBuffUptimes[aPlayer].Clear();
			myDebuffUptimes[aPlayer].Clear();
		}
	}

	internal interface IStatContainer
	{
		public abstract bool HasStats();
		public abstract void GetMaxAndTotal(out int aMax, out int aTotal);

		public abstract void ToStream(BinaryWriter aWriter);
		public abstract void FromStream(BinaryReader aReader);

		public abstract List<string> GetInfoBoxLines();
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

		public virtual List<string> GetInfoBoxLines() { return new List<string>(); }

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
					return string.Format("{0:D2}:{1:D2}", minutes, (int)seconds);
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
			aMax = (int)DPSExtremeUI.instance.myDisplayedCombat.myDurationInTicks;
			aTotal = myValue;
		}
	}

	internal class DamageStatValue : StatValue
	{
		internal int myHitCount = 0;
		internal int myCritCount = 0;
		internal int myMaxHit = 0;

		public DamageStatValue() { }

		public void AddDamage(int aDamage, bool aCrit)
		{
			myHitCount += 1;
			myCritCount += aCrit ? 1 : 0;
			myValue += aDamage;
			myMaxHit = Math.Max(myMaxHit, aDamage);
		}

		public override List<string> GetInfoBoxLines() 
		{
			List<string> lines = new List<string>();
			lines.Add(string.Format("Total Damage: {0}", myValue));
			lines.Add(string.Format("Hits: {0}", myHitCount));
			lines.Add(string.Format("Max hit: {0}", myMaxHit));
			if (myHitCount > 0) lines.Add(string.Format("Average Damage: {0}", myValue / myHitCount));
			if (myCritCount > 0) lines.Add(string.Format("Crits: {0} ({1:P0})", myCritCount, myCritCount / (float)myHitCount));

			return lines; 
		}

		public new void ToStream(BinaryWriter aWriter)
		{
			aWriter.Write7BitEncodedInt(myValue);
			aWriter.Write7BitEncodedInt(myHitCount);
			aWriter.Write7BitEncodedInt(myCritCount);
		}

		public new void FromStream(BinaryReader aReader)
		{
			myValue = aReader.Read7BitEncodedInt();
			myHitCount = aReader.Read7BitEncodedInt();
			myCritCount = aReader.Read7BitEncodedInt();
		}
	}
}
