using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace AlienKeyboardLights
{
    // Computes the frames and streams them to the keyboard and chassis lights on a background thread.
    class LightEngine
    {
        public struct Key { public byte Id; public double X, Y; public int Col, Row; }

        // Area-51m R2 key ids form a 20-column grid (rows of 20 ids), plus the arrow row at 133-135.
        public static readonly Key[] Keys = BuildKeys();
        static readonly byte[] KeyIds = Keys.Select(k => k.Id).ToArray();
        static readonly int[] IndexOfId = BuildIndex();
        public const int ChassisCount = 5;
        static readonly byte[] ChassisIds = { 0, 1, 2, 3, 4 };
        static readonly byte[] WasdIds = { 43, 62, 63, 64, 114, 133, 134, 135 };

        public readonly int[] Frame = new int[Keys.Length];          // what is shown (after dimming)
        public readonly int[] ChassisFrame = new int[ChassisCount];
        public volatile string Status = L.T("Procurando teclado...");
        public volatile string Info = "";
        public volatile bool CapsOn, NumOn;      // updated by the UI thread
        public event Action FrameReady;

        Settings settings;
        readonly object settingsLock;
        readonly Random rng = new Random();
        readonly Color[] work = new Color[Keys.Length];
        readonly ConcurrentQueue<byte[]> presses = new ConcurrentQueue<byte[]>();
        readonly double[] pressTime = new double[Keys.Length];
        readonly List<double[]> ripples = new List<double[]>(); // x, y, start, colorIndex
        readonly double[] twinkleStart = new double[Keys.Length], starLevel = new double[Keys.Length];
        readonly int[] twinkleA = new int[Keys.Length], twinkleB = new int[Keys.Length];
        readonly Color[] starColor = new Color[Keys.Length];
        readonly double[] rainHead = new double[20], rainSpeed = new double[20];
        readonly int[] rainColor = new int[20];
        readonly double[] musicHistory = new double[20];
        readonly AudioMeter audio = new AudioMeter();
        readonly ScreenSampler screen = new ScreenSampler();
        double musicGain = 0.3, musicShift, lastAmbient = -1;
        int rippleColor;

        AlienFxV5 keyboard;
        AlienFxV4 chassis;
        double nextKeyboardTry, nextChassisTry;
        Thread thread;
        volatile bool running, forceSend;
        double dim = 1;

        public LightEngine(Settings s, object lockObj) { settings = s; settingsLock = lockObj; }

        static Key[] BuildKeys()
        {
            var keys = new List<Key>();
            for (int id = 0; id < 120; id++) keys.Add(new Key { Id = (byte)id, Col = id % 20, Row = id / 20 });
            for (int id = 133; id <= 135; id++) keys.Add(new Key { Id = (byte)id, Col = id - 120, Row = 6 });
            return keys.Select(k => { k.X = k.Col / 19.0; k.Y = k.Row / 6.0; return k; }).ToArray();
        }

        static int[] BuildIndex()
        {
            var idx = Enumerable.Repeat(-1, 256).ToArray();
            for (int i = 0; i < Keys.Length; i++) idx[Keys[i].Id] = i;
            return idx;
        }

        public static int KeyAt(int col, int row)
        {
            for (int i = 0; i < Keys.Length; i++) if (Keys[i].Col == col && Keys[i].Row == row) return i;
            return -1;
        }

        public void Start()
        {
            running = true;
            thread = new Thread(Loop) { IsBackground = true, Name = "LightEngine" };
            thread.SetApartmentState(ApartmentState.MTA);
            thread.Start();
        }

        public void Stop()
        {
            running = false;
            if (thread != null) thread.Join(1500);
            if (keyboard != null) keyboard.Dispose();
            if (chassis != null) chassis.Dispose();
        }

        public void Resend() { forceSend = true; }
        public void KeyPressed(byte[] ids) { presses.Enqueue(ids); }

        void Loop()
        {
            var clock = Stopwatch.StartNew();
            int[] sentKeys = null, sentChassis = null;
            int sentBrightness = -1, sentChassisBrightness = -1;
            double lastKeySend = -10, lastChassisSend = -10, lastT = 0;
            while (running)
            {
                double t = clock.Elapsed.TotalSeconds, dt = Math.Min(0.2, t - lastT);
                lastT = t;
                Settings s;
                lock (settingsLock) s = settings.Clone();
                if (s.Colors.Count == 0) s.Colors.Add(Color.Black);

                try
                {
                    Render(s, t, dt);
                    ApplyOverlays(s, t);
                }
                catch (Exception ex) { Info = L.T("Erro no efeito: ") + ex.Message; }

                // Smooth fade towards the target dimming (off / night / idle / battery).
                double target = TargetDim(s);
                dim += Math.Max(-dt * 2, Math.Min(dt * 2, target - dim));
                for (int i = 0; i < Keys.Length; i++) Frame[i] = Pack(Scale(work[i], dim));
                ComputeChassis(s);

                var h = FrameReady; if (h != null) h();

                bool force = forceSend; forceSend = false;
                if (force) { sentKeys = null; sentChassis = null; sentBrightness = sentChassisBrightness = -1; }

                // Keyboard: send when the frame changed, plus a periodic refresh in case something else overwrote it.
                if ((sentKeys == null || !sentKeys.SequenceEqual(Frame) || t - lastKeySend > 3) && EnsureKeyboard(t))
                {
                    bool ok = true;
                    if (s.Brightness != sentBrightness) { ok &= keyboard.SetBrightness((byte)(s.Brightness * 255 / 100)); sentBrightness = s.Brightness; }
                    var copy = (int[])Frame.Clone();
                    ok &= keyboard.SetColors(KeyIds, copy);
                    if (ok) { sentKeys = copy; lastKeySend = t; Status = L.T("Teclado conectado") + (chassis != null ? L.T(" · chassi conectado") : ""); }
                    else { keyboard.Dispose(); keyboard = null; sentBrightness = -1; sentKeys = null; Status = L.T("Teclado parou de responder, reconectando..."); }
                }

                // Chassis: at most ~12 updates per second.
                if (s.Chassis != ChassisMode.Untouched && t - lastChassisSend > 0.08
                    && (sentChassis == null || !sentChassis.SequenceEqual(ChassisFrame) || t - lastChassisSend > 3) && EnsureChassis(t))
                {
                    bool ok = true;
                    if (s.Brightness != sentChassisBrightness) { ok &= chassis.SetBrightness(s.Brightness); sentChassisBrightness = s.Brightness; }
                    var copy = (int[])ChassisFrame.Clone();
                    ok &= chassis.SetColors(ChassisIds, copy);
                    if (ok) { sentChassis = copy; lastChassisSend = t; }
                    else { chassis.Dispose(); chassis = null; sentChassis = null; sentChassisBrightness = -1; }
                }

                Thread.Sleep(30);
            }
        }

        bool EnsureKeyboard(double t)
        {
            if (keyboard != null) return true;
            if (t < nextKeyboardTry) return false;
            nextKeyboardTry = t + 2;
            try { keyboard = AlienFxV5.Open(); } catch { keyboard = null; }
            if (keyboard == null) { Status = L.T("Teclado AlienFX não encontrado"); return false; }
            try { keyboard.ClearHardwareEffect(); } catch { }
            return true;
        }

        bool EnsureChassis(double t)
        {
            if (chassis != null) return true;
            if (t < nextChassisTry) return false;
            nextChassisTry = t + 5;
            try { chassis = AlienFxV4.Open(); } catch { chassis = null; }
            return chassis != null;
        }

        double TargetDim(Settings s)
        {
            if (!s.LightsOn) return 0;
            if (s.NightOn)
            {
                var now = DateTime.Now; int m = now.Hour * 60 + now.Minute;
                bool inside = s.NightStart <= s.NightEnd ? m >= s.NightStart && m < s.NightEnd : m >= s.NightStart || m < s.NightEnd;
                if (inside) return 0;
            }
            if (s.IdleMinutes > 0 && SystemStats.IdleSeconds() > s.IdleMinutes * 60) return 0;
            if (s.BatteryDim && SystemStats.OnBattery) return s.BatteryLevel / 100.0;
            return 1;
        }

        // ---------- effects ----------

        void Render(Settings s, double t, double dt)
        {
            var pal = s.Colors.ToArray();
            int n = pal.Length;
            double speed = 0.03 + s.Speed / 100.0 * 1.2;
            Info = "";

            // Key presses (for ripple and typing glow)
            byte[] ids;
            while (presses.TryDequeue(out ids))
                foreach (var id in ids)
                {
                    int i = IndexOfId[id]; if (i < 0) continue;
                    pressTime[i] = t;
                    if (s.Mode == Mode.Ripple && id == ids[0])
                        ripples.Add(new double[] { Keys[i].Col, Keys[i].Row, t, rippleColor++ % n });
                }

            switch (s.Mode)
            {
                case Mode.Music: RenderMusic(pal, t, dt, speed); return;
                case Mode.Ambient: RenderAmbient(t); return;
                case Mode.CpuMeter: { double v = SystemStats.Cpu(); Info = "CPU " + (int)(v * 100) + "%"; RenderGauge(pal, v); } return;
                case Mode.RamMeter: { double v = SystemStats.Ram(); Info = "RAM " + (int)(v * 100) + "%"; RenderGauge(pal, v); } return;
                case Mode.BatteryMeter: { double v = SystemStats.Battery(); Info = L.T("Bateria ") + (int)(v * 100) + "%"; RenderGauge(pal, v); } return;
                case Mode.Rain: RenderRain(pal, t, dt, speed); return;
                case Mode.Starfield: RenderStars(pal, dt, speed); return;
            }

            double scanPos = 0; int scanPass = 0;
            if (s.Mode == Mode.Scanner)
            {
                double p = t * speed * 0.7;
                scanPass = (int)Math.Floor(p);
                double f = p - scanPass;
                scanPos = (scanPass % 2 == 0) ? f : 1 - f;
                scanPos = scanPos * 1.2 - 0.1;
            }
            if (s.Mode == Mode.Ripple) ripples.RemoveAll(r => t - r[2] > 2.5);

            for (int i = 0; i < Keys.Length; i++)
            {
                var k = Keys[i];
                Color c;
                switch (s.Mode)
                {
                    case Mode.Gradient: c = SampleLinear(pal, k.X); break;
                    case Mode.Fade: c = SampleCyclic(pal, t * speed * 0.5); break;
                    case Mode.Breathe:
                        {
                            double p = t * speed * 0.6;
                            double v = Math.Sin(Math.PI * (p - Math.Floor(p)));
                            c = Scale(pal[(int)Math.Floor(p) % n], v * v);
                        }
                        break;
                    case Mode.Wave: c = SampleCyclic(pal, k.X * 0.8 + k.Y * 0.15 - t * speed * 0.6); break;
                    case Mode.Rainbow: c = Hsv((k.X * 0.8 - t * speed * 0.4) * 360, 1, 1); break;
                    case Mode.Blink: c = pal[(int)Math.Floor(t * speed * 2) % n]; break;
                    case Mode.Twinkle: c = Twinkle(i, pal, t, speed); break;
                    case Mode.Stripes: c = pal[((k.Col + k.Row + (int)Math.Floor(t * speed * 3)) % n + n) % n]; break;
                    case Mode.Radial:
                        {
                            double dx = k.Col - 9.5, dy = (k.Row - 3) * 1.4;
                            c = SampleCyclic(pal, Math.Sqrt(dx * dx + dy * dy) / 12 - t * speed * 0.6);
                        }
                        break;
                    case Mode.Scanner:
                        {
                            double d = Math.Abs(k.X - scanPos);
                            double v = Math.Max(0, 1 - d * 7);
                            c = Scale(pal[scanPass % n], 0.04 + 0.96 * v * v);
                        }
                        break;
                    case Mode.Ripple:
                        {
                            c = Scale(pal[0], 0.05);
                            foreach (var r in ripples)
                            {
                                double age = t - r[2];
                                double radius = age * (6 + speed * 14);
                                double dx = k.Col - r[0], dy = (k.Row - r[1]) * 1.3;
                                double ring = Math.Max(0, 1 - Math.Abs(Math.Sqrt(dx * dx + dy * dy) - radius) / 1.6);
                                double life = Math.Max(0, 1 - age / 2.5);
                                c = Max(c, Scale(pal[(int)r[3] % n], ring * life));
                            }
                        }
                        break;
                    case Mode.Paint: c = Color.FromArgb(s.Paint[i]); break;
                    default: c = pal[0]; break;
                }
                work[i] = c;
            }
        }

        // Each key fades to a new random palette color at random moments, like string lights.
        Color Twinkle(int i, Color[] pal, double t, double speed)
        {
            int n = pal.Length;
            if (twinkleA[i] >= n || twinkleB[i] >= n || twinkleStart[i] == 0)
            {
                twinkleA[i] = rng.Next(n); twinkleB[i] = rng.Next(n);
                twinkleStart[i] = t + 0.001 + rng.NextDouble() * 2;
            }
            double fade = 0.15 + 0.6 / (speed + 0.2);
            double p = (t - twinkleStart[i]) / fade;
            if (p >= 1)
            {
                twinkleA[i] = twinkleB[i];
                if (rng.NextDouble() < 0.02 + speed * 0.05)
                {
                    int next = rng.Next(n);
                    if (n > 1 && next == twinkleA[i]) next = (next + 1 + rng.Next(n - 1)) % n;
                    twinkleB[i] = next; twinkleStart[i] = t;
                }
                return pal[twinkleA[i]];
            }
            return p < 0 ? pal[twinkleA[i]] : Lerp(pal[twinkleA[i]], pal[twinkleB[i]], p);
        }

        // Dark sky; random keys flash in palette colors and fade out.
        void RenderStars(Color[] pal, double dt, double speed)
        {
            for (int i = 0; i < Keys.Length; i++)
            {
                starLevel[i] = Math.Max(0, starLevel[i] - dt * (0.6 + speed));
                if (starLevel[i] == 0 && rng.NextDouble() < dt * (0.05 + speed * 0.25))
                {
                    starLevel[i] = 1; starColor[i] = pal[rng.Next(pal.Length)];
                }
                work[i] = Scale(starColor[i], starLevel[i] * starLevel[i]);
            }
        }

        // Falling drops per column with a fading trail ("Matrix").
        void RenderRain(Color[] pal, double t, double dt, double speed)
        {
            for (int col = 0; col < 20; col++)
            {
                if (rainSpeed[col] == 0 || rainHead[col] > 11)
                {
                    rainHead[col] = -rng.NextDouble() * 8;
                    rainSpeed[col] = 0.6 + rng.NextDouble() * 0.8;
                    rainColor[col] = rng.Next(pal.Length);
                }
                rainHead[col] += dt * rainSpeed[col] * (2 + speed * 10);
            }
            for (int i = 0; i < Keys.Length; i++)
            {
                var k = Keys[i];
                double d = rainHead[k.Col] - k.Row;
                Color baseC = pal[rainColor[k.Col]];
                if (d < 0 || d > 4) work[i] = Scale(baseC, 0.02);
                else if (d < 1) work[i] = Lerp(baseC, Color.White, 0.55 * (1 - d));
                else work[i] = Scale(baseC, Math.Pow(1 - (d - 1) / 3, 2));
            }
        }

        // Volume history spreading from the center like an equalizer.
        void RenderMusic(Color[] pal, double t, double dt, double speed)
        {
            float peak = audio.Peak();
            musicGain = Math.Max(0.05, Math.Max(peak, musicGain - dt * 0.05)); // auto gain with slow decay
            double level = Math.Min(1, peak / musicGain);
            musicShift += dt * (4 + speed * 20);
            while (musicShift >= 1)
            {
                musicShift -= 1;
                for (int c = 0; c < 9; c++) { musicHistory[c] = musicHistory[c + 1]; musicHistory[19 - c] = musicHistory[18 - c]; }
            }
            musicHistory[9] = musicHistory[10] = Math.Max(level, musicHistory[9] * 0.7);
            Info = L.T("Áudio ") + (int)(peak * 100) + "%";
            for (int i = 0; i < Keys.Length; i++)
            {
                var k = Keys[i];
                double height = musicHistory[k.Col] * 6;            // rows lit from the bottom
                double rowFromBottom = k.Row >= 5 ? 0 : 5 - k.Row;
                double v = Math.Max(0, Math.Min(1, height - rowFromBottom));
                work[i] = Scale(SampleLinear(pal, rowFromBottom / 5), 0.04 + 0.96 * v);
            }
        }

        void RenderAmbient(double t)
        {
            if (t - lastAmbient > 0.066) { screen.Sample(); lastAmbient = t; }
            for (int i = 0; i < Keys.Length; i++)
            {
                var k = Keys[i];
                work[i] = Saturate(screen.Cells[k.Col, Math.Min(k.Row, ScreenSampler.H - 1)], 1.4);
            }
        }

        // Horizontal fill bar; color follows the palette from left (low) to right (high).
        void RenderGauge(Color[] pal, double value)
        {
            double fill = value * 20;
            for (int i = 0; i < Keys.Length; i++)
            {
                var k = Keys[i];
                double v = Math.Max(0, Math.Min(1, fill - k.Col));
                work[i] = Scale(SampleLinear(pal, k.X), 0.06 + 0.94 * v);
            }
        }

        void ApplyOverlays(Settings s, double t)
        {
            if (s.Wasd)
                foreach (var id in WasdIds) work[IndexOfId[id]] = s.Accent;
            if (s.Locks)
            {
                if (CapsOn) { work[IndexOfId[60]] = s.Accent; work[IndexOfId[61]] = s.Accent; }
                if (NumOn) work[IndexOfId[36]] = s.Accent;
            }
            if (s.TypingGlow)
                for (int i = 0; i < Keys.Length; i++)
                {
                    double age = t - pressTime[i];
                    if (pressTime[i] > 0 && age < 1.2) work[i] = Lerp(work[i], s.Accent, 1 - age / 1.2);
                }
        }

        // Alien head and power button = average of the keyboard; rings = left/right halves; touchpad = bottom rows.
        void ComputeChassis(Settings s)
        {
            Color head, pad, ringTop, ringBottom;
            switch (s.Chassis)
            {
                case ChassisMode.Accent: head = pad = ringTop = ringBottom = s.Accent; break;
                case ChassisMode.Off: case ChassisMode.Untouched: head = pad = ringTop = ringBottom = Color.Black; break;
                default:
                    head = Average(k => true);
                    pad = Average(k => k.Row >= 4 && k.Col >= 4 && k.Col <= 12);
                    ringTop = Average(k => k.Col < 10);
                    ringBottom = Average(k => k.Col >= 10);
                    break;
            }
            ChassisFrame[0] = Pack(Scale(head, dim));
            ChassisFrame[1] = Pack(Scale(pad, dim));
            ChassisFrame[2] = Pack(Scale(head, dim));
            ChassisFrame[3] = Pack(Scale(ringTop, dim));
            ChassisFrame[4] = Pack(Scale(ringBottom, dim));
        }

        Color Average(Func<Key, bool> filter)
        {
            int r = 0, g = 0, b = 0, cnt = 0, max = 0;
            for (int i = 0; i < Keys.Length; i++)
                if (filter(Keys[i])) { r += work[i].R; g += work[i].G; b += work[i].B; cnt++; }
            if (cnt == 0) return Color.Black;
            r /= cnt; g /= cnt; b /= cnt;
            max = Math.Max(r, Math.Max(g, b));
            // Keep hue but lift it so the average of a colorful keyboard is not muddy.
            return max == 0 ? Color.Black : Scale(Color.FromArgb(r * 255 / max, g * 255 / max, b * 255 / max), Math.Min(1, max / 160.0));
        }

        // ---------- color helpers ----------

        static int Pack(Color c) { return (c.R << 16) | (c.G << 8) | c.B; }

        static Color SampleLinear(Color[] pal, double x)
        {
            if (pal.Length == 1) return pal[0];
            double p = Math.Max(0, Math.Min(1, x)) * (pal.Length - 1);
            int a = Math.Min((int)p, pal.Length - 2);
            return Lerp(pal[a], pal[a + 1], p - a);
        }

        static Color SampleCyclic(Color[] pal, double x)
        {
            int n = pal.Length;
            double p = (x - Math.Floor(x)) * n;
            int a = (int)p % n;
            return Lerp(pal[a], pal[(a + 1) % n], p - Math.Floor(p));
        }

        public static Color Lerp(Color a, Color b, double f)
        {
            f = Math.Max(0, Math.Min(1, f));
            f = f * f * (3 - 2 * f); // smoothstep
            return Color.FromArgb((int)(a.R + (b.R - a.R) * f), (int)(a.G + (b.G - a.G) * f), (int)(a.B + (b.B - a.B) * f));
        }

        static Color Scale(Color c, double f)
        {
            f = Math.Max(0, Math.Min(1, f));
            return Color.FromArgb((int)(c.R * f), (int)(c.G * f), (int)(c.B * f));
        }

        static Color Max(Color a, Color b) { return Color.FromArgb(Math.Max(a.R, b.R), Math.Max(a.G, b.G), Math.Max(a.B, b.B)); }

        static Color Saturate(Color c, double amount)
        {
            double gray = (c.R + c.G + c.B) / 3.0;
            Func<double, int> f = v => (int)Math.Max(0, Math.Min(255, gray + (v - gray) * amount));
            return Color.FromArgb(f(c.R), f(c.G), f(c.B));
        }

        static Color Hsv(double h, double s, double v)
        {
            h = ((h % 360) + 360) % 360;
            double c = v * s, x = c * (1 - Math.Abs(h / 60 % 2 - 1)), m = v - c, r, g, b;
            if (h < 60) { r = c; g = x; b = 0; } else if (h < 120) { r = x; g = c; b = 0; }
            else if (h < 180) { r = 0; g = c; b = x; } else if (h < 240) { r = 0; g = x; b = c; }
            else if (h < 300) { r = x; g = 0; b = c; } else { r = c; g = 0; b = x; }
            return Color.FromArgb((int)((r + m) * 255), (int)((g + m) * 255), (int)((b + m) * 255));
        }
    }
}
