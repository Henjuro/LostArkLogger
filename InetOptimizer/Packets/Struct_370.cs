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
    public class Struct_370
    {
        public bool valid = false;
        internal Struct_370()
        {
            //Made for conditional structures
        }

        internal Struct_370(BitReader reader)
        {
            valid = true;
            Unk0 = reader.ReadInt16();
            if(Unk0 <= 5)
            {
                for(var i = 0; i < Unk0; i++)
                {
                    Unk0_0_0.Add(new Struct_676(reader));
                }
            }
        }

        public short Unk0 { get; }
        public List<Struct_676> Unk0_0_0 { get; } = new List<Struct_676>();
    }
}