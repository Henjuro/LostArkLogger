using System;
using System.Collections.Generic;
namespace InetOptimizer
{
    public partial class PKTNewProjectile
    {
        public void SteamDecode(BitReader reader)
        {
            projectileInfo = reader.Read<ProjectileInfo>();
        }
    }
}
