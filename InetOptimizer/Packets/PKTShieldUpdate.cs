using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InetOptimizer
{
    internal class PKTShieldUpdate
    {
        public PKTShieldUpdate(BitReader reader) {
            ShieldAmount = reader.ReadInt32();
            Unk0 = reader.ReadInt32();
            CharacterId = reader.ReadUInt64();
            EffectInstanceId = reader.ReadInt32();
            TargetId = reader.ReadUInt64();
            Unk2 = reader.ReadByte();
        }

        public int ShieldAmount;
        public int Unk0;
        public ulong CharacterId;
        public int EffectInstanceId;
        public ulong TargetId;
        public byte Unk2;
    }
}
