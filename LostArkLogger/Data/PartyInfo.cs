using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostArkLogger
{
    internal class PartyInfo {
    
        public UInt32 PartyId;
        private readonly Dictionary<UInt64, PartyMemberInfo> Members;
        public PartyInfo(PKTPartyInfo pkt)
        {
            Members = new Dictionary<ulong, PartyMemberInfo>();
            PartyId = pkt.PartyInstanceId;
            foreach(var member in pkt.MemberDatas)
            {
                Members.Add(member.CharacterId, new PartyMemberInfo(member));
            }
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
