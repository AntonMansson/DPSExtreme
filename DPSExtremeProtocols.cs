using System.Collections.Generic;
using System.IO;

namespace DPSExtreme
{
	enum DPSExtremeMessageType : byte
	{
		InformServerCurrentDPS,
		InformClientsCurrentDPSs,
		InformClientsCurrentBossTotals,
	}

	abstract internal class DPSExtremeProtocol
	{
		abstract public void ToStream(BinaryWriter aWriter);
		abstract public bool FromStream(BinaryReader aReader);

		abstract public DPSExtremeMessageType GetDelimiter();
	}

	internal class ProtocolPushBossFightStats : DPSExtremeProtocol
	{
		public override DPSExtremeMessageType GetDelimiter() { return DPSExtremeMessageType.InformClientsCurrentBossTotals; }

		public override void ToStream(BinaryWriter aWriter) {
			aWriter.Write((byte)GetDelimiter());

			aWriter.Write(myBossIsDead);
			aWriter.Write((byte)myBossIndex);
			aWriter.Write(myPlayerCount);

			for (int i = 0; i < myPlayerCount; i++) {
				aWriter.Write((byte)myPlayerIndices[i]);
				aWriter.Write(myPlayerDPSs[i]);
			}

			aWriter.Write(myBossDamageTakenFromDOT);
		}

		public override bool FromStream(BinaryReader aReader) {
			myBossIsDead = aReader.ReadBoolean();
			myBossIndex = aReader.ReadByte();
			myPlayerCount = aReader.ReadInt32();

			if (myPlayerCount > 256)
				return false;

			for (int i = 0; i < myPlayerCount; i++) {
				myPlayerIndices.Add(aReader.ReadByte());
				myPlayerDPSs.Add(aReader.ReadInt32());
			}

			myBossDamageTakenFromDOT = aReader.ReadInt32();

			return true;
		}

		public bool myBossIsDead = false;
		public byte myBossIndex = 255;
		public int myPlayerCount = 0;
		public List<byte> myPlayerIndices = new List<byte>();
		public List<int> myPlayerDPSs = new List<int>();
		public int myBossDamageTakenFromDOT = 0;
	}

	internal class ProtocolReqInformServerCurrentDPS : DPSExtremeProtocol
	{
		public override DPSExtremeMessageType GetDelimiter() { return DPSExtremeMessageType.InformServerCurrentDPS; }

		public override void ToStream(BinaryWriter aWriter) {
			aWriter.Write((byte)GetDelimiter());

			aWriter.Write(myPlayer);
			aWriter.Write(myDPS);
		}

		public override bool FromStream(BinaryReader aReader) {
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

		public override void ToStream(BinaryWriter aWriter) {
			aWriter.Write((byte)GetDelimiter());

			aWriter.Write(myPlayerCount);

			for (int i = 0; i < myPlayerCount; i++) {
				aWriter.Write((byte)myPlayerIndices[i]);
				aWriter.Write(myPlayerDPSs[i]);
			}
		}

		public override bool FromStream(BinaryReader aReader) {
			myPlayerCount = aReader.ReadInt32();

			if (myPlayerCount >= 256)
				return false;

			for (int i = 0; i < myPlayerCount; i++) {
				myPlayerIndices.Add(aReader.ReadByte());
				myPlayerDPSs.Add(aReader.ReadInt32());
			}

			return true;
		}

		public int myPlayerCount = 0;
		public List<int> myPlayerDPSs = new List<int>();
		public List<byte> myPlayerIndices = new List<byte>();
	}
}
