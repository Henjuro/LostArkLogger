using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InetOptimizer
{
    internal class PartyTracker
    {
        private static readonly PartyTracker instance = new();
        private Dictionary<UInt64, UInt32> CharacterIdToPartyId = new();
        private Dictionary<UInt64, UInt32> EntityIdToPartyId = new();
        private Dictionary<UInt64, UInt64> EntityIdToCharacterId = new();
        private Dictionary<UInt64, UInt64> CharacterIdToEntityId = new();
        private Dictionary<UInt32, PartyInfo> PartyInformations = new();
        private String ownCharacterName = "";

        private PartyTracker() { }

        public static PartyTracker Instance
        {
            get { return instance; }
        }

        public void ProcessPKTPartyInfo(PKTPartyInfo pkt)
        {
            RemovePartyMappings(pkt.PartyInstanceId);

            // you are the only one left so party quit
            if (pkt.MemberDatas.Count <= 1)
                return;

            foreach (var x in pkt.MemberDatas)
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

        public void ProcessPKTInitPC(PKTInitPC pkt)
        {
            EntityIdToCharacterId[pkt.PlayerId] = pkt.CharacterId;
            CharacterIdToEntityId[pkt.CharacterId] = pkt.PlayerId;
            ownCharacterName = pkt.Name;
            if (CharacterIdToPartyId.ContainsKey(pkt.CharacterId))
                EntityIdToPartyId[pkt.PlayerId] = CharacterIdToPartyId[pkt.CharacterId];
        }
        public void ProcessPKTInitEnv(PKTInitEnv pkt, UInt64 localCharacterId)
        {
            EntityIdToCharacterId.Clear();
            CharacterIdToEntityId.Clear();
            EntityIdToCharacterId[pkt.PlayerId] = localCharacterId;
            CharacterIdToEntityId[localCharacterId] = pkt.PlayerId;
            if (CharacterIdToPartyId.ContainsKey(localCharacterId))
                EntityIdToPartyId[pkt.PlayerId] = CharacterIdToPartyId[localCharacterId];
        }

        public void ProcessPKTPartyUnknown(PKTPartyUnknown pkt)
        {
            CharacterIdToPartyId[pkt.CharacterId] = pkt.PartyInstanceId;
            if (CharacterIdToEntityId.ContainsKey(pkt.CharacterId))
                EntityIdToPartyId[CharacterIdToEntityId[pkt.CharacterId]] = pkt.PartyInstanceId;
            if (!PartyInformations.ContainsKey(pkt.PartyInstanceId))
                PartyInformations[pkt.PartyInstanceId] = new PartyInfo(pkt);
        }

        public void ProcessPKTPartyLeaveResult(PKTPartyLeaveResult pkt)
        {

            if (pkt.Name.Equals(ownCharacterName))
            {
                PartyInfo pi = PartyInformations[pkt.PartyInstanceId];
                List<PartyInfo> parties = new List<PartyInfo>();
                foreach (var p in PartyInformations)
                {
                    if (p.Value.RaidId == pi.RaidId)
                        parties.Add(p.Value);
                }
                foreach (var p in parties)
                    RemovePartyMappings(p.PartyId);
            }
        }

        private void RemovePartyMappings(UInt32 partyInstanceId)
        {
            // remove old entries
            List<UInt64> removals = new List<UInt64>();
            foreach (var item in CharacterIdToPartyId)
            {
                if (item.Value == partyInstanceId)
                    removals.Add(item.Key);
            }
            foreach (var item in removals)
                CharacterIdToPartyId.Remove(item);
            removals.Clear();
            foreach (var item in EntityIdToPartyId)
            {
                if (item.Value == partyInstanceId)
                    removals.Add(item.Key);
            }
            foreach (var item in removals)
                EntityIdToPartyId.Remove(item);
            removals.Clear();
            PartyInformations.Remove(partyInstanceId);
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
