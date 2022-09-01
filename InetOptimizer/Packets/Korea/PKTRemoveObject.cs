using System;
using System.Collections.Generic;
namespace InetOptimizer
{
    public partial class PKTRemoveObject
    {
        public void KoreaDecode(BitReader reader)
        {
            blist_0 = reader.ReadList<Byte>();
        }
    }
}
