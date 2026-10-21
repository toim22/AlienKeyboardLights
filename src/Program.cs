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
            bool created;
            using (var mutex = new Mutex(true, "AlienKeyboardLights_SingleInstance", out created))
            {
                if (!created) { MessageBox.Show("O Alien Keyboard Lights já está aberto (veja a bandeja do sistema).", "Alien Keyboard Lights"); return; }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm(args.Contains("--tray")));
            }
        }
    }
}
