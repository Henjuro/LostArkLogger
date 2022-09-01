using System;
using System.Collections.Generic;
namespace InetOptimizer
{
    public partial class PKTNewProjectile
    {
        public void KoreaDecode(BitReader reader)
        {
            projectileInfo = reader.Read<ProjectileInfo>();
        }
    }
}
