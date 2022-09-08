using System;
using System.Collections.Generic;
namespace InetOptimizer
{
    public partial class PKTStatusEffectRemoveNotify
    {
        public void SteamDecode(BitReader reader)
        {
            ObjectId = reader.ReadUInt64();
            InstanceIds = reader.ReadList<UInt32>();
            Reason = reader.ReadByte();
        }
    }
}
