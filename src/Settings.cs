using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace AlienKeyboardLights
{
    enum Mode
    {
        Solid, Gradient, Fade, Breathe, Wave, Rainbow, Blink, Twinkle, Stripes,
        Radial, Starfield, Scanner, Rain, Ripple, Music, Ambient, CpuMeter, RamMeter, BatteryMeter, Paint
    }

    enum ChassisMode { FollowKeyboard, Accent, Off, Untouched }

    class Theme
    {
        public string Name; public Mode Mode; public int Speed; public Color[] Colors;
        public Theme(string name, Mode mode, int speed, params int[] rgb)
        {
            Name = name; Mode = mode; Speed = speed;
            Colors = rgb.Select(c => Color.FromArgb(255, Color.FromArgb(c))).ToArray();
        }
    }

    // All user settings. Lighting fields are what a profile stores; the rest are global preferences.
    class Settings
    {
        // Lighting (saved in profiles)
        public Mode Mode = Mode.Wave;
        public int Brightness = 100;   // 0..100
        public int Speed = 40;         // 1..100
        public List<Color> Colors = new List<Color> { Color.FromArgb(0, 200, 255), Color.FromArgb(140, 0, 255) };
        public Color Accent = Color.White;
        public int[] Paint = new int[LightEngine.Keys.Length];
        public bool TypingGlow, Wasd, Locks;
        public ChassisMode Chassis = ChassisMode.FollowKeyboard;

        // Global
        public bool LightsOn = true;
        public int IdleMinutes = 0;
        public bool BatteryDim;
        public int BatteryLevel = 30;
        public bool NightOn;
        public int NightStart = 23 * 60, NightEnd = 7 * 60;
        public bool Hotkeys = true;
        public bool AutoProfiles = true;

        public int Version;            // bumped on every change

        public static string Folder
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AlienKeyboardLights"); }
        }
        static string FilePath { get { return Path.Combine(Folder, "settings.txt"); } }

        public Settings Clone()
        {
            var s = (Settings)MemberwiseClone();
            s.Colors = new List<Color>(Colors);
            s.Paint = (int[])Paint.Clone();
            return s;
        }

        public void CopyLightingFrom(Settings o)
        {
            Mode = o.Mode; Brightness = o.Brightness; Speed = o.Speed; Colors = new List<Color>(o.Colors);
            Accent = o.Accent; Paint = (int[])o.Paint.Clone(); TypingGlow = o.TypingGlow; Wasd = o.Wasd; Locks = o.Locks;
            Chassis = o.Chassis;
        }

        static string Hex(Color c) { return (c.ToArgb() & 0xFFFFFF).ToString("X6"); }
        static Color FromHex(string h) { return Color.FromArgb(255, Color.FromArgb(Convert.ToInt32(h, 16))); }

        public List<string> ToLines(bool lightingOnly)
        {
            var l = new List<string> {
                "mode=" + Mode, "brightness=" + Brightness, "speed=" + Speed,
                "colors=" + string.Join(",", Colors.Select(Hex)), "accent=" + Hex(Accent),
                "paint=" + string.Join(",", Paint.Select(p => p.ToString("X6"))),
                "typing=" + TypingGlow, "wasd=" + Wasd, "locks=" + Locks, "chassis=" + Chassis
            };
            if (!lightingOnly)
                l.AddRange(new[] {
                    "lightson=" + LightsOn, "idle=" + IdleMinutes, "batterydim=" + BatteryDim, "batterylevel=" + BatteryLevel,
                    "night=" + NightOn, "nightstart=" + NightStart, "nightend=" + NightEnd, "hotkeys=" + Hotkeys, "autoprofiles=" + AutoProfiles
                });
            return l;
        }

        public void FromLines(IEnumerable<string> lines, bool lightingOnly)
        {
            foreach (var line in lines)
            {
                int eq = line.IndexOf('='); if (eq < 0) continue;
                string k = line.Substring(0, eq), v = line.Substring(eq + 1);
                try
                {
                    switch (k)
                    {
                        case "mode": Enum.TryParse(v, out Mode); break;
                        case "brightness": Brightness = Clamp(int.Parse(v), 0, 100); break;
                        case "speed": Speed = Clamp(int.Parse(v), 1, 100); break;
                        case "colors":
                            var list = v.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(FromHex).ToList();
                            if (list.Count > 0) Colors = list;
                            break;
                        case "accent": Accent = FromHex(v); break;
                        case "paint":
                            var p = v.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            for (int i = 0; i < p.Length && i < Paint.Length; i++) Paint[i] = Convert.ToInt32(p[i], 16);
                            break;
                        case "typing": TypingGlow = bool.Parse(v); break;
                        case "wasd": Wasd = bool.Parse(v); break;
                        case "locks": Locks = bool.Parse(v); break;
                        case "chassis": Enum.TryParse(v, out Chassis); break;
                    }
                    if (lightingOnly) continue;
                    switch (k)
                    {
                        case "lightson": LightsOn = bool.Parse(v); break;
                        case "idle": IdleMinutes = Clamp(int.Parse(v), 0, 240); break;
                        case "batterydim": BatteryDim = bool.Parse(v); break;
                        case "batterylevel": BatteryLevel = Clamp(int.Parse(v), 0, 100); break;
                        case "night": NightOn = bool.Parse(v); break;
                        case "nightstart": NightStart = Clamp(int.Parse(v), 0, 1439); break;
                        case "nightend": NightEnd = Clamp(int.Parse(v), 0, 1439); break;
                        case "hotkeys": Hotkeys = bool.Parse(v); break;
                        case "autoprofiles": AutoProfiles = bool.Parse(v); break;
                    }
                }
                catch { }
            }
        }

        static int Clamp(int v, int lo, int hi) { return Math.Max(lo, Math.Min(hi, v)); }

        public void Save()
        {
            try { Directory.CreateDirectory(Folder); File.WriteAllLines(FilePath, ToLines(false)); } catch { }
        }

        public void Load()
        {
            try { if (File.Exists(FilePath)) FromLines(File.ReadAllLines(FilePath), false); } catch { }
        }
    }

    // A named lighting setup, optionally activated automatically when an app is in the foreground.
    class Profile
    {
        public string Name, App = "";
        public Settings Lighting = new Settings();

        static string Dir { get { return Path.Combine(Settings.Folder, "profiles"); } }

        public static List<Profile> LoadAll()
        {
            var list = new List<Profile>();
            try
            {
                if (!Directory.Exists(Dir)) return list;
                foreach (var f in Directory.GetFiles(Dir, "*.txt"))
                {
                    var lines = File.ReadAllLines(f);
                    var p = new Profile { Name = Path.GetFileNameWithoutExtension(f) };
                    p.Lighting.FromLines(lines, true);
                    var app = lines.FirstOrDefault(l => l.StartsWith("app="));
                    if (app != null) p.App = app.Substring(4);
                    list.Add(p);
                }
            }
            catch { }
            return list.OrderBy(p => p.Name).ToList();
        }

        public void Save()
        {
            Directory.CreateDirectory(Dir);
            var lines = Lighting.ToLines(true);
            lines.Add("app=" + App);
            File.WriteAllLines(Path.Combine(Dir, Name + ".txt"), lines);
        }

        public void Delete() { try { File.Delete(Path.Combine(Dir, Name + ".txt")); } catch { } }

        public override string ToString() { return string.IsNullOrEmpty(App) ? Name : Name + "   (auto: " + App + ")"; }
    }
}
