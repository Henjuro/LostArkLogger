using System;
using System.Collections.Generic;
namespace InetOptimizer
{
    public partial class PKTNewProjectile
    {
        public PKTNewProjectile(BitReader reader)
        {
            if (Properties.Settings.Default.Region == Region.Steam) SteamDecode(reader);
            if (Properties.Settings.Default.Region == Region.Korea) KoreaDecode(reader);
        }
        public ProjectileInfo projectileInfo;
    }
}
