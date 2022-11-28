﻿using System;
using System.Collections.Generic;
namespace InetOptimizer
{
    public partial class PKTPartyInfo
    {
        public void SteamDecode(BitReader reader)
        {
            b_0 = reader.ReadByte();
            PartyInstanceId = reader.ReadUInt32();
            u32_1 = reader.ReadUInt32();
            u32_2 = reader.ReadUInt32();
            MemberDatas = reader.ReadList<PartyMemberData>();
            b_1 = reader.ReadByte();

        }
    }
}
