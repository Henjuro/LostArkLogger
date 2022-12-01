/*
 * This file has been Auto Generated, Please do not edit.
 * If you need changes, follow the instructions written in the readme.md
 *
 * Generated by Herysia.
 */

using System;
using System.Collections.Generic;
using InetOptimizer.Types;

namespace InetOptimizer
{
    public class PartyMemberData
    {
        public bool valid = false;
        internal PartyMemberData()
        {
            //Made for conditional structures
        }

        internal PartyMemberData(BitReader reader)
        {
            valid = true;
            Unk0 = reader.ReadInt16();
            Name = new LostArkString(reader);
            CurHP = new ReadNBytesInt64(reader);
            Unk3 = reader.ReadByte();
            Unk4 = reader.ReadByte();
            Unk5 = reader.ReadInt32();
            Unk6 = reader.ReadInt16();
            Unk7 = reader.ReadInt32();
            Unk8 = reader.ReadByte();
            Unk9 = reader.ReadInt64();
            CharacterLevel = reader.ReadUInt16();
            Unk11 = reader.ReadByte();
            Unk12 = reader.ReadByte();
            CharacterId = reader.ReadUInt64();
            MaxHP = new ReadNBytesInt64(reader);
            PartyMemberNumber = reader.ReadByte();
            Unk16 = reader.ReadByte();
            Unk17 = reader.ReadByte();
            Unk18 = reader.ReadInt64();
            Unk19 = reader.ReadInt32();
        }

        public short Unk0 { get; }
        public LostArkString Name { get; } = new LostArkString();
        public ReadNBytesInt64 CurHP { get; } = new ReadNBytesInt64();
        public byte Unk3 { get; }
        public byte Unk4 { get; }
        public int Unk5 { get; }
        public short Unk6 { get; }
        public int Unk7 { get; }
        public byte Unk8 { get; }
        public long Unk9 { get; }
        public ushort CharacterLevel { get; }
        public byte Unk11 { get; }
        public byte Unk12 { get; }
        public ulong CharacterId { get; }
        public ReadNBytesInt64 MaxHP { get; } = new ReadNBytesInt64();
        public byte PartyMemberNumber { get; }
        public byte Unk16 { get; }
        public byte Unk17 { get; }
        public long Unk18 { get; }
        public int Unk19 { get; }
    }
}