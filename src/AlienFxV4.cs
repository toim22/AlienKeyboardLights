using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace AlienKeyboardLights
{
    // Driver for the chassis ("Tron") lights controller, AlienFX API v4 (USB 187C:0550).
    // Area-51m R2 light ids: 0 Alien Head, 1 Touchpad, 2 Power Button, 3 Ring Top, 4 Ring Bottom.
    public sealed class AlienFxV4 : IDisposable
    {
        public const ushort Vid = 0x187C, Pid = 0x0550;
        public static readonly string[] LightNames = { "Cabeça Alien", "Touchpad", "Botão Power", "Anel superior", "Anel inferior" };

        SafeFileHandle handle;
        int length;
        readonly object sync = new object();

        public static AlienFxV4 Open()
        {
            foreach (string path in Hid.EnumeratePaths("vid_187c&pid_0550"))
            {
                int outLen, featLen;
                var h = Hid.OpenDevice(path, Vid, Pid, out outLen, out featLen);
                if (h == null) continue;
                if (outLen >= 34) return new AlienFxV4 { handle = h, length = outLen };
                h.Dispose();
            }
            return null;
        }

        bool Send(params byte[] cmd)
        {
            var buf = new byte[length];
            Array.Copy(cmd, 0, buf, 1, Math.Min(cmd.Length, length - 1));
            return Hid.HidD_SetOutputReport(handle, buf, length);
        }

        void WaitReady()
        {
            var buf = new byte[length];
            for (int i = 0; i < 25; i++)
            {
                if (!Hid.HidD_GetInputReport(handle, buf, length) || buf[2] != 34) return;
                Thread.Sleep(10);
            }
        }

        // colors[i] = 0xRRGGBB for light ids[i]; lights sharing a color are sent together.
        public bool SetColors(IList<byte> ids, IList<int> colors)
        {
            lock (sync)
            {
                WaitReady();
                bool ok = Send(0x03, 0x21, 0x00, 0x04, 0xFF, 0xFF) & Send(0x03, 0x21, 0x00, 0x01, 0xFF, 0xFF);
                foreach (var group in Enumerable.Range(0, ids.Count).GroupBy(i => colors[i]))
                {
                    int c = group.Key;
                    var cmd = new List<byte> { 0x03, 0x27, (byte)(c >> 16), (byte)(c >> 8), (byte)c, 0x00, (byte)group.Count() };
                    cmd.AddRange(group.Select(i => ids[i]));
                    ok &= Send(cmd.ToArray());
                }
                ok &= Send(0x03, 0x21, 0x00, 0x03, 0xFF, 0xFF);
                return ok;
            }
        }

        // Brightness 0..100.
        public bool SetBrightness(int percent)
        {
            lock (sync)
            {
                WaitReady();
                return Send(0x03, 0x26, (byte)(0x64 - percent), 0x00, 0x05, 0x00, 0x01, 0x02, 0x03, 0x04);
            }
        }

        public void Dispose() { if (handle != null) handle.Dispose(); }
    }
}
