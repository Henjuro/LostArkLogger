﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows.Forms;
namespace InetOptimizer
{
    public class MxFilter : TraceFilter
    {
        public override bool ShouldTrace(TraceEventCache cache, string source, TraceEventType eventType, int id, string formatOrMessage, object[] args, object data1, object[] data)
        {
            if (!String.IsNullOrEmpty(formatOrMessage) && formatOrMessage.StartsWith("Skill:")) {
                return true;
            }
            return false;
        }
    }
    internal static class Program
    {
        //public static bool IsConsole = OpenStandardInput(1) != Stream.Null;
        // https://stackoverflow.com/questions/1188658/how-can-a-c-sharp-windows-console-application-tell-if-it-is-run-interactively
        [DllImport("kernel32.dll")]
        static extern IntPtr GetModuleHandleW(IntPtr lpModuleName);
        static Subsystem GetSubsystem()
        {
            var p = GetModuleHandleW(default);          // PE image VM mapped base address
            p += Marshal.ReadInt32(p, 0x3C);                // RVA of COFF/PE within DOS header
            return (Subsystem)Marshal.ReadInt16(p + 0x5C);  // PE offset to 'Subsystem' value
        }
        public static bool IsConsole = GetSubsystem() == Subsystem.WindowsCui;
        [STAThread]
        static void Main(string[] args)
        {

            var flag = 0xF6;
            var f1 = (flag >> 1);
            var d = f1 & 7;

            var bytes = new byte[] { 0x0C };
            if (bytes.Length < 8) bytes = bytes.Concat(new Byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }).ToArray();
            var result = (BitConverter.ToInt64(bytes, 0) << 4) | (uint)(flag >> 4);
            var res = (flag & 1) == 0 ? result : -result;
            var listener = new TextWriterTraceListener("StatusEffectDebug.log", "FileLogListener");
            listener.Filter = new MxFilter();
            Trace.Listeners.Add(listener);
            System.Diagnostics.Debug.AutoFlush = true;
            Properties.Settings.Default.Providers.Clear();
            Bluegrams.Application.PortableSettingsProvider.SettingsFileName = AppDomain.CurrentDomain.FriendlyName + ".ini";
            Bluegrams.Application.PortableSettingsProvider.ApplyProvider(Properties.Settings.Default);
            if (!IsConsole) Warning();
            if (!IsConsole)
            {
                Application.EnableVisualStyles();
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainWindow());
            }
            else
            {
                var httpBridge = new HttpBridge();
                httpBridge.args = args;
                httpBridge.Start();
            }
            if (File.Exists(Utilities.Logger.fileName) && Properties.Settings.Default.AutoUpload)
            {
                var fileBytes = File.ReadAllBytes(Utilities.Logger.fileName);
                var fileText = File.ReadAllText(Utilities.Logger.fileName);
                if (fileBytes.Length > 100 && fileText.Contains("8|"))
                    Utilities.Uploader.UploadLog(fileBytes);
            }
        }
        static void AttemptFirewallPrompt()
        {
            var ipAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
            var ipLocalEndPoint = new IPEndPoint(ipAddress, 12345);
            var t = new TcpListener(ipLocalEndPoint);
            t.Start();
            t.Stop();
        }
        static void VersionCompatibility()
        {
            (var region, var installedVersion) = VersionCheck.GetLostArkVersion();
            if (installedVersion == null)
            {
                MessageBox.Show("Launch Lost Ark before launching logger if you want DPS meter to work.", "Lost Ark Not Running", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                Environment.Exit(10);
            }
            else if (region == Region.Unknown)
            {
                MessageBox.Show("DPS Meter is out of date.\nDPS Meter might not work until updated.\nCheck Discord/Github for more info.\nFeel free to add a message in the discord informing shalzuth that it's out of data", "Out of date!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (Properties.Settings.Default.Region != region)
            {
                Properties.Settings.Default.Region = region;
                Properties.Settings.Default.Save();
            }
        }
        static void Warning()
        {
            if (Properties.Settings.Default.IgnoreWarning) return;
            if (AppDomain.CurrentDomain.FriendlyName.Contains("LostArk"))
            {
                //var tempName = "LostArkDps" + Guid.NewGuid().ToString().Substring(0, 6) + ".exe";
                var tempName = "DpsMeter.exe";
                MessageBox.Show("LostArkLogger.exe is flagged.\nRenaming to " + tempName + " !", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                File.Copy(AppDomain.CurrentDomain.FriendlyName, tempName);
                Process.Start(tempName);
                Environment.Exit(0);
            }
            else
            {
                Process.GetProcessesByName("LostArkLogger").ToList().ForEach(p => p.Kill());
                if (File.Exists("LostArkLogger.exe")) File.Delete("LostArkLogger.exe");
            }
            var res = MessageBox.Show("The game director has instructed Amazon Game Studios to ban users using a DPS Meter.\n\nAt this time, please refrain from using the DPS Meter.\n\nSelect \"Retry\" to voice your feedback, as this is not a hack nor a solution that should violate TOS", "Warning!", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Warning);
            if (res == DialogResult.Abort) Environment.Exit(0);
            else if (res == DialogResult.Retry)
            {
                Process.Start("https://forums.playlostark.com/t/talk-to-us-already-about-the-dps-meter/370558");
                Environment.Exit(0);
            }
            Properties.Settings.Default.IgnoreWarning = true;
            Properties.Settings.Default.Save();
        }
        private static bool AdminRelauncher()
        {
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            {
                var startInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    WorkingDirectory = Environment.CurrentDirectory,
                    FileName = Assembly.GetEntryAssembly().Location,
                    Verb = "runas"
                };
                try { Process.Start(startInfo); }
                catch (Exception ex) { MessageBox.Show("This program must be run as an administrator!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                return false;
            }
            return true;
        }
    }
}