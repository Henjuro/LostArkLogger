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
    public class Struct_396
    {
        public bool valid = false;
        internal Struct_396()
        {
            //Made for conditional structures
        }

        internal Struct_396(BitReader reader)
        {
            valid = true;
            Unk0 = reader.ReadUInt16();
            if(Unk0 <= 8)
            {
                Unk0_0 = reader.ReadBytes(7*Unk0);
            }
        }

        public UInt16 Unk0 { get; }
        // 6. byte is skill level
        public byte[] Unk0_0 { get; }
    }
}