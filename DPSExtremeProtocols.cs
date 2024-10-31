using DPSExtreme.Combat.Stats;
using System.IO;
using static DPSExtreme.Combat.DPSExtremeCombat;

namespace DPSExtreme
{
	enum DPSExtremeMessageType : byte
	{
		StartCombatPush,
		UpgradeCombatPush,
		EndCombatPush,
		ShareCurrentDPSReq,
		CurrentDPSsPush,
		CurrentCombatTotalsPush,
	}

	abstract internal class DPSExtremeProtocol
	{
		abstract public void ToStream(BinaryWriter aWriter);
		abstract public bool FromStream(BinaryReader aReader);

		abstract public DPSExtremeMessageType GetDelimiter();
	}

	internal class ProtocolPushStartCombat : DPSExtremeProtocol
	{
		public override DPSExtremeMessageType GetDelimiter() { return DPSExtremeMessageType.StartCombatPush; }

		public override void ToStream(BinaryWriter aWriter)
		{
			aWriter.Write((byte)GetDelimiter());

			aWriter.Write((byte)myCombatType);
			aWriter.Write(myBossOrInvasionOrEventType);
		}

		public override bool FromStream(BinaryReader aReader)
		{
			myCombatType = (CombatType)aReader.ReadByte();
			myBossOrInvasionOrEventType = aReader.ReadInt32();

			return true;
		}

		public CombatType myCombatType = CombatType.Generic;
		public int myBossOrInvasionOrEventType = -1;
	}

	internal class ProtocolPushUpgradeCombat : DPSExtremeProtocol
	{
		public override DPSExtremeMessageType GetDelimiter() { return DPSExtremeMessageType.UpgradeCombatPush; }

		public override void ToStream(BinaryWriter aWriter)
		{
			aWriter.Write((byte)GetDelimiter());

			aWriter.Write((byte)myCombatType);
			aWriter.Write(myBossOrInvasionOrEventType);
		}

		public override bool FromStream(BinaryReader aReader)
		{
			myCombatType = (CombatType)aReader.ReadByte();
			myBossOrInvasionOrEventType = aReader.ReadInt32();

			return true;
		}

		public CombatType myCombatType = CombatType.Generic;
		public int myBossOrInvasionOrEventType = -1;
	}

	internal class ProtocolPushEndCombat : DPSExtremeProtocol
	{
		public override DPSExtremeMessageType GetDelimiter() { return DPSExtremeMessageType.EndCombatPush; }

		public override void ToStream(BinaryWriter aWriter)
		{
			aWriter.Write((byte)GetDelimiter());

			aWriter.Write((byte)myCombatType);
		}

		public override bool FromStream(BinaryReader aReader)
		{
			myCombatType = (CombatType)aReader.ReadByte();

			return true;
		}

		public CombatType myCombatType = CombatType.Generic;
	}

	internal class ProtocolPushCombatStats : DPSExtremeProtocol
	{
		public override DPSExtremeMessageType GetDelimiter() { return DPSExtremeMessageType.CurrentCombatTotalsPush; }

		public override void ToStream(BinaryWriter aWriter)
		{
			aWriter.Write((byte)GetDelimiter());

			aWriter.Write(myCombatIsActive);

			myDamageDone.ToStream(aWriter);
			myEnemyDamageTaken.ToStream(aWriter);
		}

		public override bool FromStream(BinaryReader aReader)
		{
			myCombatIsActive = aReader.ReadBoolean();

			myDamageDone.FromStream(aReader);
			myEnemyDamageTaken.FromStream(aReader);

			return true;
		}

		public bool myCombatIsActive = false;

		public DPSExtremeStatDictionary<int, DPSExtremeStatList<StatValue>> myEnemyDamageTaken = new DPSExtremeStatDictionary<int, DPSExtremeStatList<StatValue>>();
		public DPSExtremeStatList<DPSExtremeStatDictionary<int, StatValue>> myDamageDone = new DPSExtremeStatList<DPSExtremeStatDictionary<int, StatValue>>();
	}

	internal class ProtocolReqShareCurrentDPS : DPSExtremeProtocol
	{
		public override DPSExtremeMessageType GetDelimiter() { return DPSExtremeMessageType.ShareCurrentDPSReq; }

		public override void ToStream(BinaryWriter aWriter)
		{
			aWriter.Write((byte)GetDelimiter());

			aWriter.Write(myPlayer);
			aWriter.Write(myDPS);
		}

		public override bool FromStream(BinaryReader aReader)
		{
			myPlayer = aReader.ReadInt32();
			myDPS = aReader.ReadInt32();

			return true;
		}

		public int myPlayer = 0;
		public int myDPS = 0;
	}

	internal class ProtocolPushClientDPSs : DPSExtremeProtocol
	{
		public override DPSExtremeMessageType GetDelimiter() { return DPSExtremeMessageType.CurrentDPSsPush; }

		public override void ToStream(BinaryWriter aWriter)
		{
			aWriter.Write((byte)GetDelimiter());

			myDamagePerSecond.ToStream(aWriter);
		}

		public override bool FromStream(BinaryReader aReader)
		{
			myDamagePerSecond.FromStream(aReader);

			return true;
		}

		public DPSExtremeStatList<StatValue> myDamagePerSecond = new DPSExtremeStatList<StatValue>();
	}
}
