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
    public class StatusEffectDatas
    {
        public bool valid = false;
        internal StatusEffectDatas()
        {
            //Made for conditional structures
        }

        internal StatusEffectDatas(BitReader reader)
        {
            valid = true;
            Count = reader.ReadUInt16();
            if(Count <= 80)
            {
                for(var i = 0; i < Count; i++)
                {
                    Datas.Add(new StatusEffectData(reader));
                }
            }
        }

        public ushort Count { get; }
        public List<StatusEffectData> Datas { get; } = new List<StatusEffectData>();
    }
}