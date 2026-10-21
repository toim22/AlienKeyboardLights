using System;
using System.Collections.Generic;
using Microsoft.Win32.SafeHandles;

namespace AlienKeyboardLights
{
    // Driver for the Area-51m R2 / m17 R3-R4 per-key RGB keyboard (AlienFX API v5, USB 0D62:1A1C).
    // Protocol reference: T-Troll/alienfx-tools (AlienFX_SDK).
    public sealed class AlienFxV5 : IDisposable
    {
        public const ushort Vid = 0x0D62, Pid = 0x1A1C;
        public const int KeyCount = 136;
        const byte ReportId = 0xCC;

        SafeFileHandle handle;
        int length;
        readonly object sync = new object();

        public static AlienFxV5 Open()
        {
            foreach (string path in Hid.EnumeratePaths("vid_0d62&pid_1a1c"))
            {
                int outLen, featLen;
                var h = Hid.OpenDevice(path, Vid, Pid, out outLen, out featLen);
                if (h == null) continue;
                // The lighting collection is the one with a large feature report (report id + 63 bytes).
                if (featLen >= 33) return new AlienFxV5 { handle = h, length = featLen };
                h.Dispose();
            }
            return null;
        }

        bool Send(params byte[] cmd)
        {
            var buf = new byte[length];
            buf[0] = ReportId;
            Array.Copy(cmd, 0, buf, 1, Math.Min(cmd.Length, length - 1));
            return Hid.HidD_SetFeature(handle, buf, length);
        }

        void Reset()
        {
            Send(0x94);
            Send(0x93);
            var buf = new byte[length];
            buf[0] = ReportId;
            Hid.HidD_GetFeature(handle, buf, length);
        }

        // Brightness 0..255 (hardware level).
        public bool SetBrightness(byte level)
        {
            lock (sync) { Reset(); return Send(0x83, 0x38, 0x9C, level); }
        }

        // colors[i] = 0xRRGGBB for key ids[i]. Returns false if the device stopped responding.
        public bool SetColors(IList<byte> ids, IList<int> colors)
        {
            lock (sync)
            {
                Reset();
                bool ok = true;
                int i = 0;
                while (i < ids.Count)
                {
                    var buf = new byte[length];
                    buf[0] = ReportId; buf[1] = 0x8C; buf[2] = 0x02;
                    for (int pos = 4; pos + 3 < length && i < ids.Count; pos += 4, i++)
                    {
                        int c = colors[i];
                        buf[pos] = (byte)(ids[i] + 1);
                        buf[pos + 1] = (byte)(c >> 16); buf[pos + 2] = (byte)(c >> 8); buf[pos + 3] = (byte)c;
                    }
                    ok &= Hid.HidD_SetFeature(handle, buf, length);
                }
                ok &= Send(0x8C, 0x13);
                ok &= Send(0x8B, 0x01, 0xFF);
                return ok;
            }
        }

        // Stops any firmware animation (e.g. one left by Alienware Command Center) so per-key colors show.
        public void ClearHardwareEffect()
        {
            lock (sync) { Reset(); Send(0x80, 0x01, 0xFE, 0x00, 0x00, 0x01, 0x01, 0x01); Send(0x8B, 0x01, 0xFF); }
        }

        public void Dispose() { if (handle != null) handle.Dispose(); }
    }
}
