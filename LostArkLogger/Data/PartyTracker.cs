using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostArkLogger
{
    internal class PartyTracker
    {
        private static readonly PartyTracker instance = new PartyTracker();
        private Dictionary<UInt64, UInt32> CharacterIdToPartyId = new Dictionary<UInt64, UInt32>();
        private Dictionary<UInt64, UInt64> EntityIdToCharacterId = new Dictionary<UInt64, UInt64>();
        private Dictionary<UInt64, UInt64> CharacterIdToEntityId = new Dictionary<UInt64, UInt64>();
        private Dictionary<UInt64, UInt32> EntityIdToPartyId = new Dictionary<UInt64, UInt32>();
        private Dictionary<UInt32, PartyInfo> PartyInformations = new Dictionary<UInt32, PartyInfo>();

        private PartyTracker() { }

        public static PartyTracker Instance
        {
            get { return instance; }
        }

        public void ProcessPartyPKT(PKTPartyInfo pkt)
        {
            foreach(var x in pkt.MemberDatas)
            {
                CharacterIdToPartyId[x.CharacterId] = pkt.PartyInstanceId;
                if (CharacterIdToEntityId.ContainsKey(x.CharacterId))
                    EntityIdToPartyId[CharacterIdToEntityId[x.CharacterId]] = pkt.PartyInstanceId;
            }
            PartyInformations[pkt.PartyInstanceId] = new PartyInfo(pkt);
        }

        public void ProcessPKTNewPC(PKTNewPC pkt)
        {
            EntityIdToCharacterId[pkt.pCStruct.PlayerId] = pkt.pCStruct.PartyId;
            CharacterIdToEntityId[pkt.pCStruct.PartyId] = pkt.pCStruct.PlayerId;
            if (CharacterIdToPartyId.ContainsKey(pkt.pCStruct.PartyId))
                EntityIdToPartyId[pkt.pCStruct.PlayerId] = CharacterIdToPartyId[pkt.pCStruct.PartyId];
        }

        public bool IsCharacterIdInParty(UInt64 characterId)
        {
            return CharacterIdToPartyId.ContainsKey(characterId);
        }

        public bool IsEntityIdInParty(UInt64 entityId)
        {
            return EntityIdToPartyId.ContainsKey(entityId);
        }

        public UInt32 GetPartyIdFromCharacterId(UInt64 characterId)
        {

            return CharacterIdToPartyId[characterId];
        }

        public UInt32 GetPartyIdFromEntityId(UInt64 EntityId)
        {
            return EntityIdToPartyId[EntityId];
        }
    }
}
