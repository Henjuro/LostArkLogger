using System;
using System.Collections.Generic;
namespace InetOptimizer
{
    public partial class PKTNewNpcSummon
    {
        public void SteamDecode(BitReader reader)
        {
            npcStruct = reader.Read<NpcStruct>();
            bytearray_1 = reader.ReadBytes(4072);
            OwnerId = reader.ReadUInt64();
            bytearray_0 = reader.ReadBytes(55);
            b_0 = reader.ReadByte();
        }
    }
}
