using System;
using System.Collections.Generic;
namespace InetOptimizer
{
    public partial class PKTStatusEffectRemoveNotify
    {
        public void SteamDecode(BitReader reader)
        {
            InstanceIds = reader.ReadList<UInt32>();
            ObjectId = reader.ReadUInt64();
            Reason = reader.ReadByte();
        }
    }
}
