using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostArkLogger
{
    internal class BuffTracker
    {
        private readonly ConcurrentDictionary<UInt64, ConcurrentDictionary<Buff, byte>> BuffMap;
        public Parser parser;
        public event Action OnChange;
        public BuffTracker()
        {
            BuffMap = new ConcurrentDictionary<UInt64, ConcurrentDictionary<Buff, byte>>();
        }

        public void SetParser(Parser p)
        {
            parser = p;
            parser.onNewZone += OnNewZone;
        }

        public void OnNewZone()
        {
            BuffMap.Clear();
        }

        public void Add(PKTStatusEffectAddNotify effect)
        {
            ConcurrentDictionary<Buff, byte> buffList = GetBuffList(effect.TargetId);

            var buff = new Buff { BuffId = effect.BuffId, InstanceId = effect.InstanceId, SourceId = effect.SourceId, TargetId = effect.TargetId, Type = Buff.BuffType.Local };
            buffList.TryAdd(buff, 0x0);
            if (!parser.currentEncounter.Entities.ContainsKey(effect.TargetId))
            {
                Console.WriteLine("Could not find Entity with ID " + BitConverter.ToString(BitConverter.GetBytes(effect.TargetId)) + " for adding statuseffect");
            }
            Console.WriteLine("Buff Added " + buff.ToString());
            OnChange?.Invoke();
        }

        public void Add(PKTPartyStatusEffectAddNotify effect)
        {
            ConcurrentDictionary<Buff, byte> buffList = GetBuffList(effect.TargetPlayerPartyId);
            var buff = new Buff { BuffId = effect.BuffId, InstanceId = effect.InstanceId, SourceId = effect.SourceId, TargetId = effect.TargetPlayerPartyId, Type = Buff.BuffType.Party };
            buffList.TryAdd(buff, 0x0);
            if (!parser.currentEncounter.PartyEntities.ContainsKey(effect.TargetPlayerPartyId))
            {
                Console.WriteLine("Could not find Party Entity with ID " + BitConverter.ToString(BitConverter.GetBytes(effect.TargetPlayerPartyId)) + " for adding statuseffect");
            }
            Console.WriteLine("Party Buff Added " + buff.ToString());
            OnChange?.Invoke();
        }

        public void Remove(PKTPartyStatusEffectRemoveNotify effect)
        {
            ConcurrentDictionary<Buff, byte> buffList = GetBuffList(effect.PlayerPartyId);
            foreach (var effectInstanceId in effect.BuffInstanceIds)
            {
                if (buffList.TryRemove(new Buff { InstanceId = effectInstanceId }, out _))
                {
                    Console.WriteLine("Party Buff removed " + effectInstanceId.ToString());
                }
                else
                {
                    Console.WriteLine("Party Buff NOT removed " + effectInstanceId.ToString());
                }
            }
            OnChange?.Invoke();
        }

        public void Remove(PKTStatusEffectRemoveNotify effect)
        {
            ConcurrentDictionary<Buff, byte> buffList = GetBuffList(effect.TargetId);
            foreach (var effectInstanceId in effect.BuffInstanceIds)
            {
                if (buffList.TryRemove(new Buff { InstanceId = effectInstanceId }, out _))
                {
                    Console.WriteLine("Buff removed " + effectInstanceId.ToString());
                }
                else
                {
                    Console.WriteLine("Buff NOT removed " + effectInstanceId.ToString());
                }
            }
            OnChange?.Invoke();
        }

        public void EntityDied(PKTDeathNotify paket)
        {
            BuffMap.TryRemove(paket.TargetId, out _);
            OnChange?.Invoke();
        }

        public int GetBuffCountFor(UInt64 PlayerId)
        {
            return GetBuffList(PlayerId).Count;
        }

        public ConcurrentDictionary<UInt64, ConcurrentDictionary<Buff, byte>> GetBuffMap()
        {
            return BuffMap;
        }


        private ConcurrentDictionary<Buff, byte> GetBuffList(UInt64 targetId)
        {
            if (!BuffMap.TryGetValue(targetId, out ConcurrentDictionary<Buff, byte> buffList))
            {
                buffList = new ConcurrentDictionary<Buff, byte>();
                BuffMap.TryAdd(targetId, buffList);
            }
            return buffList;
        }

    }
}
