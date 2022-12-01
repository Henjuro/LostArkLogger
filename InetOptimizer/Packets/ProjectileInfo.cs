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
    public class ProjectileInfo
    {
        public bool valid = false;
        internal ProjectileInfo()
        {
            //Made for conditional structures
        }

        internal ProjectileInfo(BitReader reader)
        {
            valid = true;
            SkillEffect = reader.ReadUInt32();
            SkillId = reader.ReadUInt32();
            ProjectileId = reader.ReadUInt64();
            Unk3 = reader.ReadInt32();
            Unk4 = reader.ReadByte();
            if(Unk4 == 1)
            {
                Unk4_0 = reader.ReadInt32();
            }
            Unk5 = reader.ReadInt32();
            Unk6 = reader.ReadInt64();
            SkillLevel = reader.ReadByte();
            Unk8 = reader.ReadByte();
            if(Unk8 == 1)
            {
                Unk8_0 = new Struct_316(reader);
            }
            Unk9 = reader.ReadInt32();
            Unk10 = reader.ReadInt16();
            OwnerId = reader.ReadUInt64();
            Tripods = reader.ReadBytes(3);
            Unk13 = reader.ReadByte();
            Unk14 = reader.ReadByte();
            if(Unk14 == 1)
            {
                Unk14_0 = reader.ReadInt64();
            }
            Unk15 = reader.ReadByte();
            Unk16 = reader.ReadInt64();
            Unk17 = reader.ReadInt64();
            Unk18 = reader.ReadBytes(6);
            Unk19 = reader.ReadInt32();
            Unk20 = reader.ReadInt16();
        }

        public uint SkillEffect { get; }
        public uint SkillId { get; }
        public ulong ProjectileId { get; }
        public int Unk3 { get; }
        public byte Unk4 { get; }
        public int Unk4_0 { get; }
        public int Unk5 { get; }
        public long Unk6 { get; }
        public byte SkillLevel { get; }
        public byte Unk8 { get; }
        public Struct_316 Unk8_0 { get; } = new Struct_316();
        public int Unk9 { get; }
        public short Unk10 { get; }
        public ulong OwnerId { get; }
        public byte[] Tripods { get; }
        public byte Unk13 { get; }
        public byte Unk14 { get; }
        public long Unk14_0 { get; }
        public byte Unk15 { get; }
        public long Unk16 { get; }
        public long Unk17 { get; }
        public byte[] Unk18 { get; }
        public int Unk19 { get; }
        public short Unk20 { get; }
    }
}