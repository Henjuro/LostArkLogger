/*
 * This file has been Auto Generated, Please do not edit.
 * If you need changes, follow the instructions written in the readme.md
 *
 * Generated by Herysia.
 */

using System;
using System.Collections.Generic;

namespace LostArkLogger
{
    public class Struct_662
    {
        public bool valid = false;
        internal Struct_662()
        {
            //Made for conditional structures
        }

        internal Struct_662(BitReader reader)
        {
            valid = true;
            EffectInstanceId = reader.ReadUInt32();
            Unk1 = reader.ReadUInt64();
            SourceId = reader.ReadUInt64();
            Unk3 = new Struct_396(reader);
            Unk4 = reader.ReadByte();
            Unk5 = reader.ReadUInt32();
            Unk6 = reader.ReadByte();
            Unk7 = reader.ReadByte();
            if (Unk7 == 1)
            {
                Unk7_0 = reader.ReadBytes(16);
            }
            Unk8 = reader.ReadSimpleInt();
            Unk9 = reader.ReadByte();
            if (Unk9 == 1)
            {
                Unk9_0 = reader.ReadUInt64();
            }
            StatusEffectId = reader.ReadUInt32();
        }

        public UInt32 EffectInstanceId { get; }
        public UInt64 Unk1 { get; }
        // the original caster in case of refreshed effect
        public UInt64 SourceId { get; }
        public Struct_396 Unk3 { get; } = new Struct_396();
        public byte Unk4 { get; }
        public UInt32 Unk5 { get; }
        public byte Unk6 { get; }
        public byte Unk7 { get; }
        public byte[] Unk7_0 { get; }
        public UInt64 Unk8 { get; } // a date and time
        public byte Unk9 { get; }
        public UInt64 Unk9_0 { get; }
        public UInt32 StatusEffectId { get; }
    }
}