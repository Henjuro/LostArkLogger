/*
 * This file has been Auto Generated, Please do not edit.
 * If you need changes, follow the instructions written in the readme.md
 *
 * Generated by Herysia.
 */

using System;
using System.Collections.Generic;
using LostArkLogger.Types;

namespace LostArkLogger
{
    public class PartyMemberDatas
    {
        public bool valid = false;
        internal PartyMemberDatas()
        {
            //Made for conditional structures
        }

        internal PartyMemberDatas(BitReader reader)
        {
            valid = true;
            Count = reader.ReadUInt16();
            if(Count <= 40)
            {
                for(var i = 0; i < Count; i++)
                {
                    Data.Add(new PartyMemberData(reader));
                }
            }
        }

        public ushort Count { get; }
        public List<PartyMemberData> Data { get; } = new List<PartyMemberData>();
    }
}