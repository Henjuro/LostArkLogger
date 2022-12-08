using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace InetOptimizer
{
    internal class PCIdMapper
    {
        private static readonly PCIdMapper instance = new();

        private readonly ConcurrentDictionary<UInt64, UInt64> EntityIdToCharacterIdMap;
        private readonly ConcurrentDictionary<UInt64, UInt64> CharacterIdToEntityIdMap;
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

        public bool TryGetEntityIdFormCharacterId(UInt64 characterId, out UInt64 entityId)
        {
            return CharacterIdToEntityIdMap.TryGetValue(characterId, out entityId);
        }

        public bool TryGetCharacterIdFromEntityId(UInt64 entityId, out UInt64 characterId)
        {
            return EntityIdToCharacterIdMap.TryGetValue(entityId, out characterId);
        }

        public bool ContainsEntityIdMapping(UInt64 entityId)
        {
            return EntityIdToCharacterIdMap.ContainsKey(entityId);
        }

        public bool ContainsCharacterIdMapping(UInt64 characterId)
        {
            return CharacterIdToEntityIdMap.ContainsKey(characterId);
        }

        public void AddCharacterIdAndEntityIdMapping(UInt64 characterId, UInt64 entityId)
        {
            EntityIdToCharacterIdMap.TryAdd(entityId, characterId);
            CharacterIdToEntityIdMap.TryAdd(characterId, entityId);
        }

        /**
         * Clear all EntityId to CharacterId and CharacterId to EntityId mappings
         **/
        public void Clear()
        {
            EntityIdToCharacterIdMap.Clear();
            CharacterIdToEntityIdMap.Clear();
        }
    }
}
