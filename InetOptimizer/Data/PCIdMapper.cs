using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace InetOptimizer
{
    internal class PCIdMapper
    {
        private static readonly PCIdMapper instance = new();

        private ConcurrentDictionary<UInt64, UInt64> EntityIdToCharacterIdMap;
        private ConcurrentDictionary<UInt64, UInt64> CharacterIdToEntityIdMap;
        private PCIdMapper(){
            EntityIdToCharacterIdMap = new();
            CharacterIdToEntityIdMap = new();
        }

        public static PCIdMapper Instance
        {
            get
            {
                return instance;
            }
        }

        public UInt64 GetEntityIdFormCharacterId(UInt64 characterId)
        {
            if (CharacterIdToEntityIdMap.TryGetValue(characterId, out UInt64 entityId))
            {
                return entityId;
            }
            throw new KeyNotFoundException();
        }
        public UInt64 GetCharacterIdFormEntityId(UInt64 entityId)
        {
            if (EntityIdToCharacterIdMap.TryGetValue(entityId, out UInt64 characterId))
            {
                return characterId;
            }
            throw new KeyNotFoundException();
        }

        public void AddCharacterIdAndEntityIdMapping(UInt64 characterId, UInt64 entityId)
        {
            EntityIdToCharacterIdMap.TryAdd(entityId, characterId);
            CharacterIdToEntityIdMap.TryAdd(characterId, entityId);
        }
    }
}
