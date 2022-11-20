using InetOptimizer.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace InetOptimizer
{
    internal class RemoteParser : Parser
    {

        const string remoteCaputreDll = "remotecapture.dll";
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void GetPacketData(IntPtr pData, uint size);
        [DllImport(remoteCaputreDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void set_callback(IntPtr fctPointer);
        [DllImport(remoteCaputreDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int setup();
        private static GetPacketData RetriveDataDelegate;
        public IPAddress myip = IPAddress.Parse(Properties.Settings.Default.ipfilter);

        public RemoteParser() : base()
        {

        }

        override public void InstallListener()
        {

            RetriveDataDelegate = new GetPacketData(PacketProcessing);
            IntPtr callback_delegate =
                Marshal.GetFunctionPointerForDelegate(RetriveDataDelegate);
            set_callback(callback_delegate);
            Thread backgroundThread = new(new ThreadStart(RemoteParser.StartRemoteCaputre))
            {
                IsBackground = true
            };
            backgroundThread.Start();
        }

        public void PacketProcessing(IntPtr pData, uint size)
        {
            lock (lockPacketProcessing)
            {
                byte[] bytes = Array.Empty<byte>();
                byte[] rawdata = new byte[size];
                Marshal.Copy(pData, rawdata, 0, (int)size);
                PacketDotNet.Packet packet = null;
                try
                {
                    packet = PacketDotNet.Packet.ParsePacket(PacketDotNet.LinkLayers.Ethernet, rawdata);
                }
                catch
                {

                }
                if (packet == null) { return; }
                var ipPacket = packet?.Extract<PacketDotNet.IPPacket>();
                if (ipPacket == null) { return; }

                var tcpPacket = packet?.Extract<PacketDotNet.TcpPacket>();
                if (tcpPacket == null) { return; }

                if (tcpPacket != null)
                {
                    if (tcpPacket?.SourcePort != 6040) return;
                    bytes = tcpPacket?.PayloadData;
                    if (bytes == null) { return; }

#pragma warning disable CS0618 // Type or member is obsolete
                    if (ipPacket?.SourceAddress == null) { return; }
                    var srcAddr = (uint)ipPacket?.SourceAddress?.Address;
#pragma warning restore CS0618 // Type or member is obsolete
                    if (ipPacket?.SourceAddress == null) { return; }
                    if (!ipPacket.DestinationAddress.Equals(myip)) return;
                    if (srcAddr != currentIpAddr)
                    {
                        if (currentIpAddr == 0xdeadbeef || (bytes.Length > 4 && GetOpCode(bytes) == OpCodes.PKTAuthTokenResult && bytes[0] == 0x1e))
                        {
                            base.OnNewZone();
                            currentIpAddr = srcAddr;
                            loggedPacketCount = 0;
                        }
                        else
                        {
                            //   return;
                        }
                    }
                    if (bytes != null)
                        ProcessPacket(bytes?.ToList());
                }
            }

        }

        static void StartRemoteCaputre()
        {
            int res = setup();
            if (res < 0)
            {
                MessageBox.Show($"Failed with {res}");
            }
        }
    }
}
