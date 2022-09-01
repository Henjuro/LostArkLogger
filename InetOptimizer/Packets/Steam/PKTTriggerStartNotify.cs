using System;
using System.Collections.Generic;
namespace InetOptimizer
{
    public partial class PKTTriggerStartNotify
    {
        public void SteamDecode(BitReader reader)
        {
            u64list_0 = reader.ReadList<UInt64>();
            ActorId = reader.ReadUInt32();
            TriggerUnitIndex = reader.ReadUInt64();
            Signal = reader.ReadUInt32();
        }
    }
}
