using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace LostArkLogger
{
    internal class StatusEffectTracker
    {
        private readonly ConcurrentDictionary<UInt64, Tuple<ConcurrentDictionary<UInt64, StatusEffect>, ConcurrentDictionary<UInt32, int>>> PartyStatusEffectRegistry;
        private readonly ConcurrentDictionary<UInt64, Tuple<ConcurrentDictionary<UInt64, StatusEffect>, ConcurrentDictionary<UInt32, int>>> EntityStatusEffectRegistry;
        public Parser parser;
        public event Action OnChange;
        public event Action<StatusEffect> OnStatusEffectStarted;
        public event Action<StatusEffect, TimeSpan> OnStatusEffectEnded;
        public StatusEffectTracker(Parser p)
        {
            PartyStatusEffectRegistry = new ConcurrentDictionary<UInt64, Tuple<ConcurrentDictionary<UInt64, StatusEffect>, ConcurrentDictionary<UInt32, int>>>();
            EntityStatusEffectRegistry = new ConcurrentDictionary<UInt64, Tuple<ConcurrentDictionary<UInt64, StatusEffect>, ConcurrentDictionary<UInt32, int>>>();
            parser = p;
            parser.beforeNewZone += BeforeNewZone;
        }

        public void BeforeNewZone()
        {
            // cancel remaining statuseffects so they get added to the old encounter
            foreach(var statusEffectList in PartyStatusEffectRegistry)
            {
                foreach(var statusEffect in statusEffectList.Value.Item1)
                {
                    var duration = (DateTime.UtcNow - statusEffect.Value.Started);
                    OnStatusEffectEnded?.Invoke(statusEffect.Value, duration);
                }
            }
            PartyStatusEffectRegistry.Clear();
        }

        public void InitPc(PKTInitPC packet)
        {
            var statusEffectList = GetStatusEffectList(packet.PlayerId, StatusEffect.StatusEffectType.Local);
            foreach (var statusEffect in packet.statusEffectDatas)
            {
                ProcessStatusEffectData(statusEffect, packet.PlayerId, statusEffect.SourceId, statusEffectList, StatusEffect.StatusEffectType.Local);
            }
            OnChange?.Invoke();
        }

        public void NewNpc(PKTNewNpc packet)
        {
            var statusEffectList = GetStatusEffectList(packet.npcStruct.NpcId, StatusEffect.StatusEffectType.Local);
            foreach (var statusEffect in packet.npcStruct.statusEffectDatas)
            {
                ProcessStatusEffectData(statusEffect, packet.npcStruct.NpcId, statusEffect.SourceId, statusEffectList, StatusEffect.StatusEffectType.Local);
            }
            OnChange?.Invoke();
        }

        public void NewPc(PKTNewPC packet)
        {
            var statusEffectList = GetStatusEffectList(packet.pCStruct.PartyId, StatusEffect.StatusEffectType.Party);
            foreach (var statusEffect in packet.pCStruct.statusEffectDatas)
            {
                ProcessStatusEffectData(statusEffect, packet.pCStruct.PartyId, statusEffect.SourceId, statusEffectList, StatusEffect.StatusEffectType.Party);
            }
            OnChange?.Invoke();
        }

        private void ProcessStatusEffectData(StatusEffectData effectData, UInt64 targetId, UInt64 sourceId, Tuple<ConcurrentDictionary<UInt64, StatusEffect>, ConcurrentDictionary<UInt32, int>> effectList, StatusEffect.StatusEffectType effectType)
        {
            Entity sourceEntity = parser.GetSourceEntity(sourceId);
            var amount = (effectData.hasValue > 0 && effectData.Value != null && effectData.Value.Length == 4) ? BitConverter.ToInt32(effectData.Value, 0) : 0;
            var statusEffect = new StatusEffect { Started = DateTime.UtcNow, StatusEffectId = effectData.StatusEffectId, InstanceId = effectData.EffectInstanceId, SourceId = sourceEntity.EntityId, TargetId = targetId, Type = effectType, Value = amount };
            // end this buf now, it got refreshed
            if (RemoveStatusEffect(effectList, statusEffect.InstanceId, out var oldStatusEffect))
            {
                var duration = DateTime.UtcNow - oldStatusEffect.Started;
                OnStatusEffectEnded?.Invoke(oldStatusEffect, duration);
            }
            if (effectList.Item1.TryAdd(statusEffect.InstanceId, statusEffect))
            {
                if (!effectList.Item2.ContainsKey(statusEffect.StatusEffectId))
                {
                    effectList.Item2.TryAdd(statusEffect.StatusEffectId, 1);
                }
                else
                {
                    effectList.Item2[statusEffect.StatusEffectId] = effectList.Item2[statusEffect.StatusEffectId] + 1;
                }
            }
            OnStatusEffectStarted?.Invoke(statusEffect);
        }

        public void Add(PKTStatusEffectAddNotify effect)
        {
            var statusEffectList = GetStatusEffectList(effect.ObjectId, StatusEffect.StatusEffectType.Local);//this is entityId
            ProcessStatusEffectData(effect.statusEffectData, effect.ObjectId, effect.statusEffectData.SourceId, statusEffectList, StatusEffect.StatusEffectType.Local);
            OnChange?.Invoke();
        }

        public void PartyAdd(PKTPartyStatusEffectAddNotify effect)
        {
            foreach (var statusEffect in effect.statusEffectDatas)
            {

                var applierId = statusEffect.SourceId;
                if (effect.PlayerIdOnRefresh != 0x0)
                {
                    applierId = effect.PlayerIdOnRefresh;
                }
                var statusEffectList = GetStatusEffectList(effect.PartyId, StatusEffect.StatusEffectType.Party);
                ProcessStatusEffectData(statusEffect, effect.PartyId, applierId, statusEffectList, StatusEffect.StatusEffectType.Party);
            }
            OnChange?.Invoke();
        }

        public void PartyRemove(PKTPartyStatusEffectRemoveNotify effect)
        {
            var statusEffectList = GetStatusEffectList(effect.PartyId, StatusEffect.StatusEffectType.Party);
            foreach (var effectInstanceId in effect.StatusEffectIds)
            {
                if (RemoveStatusEffect(statusEffectList, effectInstanceId, out var oldStatusEffect))
                {
                    var duration = DateTime.UtcNow - oldStatusEffect.Started;
                    OnStatusEffectEnded?.Invoke(oldStatusEffect, duration);
                }
            }
            OnChange?.Invoke();
        }

        public void Remove(PKTStatusEffectRemoveNotify effect)
        {
            var statusEffectList = GetStatusEffectList(effect.ObjectId, StatusEffect.StatusEffectType.Local);
            foreach (var effectInstanceId in effect.InstanceIds)
            {
                if (RemoveStatusEffect(statusEffectList, effectInstanceId, out var oldStatusEffect))
                {
                    var duration = DateTime.UtcNow - oldStatusEffect.Started;
                    OnStatusEffectEnded?.Invoke(oldStatusEffect, duration);
                }
            }
            OnChange?.Invoke();
        }

        private static bool RemoveStatusEffect(Tuple<ConcurrentDictionary<UInt64, StatusEffect>, ConcurrentDictionary<UInt32, int>> effectList, ulong effectInstanceId, out StatusEffect oldStatusEffect)
        {
            if(effectList.Item1.TryRemove(effectInstanceId, out oldStatusEffect))
            {
                if (effectList.Item2.TryGetValue(oldStatusEffect.StatusEffectId, out var count))
                {
                    if (count > 1)
                    {
                        effectList.Item2[oldStatusEffect.StatusEffectId] = (count - 1);
                    }
                    else
                    {
                        effectList.Item2.TryRemove(oldStatusEffect.StatusEffectId, out _);
                    }
                }
                return true;
            }
            return false;
        }

        public void DeathNotify(PKTDeathNotify packet)
        {
            if(EntityStatusEffectRegistry.TryRemove(packet.TargetId, out var statusEffectList))
            {
                foreach (var statusEffect in statusEffectList.Item1)
                {
                    var oldStatusEffect = statusEffect.Value;
                    var duration = DateTime.UtcNow - oldStatusEffect.Started;
                    OnStatusEffectEnded?.Invoke(oldStatusEffect, duration);
                }
            }
            OnChange?.Invoke();
        }

        public int GetStatusEffectCountByEntityId(UInt64 EntityId)
        {
            return GetStatusEffectList(EntityId, StatusEffect.StatusEffectType.Local).Item1.Count;
        }

        public int GetStatusEffectCountByCharacterId(UInt64 CharacterId)
        {
            return GetStatusEffectList(CharacterId, StatusEffect.StatusEffectType.Party).Item1.Count;
        }

        private Tuple<ConcurrentDictionary<UInt64, StatusEffect>, ConcurrentDictionary<UInt32, int>> GetStatusEffectList(UInt64 targetId, StatusEffect.StatusEffectType effectType)
        {
            Tuple<ConcurrentDictionary<UInt64, StatusEffect>, ConcurrentDictionary<UInt32, int>> statusEffectList;
            if (effectType == StatusEffect.StatusEffectType.Local)
            {
                if (!EntityStatusEffectRegistry.TryGetValue(targetId, out statusEffectList))
                {
                    statusEffectList = Tuple.Create(new ConcurrentDictionary<UInt64, StatusEffect>(), new ConcurrentDictionary<UInt32, int>());
                    EntityStatusEffectRegistry.TryAdd(targetId, statusEffectList);
                }
            }
            else
            {
                if (!PartyStatusEffectRegistry.TryGetValue(targetId, out statusEffectList))
                {
                    statusEffectList = Tuple.Create(new ConcurrentDictionary<UInt64, StatusEffect>(), new ConcurrentDictionary<UInt32, int>());
                    PartyStatusEffectRegistry.TryAdd(targetId, statusEffectList);
                }
            }
            return statusEffectList;
        }

        public bool EntityHasAnyStatusEffectFromParty(UInt64 targetId, UInt32 partyId, params UInt32[] statusEffectIds)
        {
            var statusEffectList = GetStatusEffectList(targetId, StatusEffect.StatusEffectType.Local);
            foreach (var effectId in statusEffectIds)
            {
                if (statusEffectList.Item2.ContainsKey(effectId))
                {
                    foreach(var statusEffectEntry in statusEffectList.Item1)
                    {
                        if (effectId != statusEffectEntry.Value.StatusEffectId)
                            continue;
                        if (PartyTracker.Instance.IsEntityIdInParty(statusEffectEntry.Value.SourceId))
                        {
                            var sourcePartyId = PartyTracker.Instance.GetPartyIdFromEntityId(statusEffectEntry.Value.SourceId);
                            return sourcePartyId == partyId;
                        }
                    }
                }
            }
            return false;
        }

        public bool EntityHasAnyStatusEffect(UInt64 targetId, params UInt32[] statusEffectIds)
        {
            var statusEffectList = GetStatusEffectList(targetId, StatusEffect.StatusEffectType.Local);
            foreach (var effectId in statusEffectIds)
            {
                if (statusEffectList.Item2.ContainsKey(effectId))
                {
                    return true;
                }
            }
            return false;
        }

        public bool PartyMemberHasAnyStatusEffect(UInt64 characterId, params UInt32[] statusEffectIds)
        {
            var statusEffectList = GetStatusEffectList(characterId, StatusEffect.StatusEffectType.Party);
            foreach (var effectId in statusEffectIds)
            {
                if (statusEffectList.Item2.ContainsKey(effectId))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
