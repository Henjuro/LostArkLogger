using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InetOptimizer
{
    internal class PartyInfo {
    
        public UInt32 PartyId;
        public UInt32 RaidId;
        private readonly Dictionary<UInt64, PartyMemberInfo> Members;
        public PartyInfo(PKTPartyInfo pkt)
        {
            Members = new();
            PartyId = pkt.PartyInstanceId;
            RaidId = pkt.RaidInstanceId;
            foreach(var member in pkt.MemberDatas)
            {
                Members.Add(member.CharacterId, new PartyMemberInfo(member));
            }
        }

        public PartyInfo(PKTPartyUnknown pkt)
        {
            Members = new();
            PartyId = pkt.PartyInstanceId;
            RaidId = pkt.RaidInstanceId;
        }

        public bool HasMemeber(UInt64 characterId)
        {
            return Members.ContainsKey(characterId);
        }

        public PartyMemberInfo GetMemberInfo(UInt64 characterId)
        {
            return Members[characterId];
        }
    }
}
