using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace InetOptimizer
{
    internal class FileParser : Parser
    {
        public FileParser() : base() 
        {
        }

        public bool DelayMode = false;

        public override void InstallListener()
        {
            Task.Run(async () =>
            {
                try
                {
                    using (var br = new BinaryReader(new FileStream("C:\\Users\\SpeedProg\\Documents\\InetOptimizer\\InetOptimizer_2022-11-28-08-47-32.bin", FileMode.Open, FileAccess.Read)))
                    {
                        var CurrentStartDateTime = DateTime.Now;
                        var length = br.BaseStream.Length;
                        DateTime FirstEntryDateTime = DateTime.FromBinary(br.ReadInt64());
                        TimeSpan DToffset = CurrentStartDateTime - FirstEntryDateTime;
                        int pktLength = br.ReadInt32();
                        byte[] pkt = br.ReadBytes(pktLength);
                        this.ProcessPacket(pkt.ToList());
                        while (br.BaseStream.Position < length)
                        {
                            DateTime dt = DateTime.FromBinary(br.ReadInt64());
                            pktLength = br.ReadInt32();
                            pkt = br.ReadBytes(pktLength);
                            if (DelayMode)
                            {
                                DateTime newDT = dt + DToffset;
                                DateTime calc = DateTime.Now;
                                if (calc < newDT)
                                {
                                    TimeSpan waitTime = newDT - calc;
                                    await Task.Delay(waitTime);
                                }
                            }
                            this.ProcessPacket(pkt.ToList());
                        }
                    }
                }
                catch (EndOfStreamException e)
                {
                    System.Diagnostics.Debug.WriteLine(e.ToString());
                }
                finally
                {
                    System.Diagnostics.Debug.Write("Reading packetdump done!");
                }
            });
        }

    }
}
