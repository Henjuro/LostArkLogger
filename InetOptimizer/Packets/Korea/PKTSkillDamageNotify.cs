using System;
using System.Collections.Generic;
namespace InetOptimizer
{
    public partial class PKTSkillDamageNotify
    {
        public void KoreaDecode(BitReader reader)
        {
            skillDamageEvents = reader.ReadList<SkillDamageEvent>();
            b_0 = reader.ReadByte();
            SkillEffectId = reader.ReadUInt32();
            SkillId = reader.ReadUInt32();
            SourceId = reader.ReadUInt64();
        }
    }
}
