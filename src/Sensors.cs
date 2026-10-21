using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AlienKeyboardLights
{
    // Peak level of the default playback device (what you hear), via the Windows Core Audio meter.
    class AudioMeter
    {
        IAudioMeterInformation meter;
        readonly Stopwatch sinceInit = new Stopwatch();

        public float Peak()
        {
            // Re-acquire periodically so a change of the default output device is picked up.
            if (meter == null || sinceInit.Elapsed.TotalSeconds > 5)
            {
                if (meter != null) { Marshal.ReleaseComObject(meter); meter = null; }
                try
                {
                    var en = (IMMDeviceEnumerator)new MMDeviceEnumeratorCom();
                    IMMDevice dev;
                    if (en.GetDefaultAudioEndpoint(0, 1, out dev) == 0)
                    {
                        var iid = typeof(IAudioMeterInformation).GUID;
                        object o;
                        if (dev.Activate(ref iid, 23, IntPtr.Zero, out o) == 0) meter = (IAudioMeterInformation)o;
                        Marshal.ReleaseComObject(dev);
                    }
                    Marshal.ReleaseComObject(en);
                }
                catch { meter = null; }
                sinceInit.Restart();
            }
            float v = 0;
            try { if (meter != null) meter.GetPeakValue(out v); } catch { meter = null; }
            return v;
        }

        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class MMDeviceEnumeratorCom { }

        [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IMMDeviceEnumerator
        {
            int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);
            int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice endpoint);
        }

        [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IMMDevice
        {
            int Activate(ref Guid iid, int clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object iface);
        }

        [ComImport, Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface IAudioMeterInformation
        {
            int GetPeakValue(out float peak);
        }
    }

    // Downscales the primary screen to a small grid (one pixel per key cell).
    class ScreenSampler
    {
        public const int W = 20, H = 7;
        public readonly Color[,] Cells = new Color[W, H];

        public void Sample()
        {
            var b = Screen.PrimaryScreen.Bounds;
            IntPtr screenDc = GetDC(IntPtr.Zero);
            IntPtr memDc = CreateCompatibleDC(screenDc);
            IntPtr bmp = CreateCompatibleBitmap(screenDc, W, H);
            IntPtr old = SelectObject(memDc, bmp);
            try
            {
                SetStretchBltMode(memDc, 4); // HALFTONE averages the area under each cell
                SetBrushOrgEx(memDc, 0, 0, IntPtr.Zero);
                StretchBlt(memDc, 0, 0, W, H, screenDc, b.X, b.Y, b.Width, b.Height, 0x00CC0020);
                SelectObject(memDc, old);
                using (var img = Image.FromHbitmap(bmp))
                    for (int x = 0; x < W; x++)
                        for (int y = 0; y < H; y++)
                            Cells[x, y] = img.GetPixel(x, y);
            }
            catch { }
            finally
            {
                DeleteObject(bmp); DeleteDC(memDc); ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr h);
        [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr h, IntPtr dc);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int w, int h);
        [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr o);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr o);
        [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern int SetStretchBltMode(IntPtr dc, int mode);
        [DllImport("gdi32.dll")] static extern bool SetBrushOrgEx(IntPtr dc, int x, int y, IntPtr prev);
        [DllImport("gdi32.dll")] static extern bool StretchBlt(IntPtr dst, int x, int y, int w, int h, IntPtr src, int sx, int sy, int sw, int sh, int rop);
    }

    // CPU load, memory load, battery, idle time.
    static class SystemStats
    {
        static long lastIdle, lastTotal;
        static double cpu;
        static readonly Stopwatch cpuClock = Stopwatch.StartNew();

        public static double Cpu()
        {
            if (cpuClock.ElapsedMilliseconds < 500 && lastTotal != 0) return cpu;
            cpuClock.Restart();
            long idle, kernel, user;
            if (!GetSystemTimes(out idle, out kernel, out user)) return cpu;
            long total = kernel + user; // kernel time includes idle time
            if (lastTotal != 0 && total > lastTotal)
                cpu = 1.0 - (double)(idle - lastIdle) / (total - lastTotal);
            lastIdle = idle; lastTotal = total;
            return cpu = Math.Max(0, Math.Min(1, cpu));
        }

        public static double Ram()
        {
            var m = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX)) };
            return GlobalMemoryStatusEx(ref m) ? m.dwMemoryLoad / 100.0 : 0;
        }

        public static double Battery()
        {
            float p = SystemInformation.PowerStatus.BatteryLifePercent;
            return p > 1 ? 1 : p;
        }

        public static bool OnBattery { get { return SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Offline; } }

        public static double IdleSeconds()
        {
            var li = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO)) };
            if (!GetLastInputInfo(ref li)) return 0;
            return unchecked((uint)Environment.TickCount - li.dwTime) / 1000.0;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MEMORYSTATUSEX { public uint dwLength, dwMemoryLoad; public ulong ullTotalPhys, ullAvailPhys, ullTotalPageFile, ullAvailPageFile, ullTotalVirtual, ullAvailVirtual, ullAvailExtendedVirtual; }
        [StructLayout(LayoutKind.Sequential)] struct LASTINPUTINFO { public uint cbSize, dwTime; }

        [DllImport("kernel32.dll")] static extern bool GetSystemTimes(out long idle, out long kernel, out long user);
        [DllImport("kernel32.dll")] static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX m);
        [DllImport("user32.dll")] static extern bool GetLastInputInfo(ref LASTINPUTINFO li);
    }

    // Global low-level keyboard hook used only to light up the key that was pressed (nothing is recorded).
    sealed class KeyboardHook : IDisposable
    {
        public event Action<byte[]> KeyDown;
        readonly HookProc proc;
        IntPtr hook;

        // Scan code (+0x100 when extended) -> light ids on the Area-51m R2 keyboard.
        static readonly Dictionary<int, byte[]> ScanToLights = BuildMap();

        public KeyboardHook()
        {
            proc = Callback;
            using (var p = Process.GetCurrentProcess())
            using (var m = p.MainModule)
                hook = SetWindowsHookEx(13, proc, GetModuleHandle(m.ModuleName), 0);
        }

        IntPtr Callback(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0 && (wParam == (IntPtr)0x100 || wParam == (IntPtr)0x104))
            {
                int scan = Marshal.ReadInt32(lParam, 4), flags = Marshal.ReadInt32(lParam, 8);
                byte[] ids;
                if (ScanToLights.TryGetValue(scan | ((flags & 1) != 0 ? 0x100 : 0), out ids) || ScanToLights.TryGetValue(scan, out ids))
                {
                    var h = KeyDown; if (h != null) h(ids);
                }
            }
            return CallNextHookEx(hook, code, wParam, lParam);
        }

        public void Dispose() { if (hook != IntPtr.Zero) { UnhookWindowsHookEx(hook); hook = IntPtr.Zero; } }

        static Dictionary<int, byte[]> BuildMap()
        {
            var m = new Dictionary<int, byte[]>();
            Action<int, int[]> add = (scan, ids) => m[scan] = Array.ConvertAll(ids, i => (byte)i);
            add(0x01, new[] { 0 });
            for (int i = 0; i < 10; i++) add(0x3B + i, new[] { 1 + i });
            add(0x57, new[] { 11 }); add(0x58, new[] { 12 });
            add(0x147, new[] { 13 }); add(0x14F, new[] { 14 }); add(0x153, new[] { 15 });
            add(0x29, new[] { 20 });
            for (int i = 0; i < 12; i++) add(0x02 + i, new[] { 21 + i });          // 1..0 - =
            add(0x0E, new[] { 33, 34, 35 });
            add(0x45, new[] { 36 }); add(0x145, new[] { 36 }); add(0x135, new[] { 37 }); add(0x37, new[] { 38 }); add(0x4A, new[] { 39 });
            add(0x0F, new[] { 40, 41 });
            for (int i = 0; i < 12; i++) add(0x10 + i, new[] { 42 + i });          // Q..]
            add(0x2B, new[] { 54, 55 });
            add(0x47, new[] { 56 }); add(0x48, new[] { 57 }); add(0x49, new[] { 58 }); add(0x4E, new[] { 59, 79 });
            add(0x3A, new[] { 60, 61 });
            for (int i = 0; i < 11; i++) add(0x1E + i, new[] { 62 + i });          // A..'
            add(0x1C, new[] { 73, 74, 75 });
            add(0x4B, new[] { 76 }); add(0x4C, new[] { 77 }); add(0x4D, new[] { 78 });
            add(0x2A, new[] { 80, 81, 82 });
            for (int i = 0; i < 10; i++) add(0x2C + i, new[] { 83 + i });          // Z../
            add(0x36, new[] { 93, 94, 95 }); add(0x136, new[] { 93, 94, 95 });
            add(0x4F, new[] { 96 }); add(0x50, new[] { 97 }); add(0x51, new[] { 98 }); add(0x11C, new[] { 99, 119 });
            add(0x1D, new[] { 100 }); add(0x15B, new[] { 102 }); add(0x38, new[] { 103 });
            add(0x39, new[] { 104, 105, 106, 107, 108, 109, 110 });
            add(0x138, new[] { 111 }); add(0x11D, new[] { 112 });
            add(0x149, new[] { 113 }); add(0x148, new[] { 114 }); add(0x151, new[] { 115 });
            add(0x52, new[] { 116, 117 }); add(0x53, new[] { 118 });
            add(0x14B, new[] { 133 }); add(0x150, new[] { 134 }); add(0x14D, new[] { 135 });
            return m;
        }

        delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true)] static extern IntPtr SetWindowsHookEx(int id, HookProc proc, IntPtr mod, uint thread);
        [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr h);
        [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr h, int code, IntPtr w, IntPtr l);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr GetModuleHandle(string name);
    }
}
