using System.Collections.Generic;
using System.IO;
using static DPSExtreme.CombatTracking.DPSExtremeCombat;

namespace DPSExtreme
{
	enum DPSExtremeMessageType : byte
	{
		InformServerCurrentDPS,
		InformClientsCurrentDPSs,
		InformClientsCurrentCombatTotals,
	}

	abstract internal class DPSExtremeProtocol
	{
		abstract public void ToStream(BinaryWriter aWriter);
		abstract public bool FromStream(BinaryReader aReader);

		abstract public DPSExtremeMessageType GetDelimiter();
	}

	internal class ProtocolPushCombatStats : DPSExtremeProtocol
	{
		public override DPSExtremeMessageType GetDelimiter() { return DPSExtremeMessageType.InformClientsCurrentCombatTotals; }

		public override void ToStream(BinaryWriter aWriter)
		{
			aWriter.Write((byte)GetDelimiter());

			aWriter.Write(myCombatIsActive);

			//<DamageDealerCount> 2
			//<DamageDealerId> 1 : <totalDamage> 1573
			//<DamageDealerId> 7 : <totalDamage> 213

			myTotalDamageDealtList.ToStream(aWriter);

			//<DamagedNPCTypesCount> 3
			//<NPCType> 14(Penguin) : [ <DamageDealerId> 1 : <damage> 0, <DamageDealerId> 7 : 15 ]
			//<NPCType> 23(Slime) : [ <DamageDealerId> 1 : <damage> 35, <DamageDealerId> 7 : 5 ]
			//<NPCType> 143(King Slime) : [ <DamageDealerId> 1 : <damage> 35, <DamageDealerId> 7 : 5 ]
			//npc ids not accurate btw

			aWriter.Write(myDamageDealtPerNPCType.Count);
			foreach ((int NPCType, DPSExtremeInfoList<DamageDealtInfo> damageInfo) in myDamageDealtPerNPCType)
			{
				aWriter.Write(NPCType);

				damageInfo.ToStream(aWriter);
			}
		}

		public override bool FromStream(BinaryReader aReader)
		{
			myCombatIsActive = aReader.ReadBoolean();

			myTotalDamageDealtList.FromStream(aReader);

			int myDamageDealtPerNPCTypeCount = aReader.ReadInt32();

			for (int i = 0; i < myDamageDealtPerNPCTypeCount; i++)
			{
				int npcType = aReader.ReadInt32();

				myDamageDealtPerNPCType[npcType].FromStream(aReader);
			}

			return true;
		}

		public bool myCombatIsActive = false;

		public Dictionary<int, DPSExtremeInfoList<DamageDealtInfo>> myDamageDealtPerNPCType = new Dictionary<int, DPSExtremeInfoList<DamageDealtInfo>>();
		public DPSExtremeInfoList<DamageDealtInfo> myTotalDamageDealtList= new DPSExtremeInfoList<DamageDealtInfo>();
	}

	internal class ProtocolReqInformServerCurrentDPS : DPSExtremeProtocol
	{
		public override DPSExtremeMessageType GetDelimiter() { return DPSExtremeMessageType.InformServerCurrentDPS; }

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
		public override DPSExtremeMessageType GetDelimiter() { return DPSExtremeMessageType.InformClientsCurrentDPSs; }

		public override void ToStream(BinaryWriter aWriter)
		{
			aWriter.Write((byte)GetDelimiter());

			myDPSList.ToStream(aWriter);
		}

		public override bool FromStream(BinaryReader aReader)
		{
			myDPSList.FromStream(aReader);

			return true;
		}

		public DPSExtremeInfoList<DamageDealtInfo> myDPSList = new DPSExtremeInfoList<DamageDealtInfo>();
	}
}
