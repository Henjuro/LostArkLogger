using System;
using System.Collections.Generic;
namespace InetOptimizer
{
    public partial class PKTNewNpcSummon
    {
        public void KoreaDecode(BitReader reader)
        {
            npcStruct = reader.Read<NpcStruct>();
            b_0 = reader.ReadByte();
            bytearray_1 = reader.ReadBytes(4087);
            OwnerId = reader.ReadUInt64();
            bytearray_0 = reader.ReadBytes(40);
        }
    }
}
