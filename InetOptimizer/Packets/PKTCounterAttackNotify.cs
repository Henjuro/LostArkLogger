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
    public class PKTCounterAttackNotify
    {
        public PKTCounterAttackNotify(BitReader reader)
        {
            SourceId = reader.ReadUInt64();
            reader.Skip(2);
            TargetId = reader.ReadUInt64();
            Type = reader.ReadUInt32();
            reader.Skip(1);
        }

        public ulong SourceId { get; }
        public ulong TargetId { get; }
        public uint Type { get; }
    }
}