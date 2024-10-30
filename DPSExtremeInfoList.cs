﻿
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using static DPSExtreme.CombatTracking.DPSExtremeCombat;

namespace DPSExtreme
{
	internal enum InfoListIndices
	{
		Players = 0,
		//0-... is players
		SupportedPlayerCount = 240,
		DisconnectedPlayersStart = 241,
		DisconnectedPlayersEnd = 250,
		DOTs = 253,
		Traps = 255,
		NPCs = 255
	}

	internal class DPSExtremeInfoList
	{
		internal int[] myValues;

		public DPSExtremeInfoList()
		{
			myValues = new int[Size()];

			for (int i = 0; i < Size(); i++)
			{
				myValues[i] = new int();
			}
		}

		public ref int this[int i]
		{
			get { return ref myValues[i]; }
		}

		public int Size() { return 256; }

		public void Clear()
		{
			for (int i = 0; i < Size(); i++)
			{
				myValues[i] = default;
			}
		}

		public void GetMaxAndTotal(out int aMax, out int aTotal)
		{
			aMax = 0;
			aTotal = 0;

			for (int i = 0; i < Size(); i++)
			{
				int value = myValues[i];
				if (value > 0)
				{
					aMax = Math.Max(aMax, value);
					aTotal += value;
				}
			}
		}

		public void ToStream(BinaryWriter aWriter)
		{
			long startPos = aWriter.BaseStream.Position;

			aWriter.Write((byte)123); //placeholder for count

			byte count = 0;
			for (int i = 0; i < Size(); i++)
			{
				if (myValues[i] <= 0)
					continue;

				aWriter.Write((byte)i);
				aWriter.Write(myValues[i]);
				count++;
			}

			long endPos = aWriter.BaseStream.Position;

			aWriter.BaseStream.Seek(startPos, SeekOrigin.Begin);
			aWriter.Write(count);
			aWriter.BaseStream.Seek(endPos, SeekOrigin.Begin);
		}

		public void FromStream(BinaryReader aReader)
		{
			int count = aReader.ReadByte();

			for (int i = 0; i < count; i++)
			{
				byte index = aReader.ReadByte();
				myValues[index] = aReader.ReadInt32();
			}
		}
	}
}
