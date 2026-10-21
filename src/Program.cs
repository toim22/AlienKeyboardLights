using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace AlienKeyboardLights
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var saved = new Settings();
            saved.Load();
            L.Set(saved.Language);

            bool created;
            using (var mutex = new Mutex(true, "AlienKeyboardLights_SingleInstance", out created))
            {
                if (!created) { MessageBox.Show(L.T("O Alien Keyboard Lights já está aberto (veja a bandeja do sistema)."), "Alien Keyboard Lights"); return; }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                bool hidden = args.Contains("--tray");
                // Changing the language closes the window and builds a new one in the same process.
                do
                {
                    MainForm.RestartRequested = false;
                    Application.Run(new MainForm(hidden));
                    hidden = false;
                } while (MainForm.RestartRequested);
            }
        }
    }
}
