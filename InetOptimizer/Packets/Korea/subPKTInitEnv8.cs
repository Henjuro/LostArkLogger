using System;
using System.Collections.Generic;
namespace InetOptimizer
{
    public partial class subPKTInitEnv8
    {
        public void KoreaDecode(BitReader reader)
        {
            num = reader.ReadUInt16();
            for(var i = 0; i < num; i++)
            {
                u16list_2.Add(reader.ReadList<UInt16>());
                u16list_0.Add(reader.ReadList<UInt16>());
                u16list_1.Add(reader.ReadList<UInt16>());
            }
        }
    }
}
