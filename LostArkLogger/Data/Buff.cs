using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostArkLogger
{
    public class Buff : IComparable
    {
        public UInt32 InstanceId;
        public UInt32 BuffId;
        // this is party id for party buff otherwise entity id
        public UInt64 TargetId;
        public UInt64 SourceId;
        public BuffType Type;

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            if (obj is Buff buff)
            {
                if (InstanceId == buff.InstanceId) return 0;
                if (InstanceId < buff.InstanceId) return -1;
                return 1;
            }
            else
            {
                throw new ArgumentException("Object is not a Buff");
            }
        }

        public override int GetHashCode()
        {
            return (int)InstanceId;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            if (obj is Buff buff)
            {
                return buff.InstanceId == this.InstanceId;
            }
            else
            {
                throw new ArgumentException("Object is not a Buff");
            }
        }
        public override string ToString()
        {
            return "IID: " + InstanceId.ToString() + " | BuffId: " + BuffId.ToString() + " | tID: " + TargetId.ToString() + " | sID: " + SourceId.ToString() + " | Type: " + Type.ToString();
        }

        public enum BuffType
        {
            Party,
            Local
        }


    }
}
