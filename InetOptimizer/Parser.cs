﻿using K4os.Compression.LZ4;
using InetOptimizer.Utilities;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using IronSnappy;
using System.Buffers.Text;
using System.IO;
using System.Reflection;
using System.Collections;

namespace InetOptimizer
{
    internal class Parser : IDisposable
    {
#pragma warning disable CA2101 // Specify marshaling for P/Invoke string arguments
        [DllImport("wpcap.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)] static extern IntPtr pcap_strerror(int err);
#pragma warning restore CA2101 // Specify marshaling for P/Invoke string arguments
        Machina.TCPNetworkMonitor tcp;
        ILiveDevice pcap;
        public event Action<LogInfo> onCombatEvent;
        public event Action onNewZone;
        public event Action beforeNewZone;
        public event Action<int> onPacketTotalCount;
        public bool use_npcap = false;
        protected object lockPacketProcessing = new object(); // needed to synchronize UI swapping devices
        public Machina.Infrastructure.NetworkMonitorType? monitorType = null;
        public List<Encounter> Encounters = new List<Encounter>();
        public Encounter currentEncounter = new Encounter();
        Byte[] fragmentedPacket = new Byte[0];
        private string _localPlayerName = "You";
        private uint _localGearLevel = 0;
        private ulong _localEntityId = 0;
        private ulong _localCharacterId;
        public bool WasWipe = false;
        public bool WasKill = false;
        public bool DisplayNames = true;
        public StatusEffectTracker statusEffectTracker;

        public Parser()
        {
            Encounters.Add(currentEncounter);
            onCombatEvent += Parser_onDamageEvent;
            onNewZone += Parser_onNewZone;
            statusEffectTracker = new StatusEffectTracker(this);
            statusEffectTracker.OnStatusEffectEnded += Parser_onStatusEffectEnded;
            statusEffectTracker.OnStatusEffectStarted += StatusEffectTracker_OnStatusEffectStarted; ;
            InstallListener();
        }

        // UI needs to be able to ask us to reload our listener based on the current user settings
        virtual public void InstallListener()
        {
            lock (lockPacketProcessing)
            {
                // If we have an installed listener, that needs to go away or we duplicate traffic
                UninstallListeners();

                // Reset all state related to current packet processing here that won't be valid when creating a new listener.
                fragmentedPacket = new Byte[0];

                // We default to using npcap, but the UI can also set this to false.
                if (use_npcap)
                {
                    monitorType = Machina.Infrastructure.NetworkMonitorType.WinPCap;
                    string filter = "ip and tcp port 6040";
                    bool foundAdapter = false;
                    NetworkInterface gameInterface;
                    // listening on every device results in duplicate traffic, unfortunately, so we'll find the adapter used by the game here
                    try
                    {
                        pcap_strerror(1); // verify winpcap works at all
                        gameInterface = NetworkUtil.GetAdapterUsedByProcess("LostArk");
                        foreach (var device in CaptureDeviceList.Instance)
                        {
                            if (device.MacAddress == null) continue; // SharpPcap.IPCapDevice.MacAddress is null in some cases
                            if (gameInterface.GetPhysicalAddress().ToString() == device.MacAddress.ToString())
                            {
                                try
                                {
                                    device.Open(DeviceModes.None, 1000); // todo: 1sec timeout ok?
                                    device.Filter = filter;
                                    device.OnPacketArrival += new PacketArrivalEventHandler(Device_OnPacketArrival_pcap);
                                    device.StartCapture();
                                    pcap = device;
                                    foundAdapter = true;
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    var exceptionMessage = "Exception while trying to listen to NIC " + device.Name + "\n" + ex.ToString();
                                    Console.WriteLine(exceptionMessage);
                                    Logger.AppendLog(254, exceptionMessage);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        var exceptionMessage = "Sharppcap init failed, using rawsockets instead, exception:\n" + ex.ToString();
                        Console.WriteLine(exceptionMessage);
                        Logger.AppendLog(254, exceptionMessage);
                    }
                    // If we failed to find a pcap device, fall back to rawsockets.
                    if (!foundAdapter)
                    {
                        use_npcap = false;
                        pcap = null;
                    }
                }

                if (use_npcap == false)
                {
                    // Always fall back to rawsockets
                    tcp = new Machina.TCPNetworkMonitor();
                    tcp.Config.WindowClass = "EFLaunchUnrealUWindowsClient";
                    monitorType = tcp.Config.MonitorType = Machina.Infrastructure.NetworkMonitorType.RawSocket;
                    tcp.DataReceivedEventHandler += (Machina.Infrastructure.TCPConnection connection, byte[] data) => Device_OnPacketArrival_machina(connection, data);
                    tcp.Start();
                }
            }
        }

        void ProcessDamageEvent(Entity sourceEntity, UInt32 skillId, UInt32 skillEffectId, SkillDamageEvent dmgEvent)
        {
            if (dmgEvent.MaxHp.Value > 3000000) {
                int perc = (int)(dmgEvent.CurHp.Value / ((double)dmgEvent.MaxHp.Value) * 100);
                string name = currentEncounter.Entities.GetOrAdd(dmgEvent.TargetId).VisibleName;
                if (perc <= 0)
                    currentEncounter.BigNPCHealthMap.Remove(name, out _);
                else
                {
                    var data = new Tuple<DateTime, int, long, long>(DateTime.UtcNow, perc, dmgEvent.MaxHp.Value, dmgEvent.CurHp.Value);
                    currentEncounter.BigNPCHealthMap.AddOrUpdate(name, data, (k, v) => data);
                }
            }
            var hitFlag = (HitFlag)(dmgEvent.Modifier & 0xf);
            if (hitFlag == HitFlag.HIT_FLAG_DAMAGE_SHARE && skillId == 0 && skillEffectId == 0)
                return;

            // damage dealer is a player
            if (!String.IsNullOrEmpty(sourceEntity.ClassName) && sourceEntity.ClassName != "UnknownClass")
            {
                // player hasn't been announced on logs before. possibly because user opened logger after they got into a zone
                if (!currentEncounter.LoggedEntities.ContainsKey(sourceEntity.EntityId))
                {
                    // classId is unknown, can be fixed
                    // level, currenthp and maxhp is unknown
                    Logger.AppendLog(3, sourceEntity.EntityId.ToString("X"), sourceEntity.Name, "0", sourceEntity.ClassName, "1", "0", "0");
                    currentEncounter.LoggedEntities.TryAdd(sourceEntity.EntityId, true);
                }
            }

            var hitOption = (HitOption)(((dmgEvent.Modifier >> 4) & 0x7) - 1);
            var skillName = Skill.GetSkillName(skillId, skillEffectId);
            var targetEntity = currentEncounter.Entities.GetOrAdd(dmgEvent.TargetId);
            var destinationName = targetEntity != null ? targetEntity.VisibleName : dmgEvent.TargetId.ToString("X");
            //var log = new LogInfo { Time = DateTime.Now, Source = sourceName, PC = sourceName.Contains("("), Destination = destinationName, SkillName = skillName, Crit = (dmgEvent.FlagsMaybe & 0x81) > 0, BackAttack = (dmgEvent.FlagsMaybe & 0x10) > 0, FrontAttack = (dmgEvent.FlagsMaybe & 0x20) > 0 };
            // 211601 heavenly tune
            bool isInParty = PartyTracker.Instance.IsCharacterIdInParty(sourceEntity.PartyId);
            bool attackbuffActive = false;
            bool supportDebuffActive = false;
            if (isInParty) {
                attackbuffActive = sourceEntity.EntityId == _localEntityId ? statusEffectTracker.EntityHasAnyStatusEffect(sourceEntity.EntityId, 211606, 211749, 361708, 360506) : statusEffectTracker.PartyMemberHasAnyStatusEffect(sourceEntity.PartyId, 211606, 211749, 361708, 360506);
                supportDebuffActive = statusEffectTracker.EntityHasAnyStatusEffectFromParty(targetEntity.EntityId, PartyTracker.Instance.GetPartyIdFromCharacterId(sourceEntity.PartyId), 210230, 360506);
            }
            else
            {
                attackbuffActive = sourceEntity.EntityId == _localEntityId ? statusEffectTracker.EntityHasAnyStatusEffect(sourceEntity.EntityId, 211606, 211749, 361708, 360506) : false;
                supportDebuffActive = statusEffectTracker.EntityHasAnyStatusEffect(targetEntity.EntityId, 210230, 360506);
            }
            var log = new LogInfo
            {
                Time = DateTime.Now,
                SourceEntity = sourceEntity,
                DestinationEntity = targetEntity,
                SkillId = skillId,
                SkillEffectId = skillEffectId,
                SkillName = skillName,
                Damage = (ulong)dmgEvent.Damage.Value,
                Crit = hitFlag == HitFlag.HIT_FLAG_CRITICAL || hitFlag == HitFlag.HIT_FLAG_DOT_CRITICAL,
                BackAttack = hitOption == HitOption.HIT_OPTION_BACK_ATTACK,
                FrontAttack = hitOption == HitOption.HIT_OPTION_FRONTAL_ATTACK,
                AttackBuff = attackbuffActive,
                DamageDebuff = supportDebuffActive
            };
            onCombatEvent?.Invoke(log);
            currentEncounter.RaidInfos.Add(log);
            Logger.AppendLog(8, sourceEntity.EntityId.ToString("X"), sourceEntity.Name, skillId.ToString(), Skill.GetSkillName(skillId), skillEffectId.ToString(), Skill.GetSkillEffectName(skillEffectId), targetEntity.EntityId.ToString("X"), targetEntity.Name, dmgEvent.Damage.ToString(), dmgEvent.Modifier.ToString("X"), dmgEvent.CurHp.ToString(), dmgEvent.MaxHp.ToString());
        }
        void ProcessSkillDamage(PKTSkillDamageNotify damage)
        {
            var sourceEntity = GetSourceEntity(damage.SourceId);
            var className = Skill.GetClassFromSkill(damage.SkillId);
            if (String.IsNullOrEmpty(sourceEntity.ClassName) && className != "UnknownClass")
            {
                sourceEntity.Type = Entity.EntityType.Player;
                sourceEntity.ClassName = className; // for case where we don't know user's class yet            
            }

            if (String.IsNullOrEmpty(sourceEntity.Name)) sourceEntity.Name = damage.SourceId.ToString("X");
            foreach (var dmgEvent in damage.SkillDamageEvents.Events)
                ProcessDamageEvent(sourceEntity, damage.SkillId, damage.SkillEffectId, dmgEvent);
        }

        void ProcessSkillDamage(PKTSkillDamageAbnormalMoveNotify damage)
        {
            var sourceEntity = GetSourceEntity(damage.SourceId);
            var className = Skill.GetClassFromSkill(damage.SkillId);
            if (String.IsNullOrEmpty(sourceEntity.ClassName) && className != "UnknownClass")
            {
                sourceEntity.Type = Entity.EntityType.Player;
                sourceEntity.ClassName = className; // for case where we don't know user's class yet            
            }

            if (String.IsNullOrEmpty(sourceEntity.Name)) sourceEntity.Name = damage.SourceId.ToString("X");
            foreach (var dmgEvent in damage.SkillDamageAbnormalMoveEvents.Events)
                ProcessDamageEvent(sourceEntity, damage.SkillId, damage.SkillEffectId, dmgEvent.DamageEvent);
        }

        protected OpCodes GetOpCode(Byte[] packets)
        {
            var opcodeVal = BitConverter.ToUInt16(packets, 2);
            var opCodeString = "";
            if (Properties.Settings.Default.Region == Region.Steam) opCodeString = ((OpCodes_Steam)opcodeVal).ToString();
            //if (Properties.Settings.Default.Region == Region.Russia) opCodeString = ((OpCodes_ru)opcodeVal).ToString();
            if (Properties.Settings.Default.Region == Region.Korea) opCodeString = ((OpCodes_Korea)opcodeVal).ToString();
            return (OpCodes)Enum.Parse(typeof(OpCodes), opCodeString);
        }
        Byte[] XorTableSteam = Convert.FromBase64String("DgZIHKcHjzVicTApEJcqhC+gJuv5g3PkLCf8Lal73VYgnLKAOs3naIrwmZMaXmPgxDZHSSJglln0HUXWdmaFF5gC3Au30+UZIQ+HuFJTfTu+d3r4EhhfKEKQ3mujoa3Hq2cuxQzjaiSdwzEr/goBtEDVOcAz8kOfWhYFzIFlnkG6T29GTO+JjdHpTjf6fPvxpZJYctu7vKhKRBvZOOyCynjOrG3hpO31royR/SPzoj3IcFGmvcuOA9h1BLZs9+KGHh/UvxX/te5+r88lCN/Jwohu9lez6kvBAGkNmuaxuT8T6FuVMlSbsFA+xmFdXNo8eQnSf3Rk1xSUqjRNEYtV0A==");
        //Byte[] XorTableRu = ObjectSerialize.Decompress(Properties.Resources.xor_ru);
        Byte[] XorTableKorea = ObjectSerialize.Decompress(Properties.Resources.xor_Korea);
        Byte[] XorTable { get { return Properties.Settings.Default.Region == Region.Steam ? XorTableSteam : XorTableKorea; } }
        protected void ProcessPacket(List<Byte> data)
        {
            var packets = data.ToArray();
            var packetWithTimestamp = BitConverter.GetBytes(DateTime.UtcNow.ToBinary()).ToArray().Concat(data);
            onPacketTotalCount?.Invoke(loggedPacketCount++);
            while (packets.Length > 0)
            {
                if (fragmentedPacket.Length > 0)
                {
                    packets = fragmentedPacket.Concat(packets).ToArray();
                    fragmentedPacket = new Byte[0];
                }
                if (6 > packets.Length)
                {
                    fragmentedPacket = packets.ToArray();
                    return;
                }
                var opcode = GetOpCode(packets);
                //Console.WriteLine(opcode);
                var packetSize = BitConverter.ToUInt16(packets.ToArray(), 0);
                if (packets[5] != 1 || 6 > packets.Length || packetSize < 7)
                {
                    // not sure when this happens
                    fragmentedPacket = new Byte[0];
                    return;
                }
                if (packetSize > packets.Length)
                {
                    fragmentedPacket = packets;
                    return;
                }
                var payload = packets.Skip(6).Take(packetSize - 6).ToArray();
                Xor.Cipher(payload, BitConverter.ToUInt16(packets, 2), XorTable);
                switch (packets[4])
                {
                    case 0: //None
                        payload = payload.Skip(16).ToArray();
                        break;
                    case 1: //LZ4
                        var buffer = new byte[0x11ff2];
                        var result = LZ4Codec.Decode(payload, 0, payload.Length, buffer, 0, 0x11ff2);
                        if (result < 1) throw new Exception("LZ4 output buffer too small");
                        payload = buffer.Take(result).Skip(16).ToArray();
                        break;
                    case 2: //Snappy
                        //https://github.com/aloneguid/IronSnappy
                        try
                        {
                            payload = IronSnappy.Snappy.Decode(payload.ToArray()).Skip(16).ToArray();
                        }
                        catch (IOException)
                        {
                            if (packets.Length < packetSize) throw new Exception("bad packet maybe");
                            packets = packets.Skip(packetSize).ToArray();
                            return;
                        }
                        //payload = SnappyCodec.Uncompress(payload.Skip(Properties.Settings.Default.Region == Region.Russia ? 4 : 0).ToArray()).Skip(16).ToArray();
                        break;
                    case 3: //Oodle
                        payload = Oodle.Decompress(payload).Skip(16).ToArray();
                        break;
                    default:
                        payload = payload.Skip(16).ToArray();
                        break;
                }

                // write packets for analyzing, bypass common, useless packets
                // if (opcode != OpCodes.PKTMoveError && opcode != OpCodes.PKTMoveNotify && opcode != OpCodes.PKTMoveNotifyList && opcode != OpCodes.PKTTransitStateNotify && opcode != OpCodes.PKTPing && opcode != OpCodes.PKTPong)
                //    Console.WriteLine(opcode + " : " + opcode.ToString("X") + " : " + BitConverter.ToString(payload));

                /* Uncomment for auction house accessory sniffing
                if (opcode == OpCodes.PKTAuctionSearchResult)
                {
                    var pc = new PKTAuctionSearchResult(payload);
                    Console.WriteLine("NumItems=" + pc.NumItems.ToString());
                    Console.WriteLine("Id, Stat1, Stat2, Engraving1, Engraving2, Engraving3");
                    foreach (var item in pc.Items)
                    {
                        Console.WriteLine(item.ToString());
                    }
                }
                */
                //A67B63A
                if (Search(payload, new byte[] { 0x3C, 0xB6, 0x67 }) >= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Opcode: {Enum.GetName(typeof(OpCodes), opcode)} found with *");
                    //Logger.AppendLog(88484, ((int)opcode).ToString(), BitConverter.ToString(payload).Replace("-", ""));
                }
                if ((int)opcode == 0xd817)
                {
                    var p = new PKTPartyUnknown(new BitReader(payload));
                    PartyTracker.Instance.ProcessPKTPartyUnknown(p);
                    System.Diagnostics.Debug.WriteLine($"Opcode: {Enum.GetName(typeof(OpCodes), opcode)} found with *");
                }
                if (opcode == OpCodes.PKTPartyLeaveResult)
                {
                    var ppl = new PKTPartyLeaveResult(new BitReader(payload));
                    PartyTracker.Instance.ProcessPKTPartyLeaveResult(ppl);
                    System.Diagnostics.Debug.WriteLine($"Opcode: {Enum.GetName(typeof(OpCodes), opcode)} found with *");
                }
                if (opcode == OpCodes.PKTTriggerStartNotify)
                {
                    var trigger = new PKTTriggerStartNotify(new BitReader(payload));
                    if (trigger.TriggerSignalType >= (int)TriggerSignalType.DUNGEON_PHASE1_CLEAR && trigger.TriggerSignalType <= (int)TriggerSignalType.DUNGEON_PHASE4_FAIL) // if in range of dungeon fail/kill
                    {
                        if (((TriggerSignalType)trigger.TriggerSignalType).ToString().Contains("FAIL")) // not as good performance, but more clear and in case enums change order in future
                        {
                            WasWipe = true;
                            WasKill = false;
                        }
                        else
                        {
                            WasKill = true;
                            WasWipe = false;
                        }
                    }
                }
                else if (opcode == OpCodes.PKTNewProjectile)
                {
                    var projectile = new PKTNewProjectile(new BitReader(payload)).projectileInfo;
                    currentEncounter.Entities.AddOrUpdate(new Entity
                    {
                        OwnerId = projectile.OwnerId,
                        EntityId = projectile.ProjectileId,
                        Type = Entity.EntityType.Projectile
                    });
                    var battleitem = BattleItem.IsBattleItem(projectile.SkillId, "projectile");
                    if (battleitem)
                    {
                        Entity entity = currentEncounter.Entities.GetOrAdd(projectile.OwnerId);
                        var log = new LogInfo
                        {
                            Time = DateTime.Now,
                            SourceEntity = entity,
                            DestinationEntity = entity, //projectiles don't have destination, but can't be null causes exception getting encounter name
                            SkillName = BattleItem.GetBattleItemName(projectile.SkillId),
                            SkillId = projectile.SkillId,
                            BattleItem = battleitem
                        };
                        currentEncounter.Infos.Add(log);
                        Logger.AppendLog(15, projectile.OwnerId.ToString("X"), entity.Name, projectile.SkillId.ToString(), BattleItem.GetBattleItemName(projectile.SkillId));
                    }
                }
                else if (opcode == OpCodes.PKTInitEnv)
                {
                    var env = new PKTInitEnv(new BitReader(payload));
                    beforeNewZone?.Invoke();
                    if (currentEncounter.Infos.Count <= 50)
                    {
                        var oldenc = currentEncounter;
                        currentEncounter = new Encounter();
                        currentEncounter.Entities = oldenc.Entities;
                        Encounters.Remove(oldenc);
                    }
                    else
                    {
                        currentEncounter.End = DateTime.Now;
                        currentEncounter = new Encounter();
                    }

                    Encounters.Add(currentEncounter);
                    _localEntityId = env.PlayerId;
                    var temp = new Entity
                    {
                        EntityId = env.PlayerId,
                        Name = _localPlayerName,
                        Type = Entity.EntityType.Player,
                        GearLevel = _localGearLevel,
                        PartyId = _localCharacterId
                    };
                    currentEncounter.Entities.TryAdd(env.PlayerId, temp);
                    if (_localCharacterId != 0)
                        PartyTracker.Instance.ProcessPKTInitEnv(env, _localCharacterId);
                    onNewZone?.Invoke();
                    Logger.AppendLog(1, env.PlayerId.ToString("X"));
                    System.Diagnostics.Debug.WriteLine($"Own EntityId: {env.PlayerId:X}");
                }
                else if (opcode == OpCodes.PKTRaidBossKillNotify //Packet sent for boss kill, wipe or start
                         || opcode == OpCodes.PKTTriggerBossBattleStatus
                         || opcode == OpCodes.PKTRaidResult)
                {
                    var Duration = Convert.ToUInt64(DateTime.Now.Subtract(currentEncounter.Start).TotalSeconds);
                    currentEncounter.End = DateTime.Now;
                    Task.Run(async() =>
                    {
                        
                        if (WasKill || WasWipe || opcode == OpCodes.PKTRaidBossKillNotify || opcode == OpCodes.PKTRaidResult) // if kill or wipe update the raid time duration 
                        {
                            await Task.Delay(12000);
                            currentEncounter.RaidTime += Duration;
                            foreach (var i in currentEncounter.Entities.Where(e => e.Value.Type == Entity.EntityType.Player))
                            {
                                if (!(i.Value.dead)) // if Player not dead on end of kill write fake death logInfo to track their time alive
                                {
                                    var log = new LogInfo
                                    {
                                        Time = DateTime.Now,
                                        SourceEntity = i.Value,
                                        DestinationEntity = i.Value,
                                        SkillName = "Death",
                                        TimeAlive = Duration,
                                        Death = true
                                    };
                                    currentEncounter.RaidInfos.Add(log);
                                    currentEncounter.Infos.Add(log);

                                }
                                else // reset death flag on every wipe or kill
                                {
                                    i.Value.dead = false;
                                }
                            }
                            
                        }
                        
                        //Task.Delay(100); // wait 4000ms to capture any final damage/status Effect packets
                        currentEncounter = new Encounter();
                        currentEncounter.Entities = Encounters.Last().Entities; // preserve entities
                        if (WasWipe || Encounters.Last().AfterWipe)
                        {

                            currentEncounter.RaidInfos = Encounters.Last().RaidInfos;
                            currentEncounter.AfterWipe = true; // flag signifying zone after wipe
                            if (Encounters.Last().AfterWipe)
                            {
                                Duration = 0; // dont add time for zone inbetween pulls for raid time
                                currentEncounter.AfterWipe = false;
                            }
                            currentEncounter.RaidTime = Encounters.Last().RaidTime + Duration;// update raid duration
                            WasWipe = false;

                        }
                        else if (WasKill)
                        {
                            WasKill = false;
                        }

                        
                        if (Encounters.Count > 0 && Encounters.Last().Infos.Count <= 50)
                        {
                            Encounters.Remove(Encounters.Last());
                        }
                        Encounters.Add(currentEncounter);

                        var phaseCode = "0"; // PKTRaidResult
                        if (opcode == OpCodes.PKTRaidBossKillNotify) phaseCode = "1";
                        else if (opcode == OpCodes.PKTTriggerBossBattleStatus) phaseCode = "2";
                        Logger.AppendLog(2, phaseCode);
                    });
                }
                else if (opcode == OpCodes.PKTInitPC)
                {
                    var pc = new PKTInitPC(new BitReader(payload));
                    beforeNewZone?.Invoke();
                    if (currentEncounter.Infos.Count == 0) Encounters.Remove(currentEncounter);
                    currentEncounter = new Encounter();
                    Encounters.Add(currentEncounter);
                    _localPlayerName = DisplayNames ? pc.Name.Value : Npc.GetPcClass(pc.ClassId);
                    _localGearLevel = pc.GearLevel;
                    _localEntityId = pc.PlayerId;
                    _localCharacterId = (ulong)pc.Unk56;
                    var tempEntity = new Entity
                    {

                        EntityId = pc.PlayerId,
                        PartyId = (ulong)pc.Unk56,
                        Name = _localPlayerName,
                        ClassName = Npc.GetPcClass(pc.ClassId),
                        Type = Entity.EntityType.Player,
                        GearLevel = _localGearLevel
                    };
                    System.Diagnostics.Debug.WriteLine($"EntityId: {tempEntity.EntityId:X} Name: {tempEntity.Name} ClassName: {tempEntity.ClassName} Type: {tempEntity.Type}");
                    currentEncounter.Entities.AddOrUpdate(tempEntity);
                    //PCIdMapper.Instance.AddCharacterIdAndEntityIdMapping(pc.CharacterId, pc.PlayerId);
                    PartyTracker.Instance.ProcessPKTInitPC(pc);
                    statusEffectTracker.InitPc(pc);
                    onNewZone?.Invoke();

                    if (!currentEncounter.LoggedEntities.ContainsKey(pc.PlayerId))
                    {
                        var gearScore = BitConverter.ToSingle(BitConverter.GetBytes(pc.GearLevel), 0).ToString("0.##");
                        Logger.AppendLog(3, pc.PlayerId.ToString("X"), pc.Name.Value, pc.ClassId.ToString(), Npc.GetPcClass(pc.ClassId), pc.Level.ToString(), gearScore, pc.statPair.Value[pc.statPair.StatType.IndexOf((Byte)StatType.STAT_TYPE_HP)].ToString(), pc.statPair.Value[pc.statPair.StatType.IndexOf((Byte)StatType.STAT_TYPE_MAX_HP)].ToString());
                        currentEncounter.LoggedEntities.TryAdd(pc.PlayerId, true);
                    }
                }
                else if (opcode == OpCodes.PKTNewPC)
                {
                    var pcPacket = new PKTNewPC(new BitReader(payload));
                    var pc = pcPacket.PCStruct;
                    var temp = new Entity
                    {
                        EntityId = pc.PlayerId,
                        PartyId = pc.CharacterId,
                        Name = DisplayNames ? pc.Name.Value : Npc.GetPcClass(pc.ClassId),
                        ClassName = Npc.GetPcClass(pc.ClassId),
                        Type = Entity.EntityType.Player,
                        GearLevel = pc.GearLevel,
                        dead = false
                    };
                    System.Diagnostics.Debug.WriteLine($"EntityId: {temp.EntityId:X} CharacterId: {temp.PartyId:X} Name: {temp.Name} ClassName: {temp.ClassName} Type: {temp.Type}");
                    if (currentEncounter.Entities.ContainsKey(temp.EntityId))
                    {
                        temp.dead = currentEncounter.Entities.GetOrAdd(temp.EntityId).dead;
                    }
                    currentEncounter.Entities.AddOrUpdate(temp);
                    var currentHp = pc.statPair.StatType.IndexOf((byte)StatType.STAT_TYPE_HP) >= 0 ? pc.statPair.Value[pc.statPair.StatType.IndexOf((byte)StatType.STAT_TYPE_HP)].ToString() : "0";
                    var maxHp = pc.statPair.StatType.IndexOf((byte)StatType.STAT_TYPE_MAX_HP) >= 0 ? pc.statPair.Value[pc.statPair.StatType.IndexOf((byte)StatType.STAT_TYPE_MAX_HP)].ToString() : "0";
                    PCIdMapper.Instance.AddCharacterIdAndEntityIdMapping(pc.CharacterId, pc.PlayerId);
                    statusEffectTracker.NewPc(pcPacket);
                    PartyTracker.Instance.ProcessPKTNewPC(pcPacket);
                    if (!currentEncounter.LoggedEntities.ContainsKey(pc.PlayerId))
                    {
                        var gearScore = BitConverter.ToSingle(BitConverter.GetBytes(pc.GearLevel), 0).ToString("0.##");
                        Logger.AppendLog(3, pc.PlayerId.ToString("X"), temp.Name, pc.ClassId.ToString(), Npc.GetPcClass(pc.ClassId), pc.Level.ToString(), gearScore, pc.statPair.Value[pc.statPair.StatType.IndexOf((Byte)StatType.STAT_TYPE_HP)].ToString(), pc.statPair.Value[pc.statPair.StatType.IndexOf((Byte)StatType.STAT_TYPE_MAX_HP)].ToString());
                        currentEncounter.LoggedEntities.TryAdd(pc.PlayerId, true);
                    }
                }
                else if (opcode == OpCodes.PKTNewNpc)
                {
                    var npcPacket = new PKTNewNpc(new BitReader(payload));
                    var npc = npcPacket.NpcStruct;
                    currentEncounter.Entities.AddOrUpdate(new Entity
                    {
                        EntityId = npc.ObjectId,
                        Name = Npc.GetNpcName(npc.TypeId),
                        Type = Entity.EntityType.Npc
                    });
                    var hp_pos = npc.statPair.StatType.IndexOf((Byte)StatType.STAT_TYPE_HP);
                    var hp_max_pos = npc.statPair.StatType.IndexOf((Byte)StatType.STAT_TYPE_MAX_HP);
                    if (hp_pos >= 0 && hp_max_pos >= 0)
                        Logger.AppendLog(4, npc.ObjectId.ToString("X"), npc.TypeId.ToString(), Npc.GetNpcName(npc.TypeId), npc.statPair.Value[hp_pos].ToString(), npc.statPair.Value[hp_max_pos].ToString());
                    statusEffectTracker.NewNpc(npcPacket);

                    System.Diagnostics.Debug.WriteLine($"EntityId: {npc.ObjectId:X} Name: {Npc.GetNpcName(npc.TypeId)} Type: {Entity.EntityType.Npc}");

                }
                else if (opcode == OpCodes.PKTRemoveObject)
                {
                    //var obj = new PKTRemoveObject(new BitReader(payload));
                    //var projectile = new PKTRemoveObject { Bytes = converted };
                    //ProjectileOwner.Remove(projectile.ProjectileId, projectile.OwnerId);
                }
                else if (opcode == OpCodes.PKTDeathNotify)
                {
                    var death = new PKTDeathNotify(new BitReader(payload));
                    Logger.AppendLog(5, death.TargetId.ToString("X"), currentEncounter.Entities.GetOrAdd(death.TargetId).Name, death.SourceId.ToString("X"), currentEncounter.Entities.GetOrAdd(death.SourceId).Name);

                    DateTime DeathTime = DateTime.Now;
                    TimeSpan timeAlive = DeathTime.Subtract(currentEncounter.Start);
                    if (currentEncounter.Entities.GetOrAdd(death.TargetId).Type == Entity.EntityType.Player) // if death is from player, add death log for time alive tracking
                    {
                        currentEncounter.Entities.GetOrAdd(death.TargetId).dead = true;
                        var log = new LogInfo
                        {
                            Time = DateTime.Now,
                            SourceEntity = currentEncounter.Entities.GetOrAdd(death.TargetId),
                            DestinationEntity = currentEncounter.Entities.GetOrAdd(death.TargetId),
                            SkillName = "Death",
                            TimeAlive = Convert.ToUInt64(timeAlive.TotalSeconds),
                            Death = true
                        };
                        currentEncounter.RaidInfos.Add(log);
                        currentEncounter.Infos.Add(log);
                    }

                    statusEffectTracker.DeathNotify(death);
                }
                else if (opcode == OpCodes.PKTSkillStartNotify)
                {
                    var skill = new PKTSkillStartNotify(new BitReader(payload));
                    Logger.AppendLog(6, skill.SourceId.ToString("X"), currentEncounter.Entities.GetOrAdd(skill.SourceId).Name, skill.SkillId.ToString(), Skill.GetSkillName(skill.SkillId));
                }
                else if (opcode == OpCodes.PKTSkillStageNotify)
                {
                    /*
                       2-stage charge
                        1 start
                        5 if use, 3 if continue
                        8 if use, 4 if continue
                        7 final
                       1-stage charge
                        1 start
                        5 if use, 2 if continue
                        6 final
                       holding whirlwind
                        1 on end
                       holding perfect zone
                        4 on start
                        5 on suc 6 on fail
                    */
                    var skill = new PKTSkillStageNotify(new BitReader(payload));
                    Logger.AppendLog(7, skill.SourceId.ToString("X"), currentEncounter.Entities.GetOrAdd(skill.SourceId).Name, skill.SkillId.ToString(), Skill.GetSkillName(skill.SkillId), skill.Stage.ToString());
                }
                else if (opcode == OpCodes.PKTSkillDamageNotify)
                    ProcessSkillDamage(new PKTSkillDamageNotify(new BitReader(payload)));
                else if (opcode == OpCodes.PKTSkillDamageAbnormalMoveNotify)
                    ProcessSkillDamage(new PKTSkillDamageAbnormalMoveNotify(new BitReader(payload)));
                else if (opcode == OpCodes.PKTStatChangeOriginNotify) // heal
                {
                    var health = new PKTStatChangeOriginNotify(new BitReader(payload));
                    var entity = currentEncounter.Entities.GetOrAdd(health.ObjectId);
                    var log = new LogInfo
                    {
                        Time = DateTime.Now,
                        SourceEntity = entity,
                        DestinationEntity = entity,
                        Heal = (UInt32)health.StatPairChangedList.Value[0].Value
                    };
                    onCombatEvent?.Invoke(log);
                    // might push this by 1??
                    Logger.AppendLog(9, entity.EntityId.ToString("X"), entity.Name, health.StatPairChangedList.Value[0].ToString(), health.StatPairChangedList.Value[0].ToString());// need to lookup cached max hp??

                }
                else if (opcode == OpCodes.PKTStatusEffectAddNotify) // shields included
                {
                    var statusEffect = new PKTStatusEffectAddNotify(new BitReader(payload));
                    var battleItem = BattleItem.IsBattleItem(statusEffect.statusEffectData.StatusEffectId, "buff");
                    if (battleItem)
                    {
                        var log = new LogInfo
                        {
                            Time = DateTime.Now,
                            SourceEntity = currentEncounter.Entities.GetOrAdd(statusEffect.statusEffectData.SourceId),
                            DestinationEntity = currentEncounter.Entities.GetOrAdd(statusEffect.ObjectId),
                            SkillId = statusEffect.statusEffectData.StatusEffectId,
                            SkillName = BattleItem.GetBattleItemName(statusEffect.statusEffectData.StatusEffectId),
                            BattleItem = battleItem
                        };
                        currentEncounter.Infos.Add(log);
                        Logger.AppendLog(15, statusEffect.statusEffectData.SourceId.ToString("X"), currentEncounter.Entities.GetOrAdd(statusEffect.statusEffectData.SourceId).Name, statusEffect.statusEffectData.StatusEffectId.ToString(), BattleItem.GetBattleItemName(statusEffect.statusEffectData.StatusEffectId));
                    }
                    statusEffectTracker.Add(statusEffect);
                    var amount = statusEffect.statusEffectData.hasValue ? BitConverter.ToUInt32(statusEffect.statusEffectData.Value, 0) : 0;
                }
                else if (opcode == OpCodes.PKTPartyStatusEffectAddNotify)
                {
                    var partyStatusEffect = new PKTPartyStatusEffectAddNotify(new BitReader(payload));
                    statusEffectTracker.PartyAdd(partyStatusEffect);
                }
                else if (opcode == OpCodes.PKTStatusEffectRemoveNotify)
                {
                    var statusEffectRemove = new PKTStatusEffectRemoveNotify(new BitReader(payload));
                    statusEffectTracker.Remove(statusEffectRemove);

                }
                else if (opcode == OpCodes.PKTPartyStatusEffectRemoveNotify)
                {
                    var partyStatusEffectRemove = new PKTPartyStatusEffectRemoveNotify(new BitReader(payload));
                    statusEffectTracker.PartyRemove(partyStatusEffectRemove);
                }
                /*else if (opcode == OpCodes.PKTParalyzationStateNotify)
                {
                    var stagger = new PKTParalyzationStateNotify(new BitReader(payload));
                    var enemy = currentEncounter.Entities.GetOrAdd(stagger.TargetId);
                    var lastInfo = currentEncounter.Infos.LastOrDefault(); // hope this works
                    if (lastInfo != null) // there's no way to tell what is the source, so drop it for now
                    {
                        var player = lastInfo.SourceEntity;
                        var staggerAmount = stagger.ParalyzationPoint - enemy.Stagger;
                        if (stagger.ParalyzationPoint == 0)
                            staggerAmount = stagger.ParalyzationPointMax - enemy.Stagger;
                        enemy.Stagger = stagger.ParalyzationPoint;
                        var log = new LogInfo
                        {
                            Time = DateTime.Now, SourceEntity = player, DestinationEntity = enemy,
                            SkillName = lastInfo?.SkillName, Stagger = staggerAmount
                        };
                        onCombatEvent?.Invoke(log);
                    }
                }*/
                else if (opcode == OpCodes.PKTCounterAttackNotify)
                {
                    var counter = new PKTCounterAttackNotify(new BitReader(payload));
                    var source = currentEncounter.Entities.GetOrAdd(counter.SourceId);
                    var target = currentEncounter.Entities.GetOrAdd(counter.TargetId);
                    var log = new LogInfo
                    {
                        Time = DateTime.Now,
                        SourceEntity = currentEncounter.Entities.GetOrAdd(counter.SourceId),
                        DestinationEntity = currentEncounter.Entities.GetOrAdd(counter.TargetId),
                        SkillName = "Counter",
                        Damage = 0,
                        Counter = true
                    };
                    onCombatEvent?.Invoke(log);
                    Logger.AppendLog(12, source.EntityId.ToString("X"), source.Name, target.EntityId.ToString("X"), target.Name);
                }
                else if (opcode == OpCodes.PKTNewNpcSummon)
                {
                    var npc = new PKTNewNpcSummon(new BitReader(payload));
                    currentEncounter.Entities.AddOrUpdate(new Entity
                    {
                        EntityId = npc.NpcData.TypeId,
                        OwnerId = npc.OwnerId,
                        Type = Entity.EntityType.Summon
                    });
                }
                else if (opcode == OpCodes.PKTPartyInfo)
                {
                    var partyInfo = new PKTPartyInfo(new BitReader(payload));
                    PartyTracker.Instance.ProcessPKTPartyInfo(partyInfo);
                    System.Diagnostics.Debug.WriteLine("Printing PartyInfo");
                    MemberInfo[] members = partyInfo.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
                    foreach (MemberInfo memberInfo in members)
                    {
  

                            if (memberInfo.MemberType == MemberTypes.Field)
                            {
                                FieldInfo fi = (FieldInfo)memberInfo;

                                object value = fi.GetValue(partyInfo);

                                if (fi.FieldType.IsValueType)
                                {
                                    System.Diagnostics.Debug.WriteLine("    {0}: {1:X}", memberInfo.Name, value);
                                }
                                else if (fi.FieldType == typeof(string))
                                {
                                    System.Diagnostics.Debug.WriteLine("    {0}: {1}", memberInfo.Name, value);
                                }
                                else
                                {
                                    var isEnumerable = typeof(IEnumerable).IsAssignableFrom(fi.FieldType);
                                    System.Diagnostics.Debug.WriteLine("    {0}: {1}", memberInfo.Name, isEnumerable ? "..." : "{ }");
                                }
                            }
                    }

                    foreach (var memberData in partyInfo.MemberDatas)
                    {
                        System.Diagnostics.Debug.WriteLine("    MemberData Start");
                        members = memberData.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);
                        foreach (MemberInfo memberInfo in members)
                        {
                            if (memberInfo.MemberType == MemberTypes.Field)
                            {
                                FieldInfo fi = (FieldInfo)memberInfo;

                                object value = fi.GetValue(memberData);

                                if (fi.FieldType.IsValueType)
                                {
                                    System.Diagnostics.Debug.WriteLine("        {0}: {1:X}", memberInfo.Name, value);
                                }
                                else if (fi.FieldType == typeof(string))
                                {
                                    System.Diagnostics.Debug.WriteLine("        {0}: {1}", memberInfo.Name, value);
                                }
                                else
                                {
                                    var isEnumerable = typeof(IEnumerable).IsAssignableFrom(fi.FieldType);
                                    System.Diagnostics.Debug.WriteLine("        {0}: {1}", memberInfo.Name, isEnumerable ? "..." : "{ }");
                                }
                            }
                        }
                        System.Diagnostics.Debug.WriteLine("    MemberData End");
                    }
                }
                else
                {
                    
                    if (Search(payload, new byte[] { 0x23, 0x5c, 0x17 }) >= 0)
                    {
                        Logger.AppendLog(88484, ((int)opcode).ToString(), BitConverter.ToString(payload).Replace("-", ""));
                    }

                }
                if (packets.Length < packetSize) throw new Exception("bad packet maybe");
                packets = packets.Skip(packetSize).ToArray();
            }
        }

        int Search(byte[] src, byte[] pattern)
        {
            int maxFirstCharSlot = src.Length - pattern.Length + 1;
            for (int i = 0; i < maxFirstCharSlot; i++)
            {
                if (src[i] != pattern[0]) // compare only first byte
                    continue;

                // found a match on first byte, now try to match rest of the pattern
                for (int j = pattern.Length - 1; j >= 1; j--)
                {
                    if (src[i + j] != pattern[j]) break;
                    if (j == 1) return i;
                }
            }
            return -1;
        }

        protected UInt32 currentIpAddr = 0xdeadbeef;
        protected int loggedPacketCount = 0;


        void Device_OnPacketArrival_machina(Machina.Infrastructure.TCPConnection connection, byte[] bytes)
        {
            if (tcp == null) return; // To avoid any late delegate calls causing state issues when listener uninstalled
            lock (lockPacketProcessing)
            {
                if (connection.RemotePort != 6040) return;
                var srcAddr = connection.RemoteIP;
                if (srcAddr != currentIpAddr)
                {
                    if (currentIpAddr == 0xdeadbeef || (bytes.Length > 4 && GetOpCode(bytes) == OpCodes.PKTAuthTokenResult && bytes[0] == 0x1e))
                    {
                        beforeNewZone?.Invoke();
                        onNewZone?.Invoke();
                        currentIpAddr = srcAddr;
                    }
                    else return;
                }
                Logger.DoDebugLog(bytes);
                try {
                    ProcessPacket(bytes.ToList());
                } catch (Exception e) {
                    // Console.WriteLine("Failure during processing of packet: " + e);
                }
            }
        }

        void Device_OnPacketArrival_pcap(object sender, PacketCapture evt)
        {
            if (pcap == null) return;
            lock (lockPacketProcessing)
            {
                var rawpkt = evt.GetPacket();
                var packet = PacketDotNet.Packet.ParsePacket(rawpkt.LinkLayerType, rawpkt.Data);
                var ipPacket = packet.Extract<PacketDotNet.IPPacket>();
                var tcpPacket = packet.Extract<PacketDotNet.TcpPacket>();
                var bytes = tcpPacket.PayloadData;

                if (tcpPacket != null)
                {
                    if (tcpPacket.SourcePort != 6040) return;
#pragma warning disable CS0618 // Type or member is obsolete
                    var srcAddr = (uint)ipPacket.SourceAddress.Address;
#pragma warning restore CS0618 // Type or member is obsolete
                    if (srcAddr != currentIpAddr)
                    {
                        if (currentIpAddr == 0xdeadbeef || (bytes.Length > 4 && GetOpCode(bytes) == OpCodes.PKTAuthTokenResult && bytes[0] == 0x1e))
                        {
                            beforeNewZone?.Invoke();
                            onNewZone?.Invoke();
                            currentIpAddr = srcAddr;
                        }
                        else return;
                    }
                    Logger.DoDebugLog(bytes);
                    try {
                        ProcessPacket(bytes.ToList());
                    } catch (Exception e) {
                        // Console.WriteLine("Failure during processing of packet: " + e);
                    }
                }
            }
        }
        private void Parser_onDamageEvent(LogInfo log)
        {
            currentEncounter.Infos.Add(log);
        }

        private void Parser_onStatusEffectEnded(StatusEffect statusEffect, TimeSpan duration)
        {
            Entity dstEntity;
            if (statusEffect.Type == StatusEffect.StatusEffectType.Party)
            {
                try
                {
                    dstEntity = currentEncounter.Entities.GetOrAdd(PCIdMapper.Instance.GetEntityIdFormCharacterId(statusEffect.TargetId));
                }
                catch(KeyNotFoundException)
                {
                    return;
                }
            }
            else
                dstEntity = currentEncounter.Entities.GetOrAdd(statusEffect.TargetId);
            var log = new LogInfo
            {
                Time = DateTime.Now,
                SourceEntity = currentEncounter.Entities.GetOrAdd(statusEffect.SourceId),
                DestinationEntity = dstEntity,
                SkillEffectId = statusEffect.StatusEffectId,
                SkillName = SkillBuff.GetSkillBuffName(statusEffect.StatusEffectId),
                Damage = 0,
                Duration = duration
            };
            currentEncounter.Infos.Add(log);
            Logger.AppendLog(11, statusEffect.StatusEffectId.ToString("X"), SkillBuff.GetSkillBuffName(statusEffect.StatusEffectId), statusEffect.TargetId.ToString("X"), currentEncounter.Entities.GetOrAdd(statusEffect.TargetId).Name);

        }
        private void StatusEffectTracker_OnStatusEffectStarted(StatusEffect statusEffect)
        {
            Logger.AppendLog(10, statusEffect.SourceId.ToString("X"), currentEncounter.Entities.GetOrAdd(statusEffect.SourceId).Name, statusEffect.StatusEffectId.ToString("X"), SkillBuff.GetSkillBuffName(statusEffect.StatusEffectId), statusEffect.TargetId.ToString("X"), currentEncounter.Entities.GetOrAdd(statusEffect.TargetId).Name, statusEffect.Value.ToString());

        }

        private void Parser_onNewZone()
        {
            //Logger.StartNewLogFile();
            loggedPacketCount = 0;
        }

        public Entity GetSourceEntity(UInt64 sourceId)
        {
            var sourceEntity = currentEncounter.Entities.GetOrAdd(sourceId);
            if (sourceEntity.Type == Entity.EntityType.Projectile)
                sourceEntity = currentEncounter.Entities.GetOrAdd(sourceEntity.OwnerId);
            if (sourceEntity.Type == Entity.EntityType.Summon)
                sourceEntity = currentEncounter.Entities.GetOrAdd(sourceEntity.OwnerId);
            return sourceEntity;
        }
        public void UninstallListeners()
        {
            if (tcp != null) tcp.Stop();
            if (pcap != null)
            {
                try
                {
                    pcap.StopCapture();
                    pcap.Close();
                }
                catch (Exception ex)
                {
                    var exceptionMessage = "Exception while trying to stop capture on NIC " + pcap.Name + "\n" + ex.ToString();
                    Console.WriteLine(exceptionMessage);
                    Logger.AppendLog(254, exceptionMessage);
                }
            }
            tcp = null;
            pcap = null;
        }
        protected void OnNewZone()
        {
            onNewZone?.Invoke();
        }

        public void Clear()
        {
            lock (lockPacketProcessing)
            {
                var oldEnc = currentEncounter;
                var newEnc = new Encounter();
                newEnc.Entities = oldEnc.Entities;
                Encounters.Add(newEnc);
                currentEncounter = newEnc;
                oldEnc.End = DateTime.UtcNow;
                onNewZone?.Invoke();
            }
        }

        public void Dispose()
        { }
    }
}
