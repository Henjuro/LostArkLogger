using System;
using System.Collections.Generic;
namespace InetOptimizer
{
    public partial class PKTPartyStatusEffectRemoveNotify
    {
        public void SteamDecode(BitReader reader)
        {
            b_0 = reader.ReadByte();
            StatusEffectIds = reader.ReadList<UInt32>();
            PartyId = reader.ReadUInt64();
        }
    }
}
