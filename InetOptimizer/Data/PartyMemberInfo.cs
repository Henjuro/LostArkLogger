using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InetOptimizer
{
    internal class PartyMemberInfo
    {
        public UInt64 CharacterId;
        public Int64 MaxHP;
        public byte PartyMemberNumber;
        public string Name;

        public PartyMemberInfo(PartyMemberData pmd)
        {
            CharacterId = pmd.CharacterId;
            MaxHP = pmd.MaxHP.Value;
            PartyMemberNumber = pmd.PartyMemberNumber;
            Name = pmd.Name.Value;
        }
    }
}
