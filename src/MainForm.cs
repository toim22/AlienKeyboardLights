using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AlienKeyboardLights
{
    class MainForm : Form
    {
        static readonly Theme[] Themes = {
            new Theme("Halloween", Mode.Wave, 30, 0xFF4A00, 0x7A00FF, 0x39FF14),
            new Theme("Natal", Mode.Twinkle, 35, 0xFF0000, 0x00C020, 0xFFB000, 0xFFFFFF),
            new Theme("Ano Novo", Mode.Starfield, 60, 0xFFC000, 0xFFFFFF, 0xFF8A00, 0xB0C4FF),
            new Theme("Carnaval", Mode.Wave, 80, 0xFF00A0, 0xFFE000, 0x00E0FF, 0x7CFF00, 0x9000FF),
            new Theme("Páscoa", Mode.Fade, 25, 0xFF8FCF, 0x8FD8FF, 0xFFF08F, 0xA8FF8F, 0xC88FFF),
            new Theme("Namorados", Mode.Breathe, 30, 0xFF0030, 0xFF4FA0, 0xC00020),
            new Theme("Festa Junina", Mode.Stripes, 25, 0xFF2000, 0xFFB000, 0x0050FF, 0x00C000),
            new Theme("Brasil", Mode.Wave, 35, 0x00B000, 0xFFDD00, 0x0030D0),
            new Theme("4 de Julho", Mode.Stripes, 30, 0xFF0000, 0xFFFFFF, 0x0030FF),
            new Theme("St. Patrick", Mode.Breathe, 30, 0x00FF40, 0x00A030, 0xFFC000),
            new Theme("Ação de Graças", Mode.Fade, 20, 0xFF6A00, 0xC02000, 0xFFB000),
            new Theme("Outubro Rosa", Mode.Breathe, 25, 0xFF4FB0, 0xFF0080),
            new Theme("Alienware", Mode.Wave, 30, 0x00C8FF, 0x8C00FF),
            new Theme("Neon", Mode.Radial, 40, 0xFF00C8, 0x00F0FF, 0x7000FF),
            new Theme("Fogo", Mode.Twinkle, 85, 0xFF1000, 0xFF5000, 0xFFA000),
            new Theme("Oceano", Mode.Wave, 20, 0x0020FF, 0x00C0FF, 0x00FFB0),
            new Theme("Aurora", Mode.Wave, 15, 0x00FF80, 0x00C0C0, 0x8000FF),
            new Theme("Matrix", Mode.Rain, 45, 0x00FF30),
            new Theme("Céu estrelado", Mode.Starfield, 30, 0xFFFFFF, 0x9FD0FF, 0xFFF0A0),
            new Theme("KITT", Mode.Scanner, 45, 0xFF0000),
            new Theme("Música", Mode.Music, 40, 0x00FF40, 0xFFE000, 0xFF0000),
            new Theme("Cinema (tela)", Mode.Ambient, 40, 0xFFFFFF),
            new Theme("Monitor CPU", Mode.CpuMeter, 40, 0x00FF40, 0xFFE000, 0xFF0000),
            new Theme("Arco-íris", Mode.Rainbow, 40, 0xFF0000),
            new Theme("Branco", Mode.Solid, 40, 0xFFFFFF),
        };

        static readonly string[] ModeNames = {
            "Cor fixa", "Gradiente (fixo)", "Transição suave", "Respirar", "Onda", "Arco-íris", "Piscar",
            "Cintilar (pisca-pisca)", "Listras", "Onda radial", "Céu estrelado", "Scanner (KITT)", "Chuva (Matrix)",
            "Ondulação ao digitar", "Música (visualizador)", "Ambiente (cores da tela)", "Medidor de CPU",
            "Medidor de RAM", "Medidor de bateria", "Pintura por tecla"
        };

        static readonly string[] ModeHints = {
            "Todas as teclas com a primeira cor.",
            "As cores se espalham da esquerda para a direita.",
            "O teclado inteiro passa suavemente por cada cor.",
            "Acende e apaga devagar, alternando as cores.",
            "As cores correm pelo teclado como uma onda.",
            "Arco-íris completo em movimento (não usa as cores escolhidas).",
            "Troca de cor de repente, no ritmo da velocidade.",
            "Cada tecla troca de cor aleatoriamente, como pisca-pisca de Natal.",
            "Faixas diagonais com as cores, andando.",
            "Ondas de cor saindo do centro do teclado.",
            "Fundo escuro com teclas piscando como estrelas.",
            "Uma faixa de luz vai e volta (cada passada usa a próxima cor).",
            "Gotas caindo pelas colunas, estilo Matrix.",
            "Cada tecla pressionada solta uma onda de cor.",
            "Equalizador que reage ao som tocando no PC (cores: base → topo).",
            "Copia as cores da tela principal — ótimo para filmes e jogos.",
            "Barra com o uso da CPU (cores da esquerda = baixo, direita = alto).",
            "Barra com o uso da memória RAM.",
            "Barra com o nível da bateria.",
            "Clique ou arraste nas teclas da prévia para pintar. Botão direito apaga.",
        };

        static readonly Color Bg = Color.FromArgb(24, 24, 30), Panel2 = Color.FromArgb(36, 36, 46),
            Fg = Color.FromArgb(230, 230, 240), Dim = Color.FromArgb(160, 160, 180), AccentUi = Color.FromArgb(0, 170, 255);

        readonly Settings settings = new Settings();
        readonly LightEngine engine;
        readonly PreviewPanel preview;
        readonly List<Button> tabButtons = new List<Button>();
        readonly List<Panel> tabPanels = new List<Panel>();

        // Effects tab
        ComboBox modeBox;
        FlowLayoutPanel colorFlow;
        TrackBar brightBar, speedBar;
        Label brightLabel, speedLabel, hintLabel;
        Panel accentSwatch, brushSwatch, paintTools;
        CheckBox typingBox, wasdBox, locksBox;
        Color brush = Color.Red;
        // Chassis tab
        RadioButton[] chassisRadios;
        // Automation tab
        CheckBox idleBox, batteryBox, nightBox, startupBox, hotkeysBox;
        NumericUpDown idleNum, batteryNum;
        DateTimePicker nightStart, nightEnd;
        // Profiles tab
        ListBox profileList;
        TextBox profileName, profileApp;
        CheckBox autoBox;
        // Bottom
        Button powerBtn;
        Label statusLabel;

        readonly NotifyIcon tray;
        KeyboardHook hook;
        List<Profile> profiles = Profile.LoadAll();
        Profile autoProfile;
        Settings beforeAuto;
        int themeIndex = -1;
        bool loading, reallyExit, hiddenStart, balloonShown;

        public MainForm(bool startHidden)
        {
            settings.Load();
            engine = new LightEngine(settings, settings);
            hiddenStart = startHidden;

            Text = "Alien Keyboard Lights";
            Font = new Font("Segoe UI", 9.5f);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Bg; ForeColor = Fg;
            ClientSize = new Size(860, 770);
            MinimumSize = new Size(876, 809);
            StartPosition = FormStartPosition.CenterScreen;
            Icon = MakeIcon();

            preview = new PreviewPanel(engine) { Location = new Point(16, 16), Size = new Size(828, 250), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            preview.KeyPainted += OnKeyPainted;
            Controls.Add(preview);

            string[] tabs = { "Efeitos", "Temas", "Chassi", "Automação", "Perfis" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                var b = MakeButton(tabs[i], 16 + i * 128, 278, 122);
                b.Click += (s, e) => SelectTab(index);
                tabButtons.Add(b); Controls.Add(b);
                var p = new Panel { Location = new Point(16, 324), Size = new Size(828, 380), Visible = false };
                tabPanels.Add(p); Controls.Add(p);
            }
            BuildEffectsTab(tabPanels[0]);
            BuildThemesTab(tabPanels[1]);
            BuildChassisTab(tabPanels[2]);
            BuildAutomationTab(tabPanels[3]);
            BuildProfilesTab(tabPanels[4]);
            SelectTab(0);

            powerBtn = MakeButton("", 16, 718, 150);
            powerBtn.Click += (s, e) => ToggleLights();
            var trayBtn = MakeButton("Minimizar p/ bandeja", 176, 718, 170);
            trayBtn.Click += (s, e) => HideToTray();
            statusLabel = new Label { Location = new Point(360, 726), AutoSize = true, ForeColor = Dim };
            Controls.AddRange(new Control[] { powerBtn, trayBtn, statusLabel });

            tray = new NotifyIcon { Icon = Icon, Text = "Alien Keyboard Lights", Visible = true };
            tray.ContextMenuStrip = BuildTrayMenu();
            tray.DoubleClick += (s, e) => ShowFromTray();

            var uiTimer = new Timer { Interval = 250 };
            uiTimer.Tick += (s, e) => UiTick();
            uiTimer.Start();
            var profileTimer = new Timer { Interval = 1000 };
            profileTimer.Tick += (s, e) => CheckAutoProfile();
            profileTimer.Start();

            SystemEvents.PowerModeChanged += OnPower;

            LoadIntoUi();
            LoadAutomationUi();
            engine.Start();
        }

        // ---------------- tabs ----------------

        void SelectTab(int index)
        {
            for (int i = 0; i < tabPanels.Count; i++)
            {
                tabPanels[i].Visible = i == index;
                tabButtons[i].BackColor = i == index ? Color.FromArgb(0, 90, 140) : Panel2;
            }
        }

        void BuildEffectsTab(Panel p)
        {
            p.Controls.Add(Caption("Efeito", 0, 0));
            modeBox = new ComboBox { Location = new Point(0, 22), Width = 280, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Panel2, ForeColor = Fg, FlatStyle = FlatStyle.Flat, MaxDropDownItems = 20 };
            modeBox.Items.AddRange(ModeNames);
            modeBox.SelectedIndexChanged += (s, e) => { Change(() => settings.Mode = (Mode)modeBox.SelectedIndex); UpdateModeUi(); };
            p.Controls.Add(modeBox);

            p.Controls.Add(Caption("Cores  (clique para editar, botão direito para remover)", 300, 0));
            colorFlow = new FlowLayoutPanel { Location = new Point(300, 20), Size = new Size(528, 44) };
            p.Controls.Add(colorFlow);

            brightLabel = Caption("", 0, 78);
            brightBar = Slider(0, 98, 0, 100);
            brightBar.ValueChanged += (s, e) => { UpdateLabels(); Change(() => settings.Brightness = brightBar.Value); };
            speedLabel = Caption("", 424, 78);
            speedBar = Slider(424, 98, 1, 100);
            speedBar.ValueChanged += (s, e) => { UpdateLabels(); Change(() => settings.Speed = speedBar.Value); };
            p.Controls.AddRange(new Control[] { brightLabel, brightBar, speedLabel, speedBar });

            hintLabel = new Label { Location = new Point(0, 150), Size = new Size(828, 22), ForeColor = AccentUi };
            p.Controls.Add(hintLabel);

            p.Controls.Add(Caption("Cor de destaque", 0, 184));
            accentSwatch = Swatch(0, 206);
            accentSwatch.Click += (s, e) => { var c = PickColor(settings.Accent); if (c.HasValue) { Change(() => settings.Accent = c.Value); accentSwatch.BackColor = c.Value; } };
            p.Controls.Add(accentSwatch);
            typingBox = Check("Teclas acendem ao digitar (na cor de destaque)", 130, 184);
            typingBox.CheckedChanged += (s, e) => { Change(() => settings.TypingGlow = typingBox.Checked); UpdateHook(); };
            wasdBox = Check("Destacar WASD e setas (para jogos)", 130, 210);
            wasdBox.CheckedChanged += (s, e) => Change(() => settings.Wasd = wasdBox.Checked);
            locksBox = Check("Mostrar Caps Lock e Num Lock ligados", 130, 236);
            locksBox.CheckedChanged += (s, e) => Change(() => settings.Locks = locksBox.Checked);
            p.Controls.AddRange(new Control[] { typingBox, wasdBox, locksBox });

            paintTools = new Panel { Location = new Point(0, 280), Size = new Size(828, 60) };
            paintTools.Controls.Add(Caption("Pincel", 0, 0));
            brushSwatch = Swatch(0, 20);
            brushSwatch.BackColor = brush;
            brushSwatch.Click += (s, e) => { var c = PickColor(brush); if (c.HasValue) { brush = c.Value; brushSwatch.BackColor = c.Value; } };
            var fill = MakeButton("Preencher tudo", 60, 22, 140);
            fill.Click += (s, e) => Change(() => { for (int i = 0; i < settings.Paint.Length; i++) settings.Paint[i] = brush.ToArgb() & 0xFFFFFF; });
            var clear = MakeButton("Limpar tudo", 210, 22, 120);
            clear.Click += (s, e) => Change(() => Array.Clear(settings.Paint, 0, settings.Paint.Length));
            var fromEffect = MakeButton("Copiar cores atuais da prévia", 340, 22, 230);
            fromEffect.Click += (s, e) => Change(() => Array.Copy(engine.Frame, settings.Paint, settings.Paint.Length));
            paintTools.Controls.AddRange(new Control[] { brushSwatch, fill, clear, fromEffect });
            p.Controls.Add(paintTools);
        }

        void BuildThemesTab(Panel p)
        {
            var flow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true };
            foreach (var th in Themes) flow.Controls.Add(ThemeButton(th));
            p.Controls.Add(flow);
        }

        void BuildChassisTab(Panel p)
        {
            p.Controls.Add(Caption("Luzes do chassi: cabeça alien, touchpad, botão power e anel de luz (Tron)", 0, 0));
            string[] names = { "Acompanhar as cores do teclado", "Usar a cor de destaque (aba Efeitos)", "Desligadas", "Não controlar (deixar como estão)" };
            chassisRadios = new RadioButton[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                var r = new RadioButton { Text = names[i], Location = new Point(0, 34 + i * 30), AutoSize = true };
                r.CheckedChanged += (s, e) => { if (r.Checked) Change(() => settings.Chassis = (ChassisMode)index); };
                chassisRadios[i] = r; p.Controls.Add(r);
            }
            p.Controls.Add(new Label
            {
                Location = new Point(0, 170), Size = new Size(820, 60), ForeColor = Dim,
                Text = "As luzes do chassi aparecem na parte de baixo da prévia. A intensidade e o desligamento automático valem para elas também."
            });
        }

        void BuildAutomationTab(Panel p)
        {
            idleBox = Check("Apagar as luzes depois de", 0, 2);
            idleNum = Numeric(220, 0, 1, 240, 10);
            p.Controls.Add(Caption("minutos sem usar o PC (acendem ao mexer)", 290, 3));
            batteryBox = Check("Na bateria, reduzir a intensidade para", 0, 42);
            batteryNum = Numeric(290, 40, 0, 100, 30);
            p.Controls.Add(Caption("%", 360, 43));
            nightBox = Check("Modo noturno: desligar das", 0, 82);
            nightStart = TimePicker(220, 80);
            p.Controls.Add(Caption("às", 320, 83));
            nightEnd = TimePicker(346, 80);
            startupBox = Check("Iniciar com o Windows (fica na bandeja)", 0, 132);
            startupBox.Checked = IsStartupEnabled();
            startupBox.CheckedChanged += (s, e) => SetStartup(startupBox.Checked);
            hotkeysBox = Check("Atalhos globais de teclado:", 0, 172);
            p.Controls.Add(new Label
            {
                Location = new Point(20, 198), Size = new Size(800, 110), ForeColor = Dim,
                Text = "Ctrl+Shift+F12  ligar/desligar luzes\nCtrl+Shift+F11  próximo efeito\nCtrl+Shift+F8    próximo tema\n" +
                       "Ctrl+Shift+F9 / F10  diminuir / aumentar intensidade"
            });
            p.Controls.AddRange(new Control[] { idleBox, idleNum, batteryBox, batteryNum, nightBox, nightStart, nightEnd, startupBox, hotkeysBox });

            EventHandler save = (s, e) => SaveAutomation();
            idleBox.CheckedChanged += save; idleNum.ValueChanged += save;
            batteryBox.CheckedChanged += save; batteryNum.ValueChanged += save;
            nightBox.CheckedChanged += save; nightStart.ValueChanged += save; nightEnd.ValueChanged += save;
            hotkeysBox.CheckedChanged += (s, e) => { SaveAutomation(); RegisterHotkeys(); };
        }

        void BuildProfilesTab(Panel p)
        {
            profileList = new ListBox { Location = new Point(0, 0), Size = new Size(400, 330), BackColor = Panel2, ForeColor = Fg, BorderStyle = BorderStyle.FixedSingle, IntegralHeight = false };
            profileList.SelectedIndexChanged += (s, e) =>
            {
                var pr = profileList.SelectedItem as Profile;
                if (pr != null) { profileName.Text = pr.Name; profileApp.Text = pr.App; }
            };
            profileList.DoubleClick += (s, e) => ApplySelectedProfile();
            p.Controls.Add(profileList);

            p.Controls.Add(Caption("Nome do perfil", 420, 0));
            profileName = TextInput(420, 20, 400);
            p.Controls.Add(Caption("Ativar automaticamente com o app (opcional, ex.: Diablo IV.exe)", 420, 56));
            profileApp = TextInput(420, 76, 300);
            var browse = MakeButton("Procurar...", 726, 74, 94);
            browse.Click += (s, e) =>
            {
                using (var dlg = new OpenFileDialog { Filter = "Programas (*.exe)|*.exe" })
                    if (dlg.ShowDialog(this) == DialogResult.OK) profileApp.Text = Path.GetFileName(dlg.FileName);
            };
            var saveBtn = MakeButton("Salvar o visual atual neste perfil", 420, 120, 400);
            saveBtn.Click += (s, e) => SaveProfile();
            var applyBtn = MakeButton("Aplicar perfil selecionado", 420, 160, 196);
            applyBtn.Click += (s, e) => ApplySelectedProfile();
            var delBtn = MakeButton("Excluir perfil", 624, 160, 196);
            delBtn.Click += (s, e) =>
            {
                var pr = profileList.SelectedItem as Profile;
                if (pr != null && MessageBox.Show(this, "Excluir o perfil \"" + pr.Name + "\"?", Text, MessageBoxButtons.YesNo) == DialogResult.Yes)
                { pr.Delete(); RefreshProfiles(); }
            };
            autoBox = Check("Trocar de perfil sozinho quando o app estiver em primeiro plano", 420, 210);
            autoBox.CheckedChanged += (s, e) => { lock (settings) settings.AutoProfiles = autoBox.Checked; SaveIfManual(); };
            p.Controls.Add(new Label
            {
                Location = new Point(420, 240), Size = new Size(400, 90), ForeColor = Dim,
                Text = "Ex.: crie um perfil \"Jogo\" com WASD destacado e associe ao .exe do jogo. Ao sair do jogo, o visual normal volta sozinho."
            });
            p.Controls.AddRange(new Control[] { profileName, profileApp, browse, saveBtn, applyBtn, delBtn, autoBox });
            RefreshProfiles();
        }

        ContextMenuStrip BuildTrayMenu()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add("Abrir", null, (s, e) => ShowFromTray());
            menu.Items.Add("Ligar / desligar luzes", null, (s, e) => ToggleLights());
            menu.Items.Add("Próximo tema", null, (s, e) => NextTheme());
            var themes = new ToolStripMenuItem("Temas");
            foreach (var th in Themes) { var t = th; themes.DropDownItems.Add(t.Name, null, (s, e) => ApplyTheme(t)); }
            menu.Items.Add(themes);
            menu.Items.Add("-");
            menu.Items.Add("Sair", null, (s, e) => { reallyExit = true; Close(); });
            return menu;
        }

        // ---------------- state ----------------

        void Change(Action apply)
        {
            if (loading) return;
            lock (settings) { apply(); settings.Version++; }
            SaveIfManual();
        }

        // While an automatic profile is active, edits are temporary and do not overwrite the main settings.
        void SaveIfManual() { if (autoProfile == null) settings.Save(); }

        void LoadIntoUi()
        {
            loading = true;
            modeBox.SelectedIndex = (int)settings.Mode;
            brightBar.Value = settings.Brightness;
            speedBar.Value = settings.Speed;
            accentSwatch.BackColor = settings.Accent;
            typingBox.Checked = settings.TypingGlow;
            wasdBox.Checked = settings.Wasd;
            locksBox.Checked = settings.Locks;
            chassisRadios[(int)settings.Chassis].Checked = true;
            UpdateLabels();
            RebuildColors();
            UpdateModeUi();
            loading = false;
            lock (settings) settings.Version++;
            UpdateHook();
        }

        void LoadAutomationUi()
        {
            loading = true;
            idleBox.Checked = settings.IdleMinutes > 0;
            if (settings.IdleMinutes > 0) idleNum.Value = settings.IdleMinutes;
            batteryBox.Checked = settings.BatteryDim;
            batteryNum.Value = settings.BatteryLevel;
            nightBox.Checked = settings.NightOn;
            nightStart.Value = DateTime.Today.AddMinutes(settings.NightStart);
            nightEnd.Value = DateTime.Today.AddMinutes(settings.NightEnd);
            hotkeysBox.Checked = settings.Hotkeys;
            autoBox.Checked = settings.AutoProfiles;
            loading = false;
        }

        void SaveAutomation()
        {
            if (loading) return;
            lock (settings)
            {
                settings.IdleMinutes = idleBox.Checked ? (int)idleNum.Value : 0;
                settings.BatteryDim = batteryBox.Checked;
                settings.BatteryLevel = (int)batteryNum.Value;
                settings.NightOn = nightBox.Checked;
                settings.NightStart = nightStart.Value.Hour * 60 + nightStart.Value.Minute;
                settings.NightEnd = nightEnd.Value.Hour * 60 + nightEnd.Value.Minute;
                settings.Hotkeys = hotkeysBox.Checked;
            }
            SaveIfManual();
        }

        void ApplyTheme(Theme th)
        {
            lock (settings)
            {
                settings.Mode = th.Mode; settings.Speed = th.Speed; settings.Colors = th.Colors.ToList();
                settings.LightsOn = true;
                if (settings.Brightness == 0) settings.Brightness = 100;
            }
            themeIndex = Array.IndexOf(Themes, th);
            LoadIntoUi();
            SaveIfManual();
        }

        void NextTheme() { ApplyTheme(Themes[(themeIndex + 1) % Themes.Length]); }

        void ToggleLights() { Change(() => settings.LightsOn = !settings.LightsOn); }

        void UpdateLabels()
        {
            brightLabel.Text = "Intensidade: " + brightBar.Value + "%";
            speedLabel.Text = "Velocidade: " + speedBar.Value;
        }

        void UpdateModeUi()
        {
            int m = Math.Max(0, modeBox.SelectedIndex);
            hintLabel.Text = ModeHints[m];
            paintTools.Visible = (Mode)m == Mode.Paint;
            preview.PaintMode = (Mode)m == Mode.Paint;
            UpdateHook();
        }

        void UpdateHook()
        {
            bool need;
            lock (settings) need = settings.TypingGlow || settings.Mode == Mode.Ripple;
            if (need && hook == null) { hook = new KeyboardHook(); hook.KeyDown += engine.KeyPressed; }
            else if (!need && hook != null) { hook.Dispose(); hook = null; }
        }

        void UiTick()
        {
            engine.CapsOn = IsKeyLocked(Keys.CapsLock);
            engine.NumOn = IsKeyLocked(Keys.NumLock);
            bool on; lock (settings) on = settings.LightsOn;
            powerBtn.Text = on ? "Desligar luzes" : "Ligar luzes";
            string info = engine.Info;
            statusLabel.Text = engine.Status + (string.IsNullOrEmpty(info) ? "" : "  ·  " + info)
                + (autoProfile != null ? "  ·  perfil automático: " + autoProfile.Name : "");
        }

        void RebuildColors()
        {
            colorFlow.SuspendLayout();
            colorFlow.Controls.Clear();
            List<Color> colors; lock (settings) colors = settings.Colors.ToList();
            for (int i = 0; i < colors.Count; i++)
            {
                int index = i;
                var sw = Swatch(0, 0);
                sw.BackColor = colors[i];
                sw.Margin = new Padding(0, 0, 6, 0);
                sw.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtons.Right)
                    {
                        if (colors.Count > 1) Change(() => settings.Colors.RemoveAt(index));
                        RebuildColors();
                    }
                    else if (e.Button == MouseButtons.Left)
                    {
                        var c = PickColor(colors[index]);
                        if (c.HasValue) { Change(() => settings.Colors[index] = c.Value); RebuildColors(); }
                    }
                };
                colorFlow.Controls.Add(sw);
            }
            if (colors.Count < 8)
            {
                var add = MakeButton("+", 0, 0, 40); add.Height = 36; add.Font = new Font(Font.FontFamily, 13, FontStyle.Bold);
                add.Click += (s, e) =>
                {
                    var c = PickColor(Color.White);
                    if (c.HasValue) { Change(() => settings.Colors.Add(c.Value)); RebuildColors(); }
                };
                colorFlow.Controls.Add(add);
            }
            colorFlow.ResumeLayout();
        }

        void OnKeyPainted(int index, MouseButtons button)
        {
            int c = button == MouseButtons.Right ? 0 : brush.ToArgb() & 0xFFFFFF;
            Change(() => settings.Paint[index] = c);
        }

        // ---------------- profiles ----------------

        void RefreshProfiles()
        {
            profiles = Profile.LoadAll();
            profileList.Items.Clear();
            foreach (var p in profiles) profileList.Items.Add(p);
        }

        void SaveProfile()
        {
            string name = new string(profileName.Text.Trim().Where(ch => Path.GetInvalidFileNameChars().All(x => x != ch)).ToArray());
            if (name.Length == 0) { MessageBox.Show(this, "Digite um nome para o perfil.", Text); return; }
            var p = new Profile { Name = name, App = profileApp.Text.Trim() };
            lock (settings) p.Lighting.CopyLightingFrom(settings);
            try { p.Save(); } catch (Exception ex) { MessageBox.Show(this, "Não foi possível salvar: " + ex.Message, Text); return; }
            RefreshProfiles();
        }

        void ApplySelectedProfile()
        {
            var p = profileList.SelectedItem as Profile;
            if (p == null) return;
            if (autoProfile != null) { autoProfile = null; beforeAuto = null; }
            lock (settings) { settings.CopyLightingFrom(p.Lighting); settings.LightsOn = true; }
            LoadIntoUi();
            settings.Save();
        }

        void CheckAutoProfile()
        {
            bool enabled; lock (settings) enabled = settings.AutoProfiles;
            Profile match = null;
            if (enabled)
            {
                string fg = ForegroundProcessName();
                if (fg != null)
                    match = profiles.FirstOrDefault(p => p.App.Length > 0 &&
                        string.Equals(Path.GetFileNameWithoutExtension(p.App), fg, StringComparison.OrdinalIgnoreCase));
            }
            if (match == autoProfile) return;
            if (match != null)
            {
                if (autoProfile == null) lock (settings) beforeAuto = settings.Clone();
                autoProfile = match;
                lock (settings) settings.CopyLightingFrom(match.Lighting);
            }
            else
            {
                autoProfile = null;
                if (beforeAuto != null) lock (settings) settings.CopyLightingFrom(beforeAuto);
                beforeAuto = null;
            }
            LoadIntoUi();
        }

        static string ForegroundProcessName()
        {
            try
            {
                uint pid;
                GetWindowThreadProcessId(GetForegroundWindow(), out pid);
                if (pid == 0) return null;
                using (var p = Process.GetProcessById((int)pid)) return p.ProcessName;
            }
            catch { return null; }
        }

        // ---------------- hotkeys / power / tray ----------------

        void RegisterHotkeys()
        {
            if (!IsHandleCreated) return;
            for (int i = 1; i <= 5; i++) UnregisterHotKey(Handle, i);
            if (!hotkeysBox.Checked) return;
            const uint mods = 0x2 | 0x4 | 0x4000; // Ctrl + Shift, no repeat
            RegisterHotKey(Handle, 1, mods, (uint)Keys.F12);
            RegisterHotKey(Handle, 2, mods, (uint)Keys.F11);
            RegisterHotKey(Handle, 3, mods, (uint)Keys.F8);
            RegisterHotKey(Handle, 4, mods & ~0x4000u, (uint)Keys.F9);
            RegisterHotKey(Handle, 5, mods & ~0x4000u, (uint)Keys.F10);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RegisterHotkeys();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312)
            {
                switch (m.WParam.ToInt32())
                {
                    case 1: ToggleLights(); break;
                    case 2: modeBox.SelectedIndex = (modeBox.SelectedIndex + 1) % ModeNames.Length; break;
                    case 3: NextTheme(); break;
                    case 4: brightBar.Value = Math.Max(0, brightBar.Value - 10); break;
                    case 5: brightBar.Value = Math.Min(100, brightBar.Value + 10); break;
                }
            }
            base.WndProc(ref m);
        }

        void OnPower(object sender, PowerModeChangedEventArgs e) { if (e.Mode == PowerModes.Resume) engine.Resend(); }

        void HideToTray()
        {
            Hide();
            if (!balloonShown) { balloonShown = true; tray.ShowBalloonTip(1500, "Alien Keyboard Lights", "Continua rodando aqui na bandeja.", ToolTipIcon.Info); }
        }

        void ShowFromTray() { Show(); WindowState = FormWindowState.Normal; Activate(); }

        protected override void SetVisibleCore(bool value)
        {
            if (hiddenStart) { hiddenStart = false; if (!IsHandleCreated) CreateHandle(); value = false; }
            base.SetVisibleCore(value);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized && Visible) HideToTray();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Closing the window keeps the effects running in the tray; "Sair" in the tray menu exits.
            if (!reallyExit && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; HideToTray(); return; }
            SystemEvents.PowerModeChanged -= OnPower;
            if (hook != null) hook.Dispose();
            engine.Stop();
            tray.Visible = false;
            base.OnFormClosing(e);
        }

        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run", RunName = "AlienKeyboardLights";

        static bool IsStartupEnabled()
        {
            using (var k = Registry.CurrentUser.OpenSubKey(RunKey)) return k != null && k.GetValue(RunName) != null;
        }

        static void SetStartup(bool on)
        {
            using (var k = Registry.CurrentUser.CreateSubKey(RunKey))
            {
                if (on) k.SetValue(RunName, "\"" + Application.ExecutablePath + "\" --tray");
                else k.DeleteValue(RunName, false);
            }
        }

        // ---------------- control factories ----------------

        Color? PickColor(Color initial)
        {
            using (var dlg = new ColorDialog { Color = initial, FullOpen = true })
                return dlg.ShowDialog(this) == DialogResult.OK ? dlg.Color : (Color?)null;
        }

        Control ThemeButton(Theme th)
        {
            var b = new Button { Size = new Size(156, 50), FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 8, 8), Cursor = Cursors.Hand, Text = "" };
            b.FlatAppearance.BorderSize = 0;
            b.Paint += (s, e) =>
            {
                var g = e.Graphics;
                var r = b.ClientRectangle;
                using (var br = new LinearGradientBrush(r, Color.Black, Color.Black, 0f))
                {
                    var cols = th.Mode == Mode.Rainbow ? new[] { Color.Red, Color.Yellow, Color.Lime, Color.Cyan, Color.Blue, Color.Magenta }
                        : th.Colors.Length == 1 ? new[] { th.Colors[0], th.Colors[0] } : th.Colors;
                    br.InterpolationColors = new ColorBlend { Colors = cols, Positions = cols.Select((c, i) => i / (float)(cols.Length - 1)).ToArray() };
                    g.FillRectangle(br, r);
                }
                using (var shade = new SolidBrush(Color.FromArgb(130, 0, 0, 0))) g.FillRectangle(shade, 0, r.Height - 22, r.Width, 22);
                TextRenderer.DrawText(g, th.Name, Font, new Rectangle(0, r.Height - 22, r.Width, 22), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            b.Click += (s, e) => ApplyTheme(th);
            return b;
        }

        static Label Caption(string text, int x, int y)
        {
            return new Label { Text = text, Location = new Point(x, y), AutoSize = true, ForeColor = Dim };
        }

        TrackBar Slider(int x, int y, int min, int max)
        {
            return new TrackBar { Location = new Point(x, y), Width = 404, Minimum = min, Maximum = max, TickFrequency = 10, BackColor = Bg };
        }

        static CheckBox Check(string text, int x, int y)
        {
            return new CheckBox { Text = text, Location = new Point(x, y), AutoSize = true };
        }

        static Panel Swatch(int x, int y)
        {
            return new Panel { Location = new Point(x, y), Size = new Size(40, 36), Cursor = Cursors.Hand, BorderStyle = BorderStyle.FixedSingle };
        }

        static NumericUpDown Numeric(int x, int y, int min, int max, int value)
        {
            return new NumericUpDown { Location = new Point(x, y), Width = 62, Minimum = min, Maximum = max, Value = value, BackColor = Panel2, ForeColor = Fg };
        }

        static DateTimePicker TimePicker(int x, int y)
        {
            return new DateTimePicker { Location = new Point(x, y), Width = 90, Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true };
        }

        static TextBox TextInput(int x, int y, int w)
        {
            return new TextBox { Location = new Point(x, y), Width = w, BackColor = Panel2, ForeColor = Fg, BorderStyle = BorderStyle.FixedSingle };
        }

        static Button MakeButton(string text, int x, int y, int w)
        {
            var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, 36), FlatStyle = FlatStyle.Flat, BackColor = Panel2, ForeColor = Fg, Cursor = Cursors.Hand };
            b.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 90);
            return b;
        }

        static Icon MakeIcon()
        {
            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var br = new LinearGradientBrush(new Rectangle(0, 0, 32, 32), Color.FromArgb(0, 200, 255), Color.FromArgb(160, 0, 255), 45f))
                    g.FillEllipse(br, 2, 2, 28, 28);
                using (var br = new SolidBrush(Color.FromArgb(20, 20, 26)))
                {
                    g.FillEllipse(br, 8, 12, 7, 5);
                    g.FillEllipse(br, 17, 12, 7, 5);
                }
            }
            return Icon.FromHandle(bmp.GetHicon());
        }

        [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h, int id, uint mods, uint vk);
        [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h, int id);
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    }

    // Live drawing of the keyboard and chassis lights; in paint mode, clicks paint keys.
    class PreviewPanel : Control
    {
        static readonly string[] ChassisLabels = { "Alien", "Touchpad", "Power", "Anel ↑", "Anel ↓" };
        static readonly string[] KeyLabels = BuildKeyLabels();

        // Physical keys that sit over several lights; each is drawn (and painted) as one key.
        static readonly int[][] MultiLightKeys = {
            new[] { 33, 34, 35 }, new[] { 40, 41 }, new[] { 54, 55 }, new[] { 60, 61 }, new[] { 73, 74, 75 },
            new[] { 80, 81, 82 }, new[] { 93, 94, 95 }, new[] { 104, 105, 106, 107, 108, 109, 110 },
            new[] { 116, 117 }, new[] { 59, 79 }, new[] { 99, 119 }
        };
        // Physical keys as lists of LightEngine.Keys indices, plus the key each light index belongs to.
        static readonly List<int[]> Groups = new List<int[]>();
        static readonly int[] GroupOfKey = BuildGroups();

        // Short key names by light id (Area-51m R2 layout); for multi-light keys only the first id is used.
        static string[] BuildKeyLabels()
        {
            var l = new string[256];
            Action<int, string> set = (id, s) => l[id] = s;
            Action<int, string[]> seq = (start, names) => { for (int i = 0; i < names.Length; i++) l[start + i] = names[i]; };
            seq(0, new[] { "Esc", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12", "Home", "End", "Del", "M A", "M B", "M C", "M D" });
            seq(20, new[] { "`", "1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "-", "=", "Backspace", null, null, "Num", "/", "*", "-" });
            seq(40, new[] { "Tab", null, "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "[", "]", "\\", null, "7", "8", "9", "+" });
            seq(60, new[] { "Caps Lock", null, "A", "S", "D", "F", "G", "H", "J", "K", "L", ";", "'", "Enter", null, null, "4", "5", "6", null });
            seq(80, new[] { "Shift", null, null, "Z", "X", "C", "V", "B", "N", "M", ",", ".", "/", "Shift", null, null, "1", "2", "3", "Enter" });
            seq(100, new[] { "Ctrl", "Fn", "Win", "Alt", "Space", null, null, null, null, null, null, "AltGr", "Ctrl", "PgUp", "↑", "PgDn", "0", null, ".", null });
            set(133, "←"); set(134, "↓"); set(135, "→");
            return l;
        }

        static int[] BuildGroups()
        {
            var keys = LightEngine.Keys;
            var owner = Enumerable.Repeat(-1, keys.Length).ToArray();
            Func<int, int> indexOf = id => Array.FindIndex(keys, k => k.Id == id);
            foreach (var ids in MultiLightKeys)
            {
                var members = ids.Select(indexOf).ToArray();
                foreach (var m in members) owner[m] = Groups.Count;
                Groups.Add(members);
            }
            for (int i = 0; i < keys.Length; i++)
                if (owner[i] < 0) { owner[i] = Groups.Count; Groups.Add(new[] { i }); }
            return owner;
        }
        readonly LightEngine engine;
        public bool PaintMode;
        public event Action<int, MouseButtons> KeyPainted;

        public PreviewPanel(LightEngine e)
        {
            engine = e;
            DoubleBuffered = true;
            BackColor = Color.FromArgb(12, 12, 16);
            e.FrameReady += () => { try { if (IsHandleCreated && Visible) BeginInvoke((Action)Invalidate); } catch { } };
        }

        float CellW { get { return (Width - 20) / 20f; } }
        float CellH { get { return (Height - 20) / 7f; } }

        protected override void OnPaint(PaintEventArgs pe)
        {
            var g = pe.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            float cw = CellW, ch = CellH;
            var keys = LightEngine.Keys;
            using (var labelFont = new Font("Segoe UI", 8f, FontStyle.Bold))
                foreach (var group in Groups)
                {
                    // One rounded key over all its cells; several lights under it show as a gradient.
                    int c0 = group.Min(i => keys[i].Col), c1 = group.Max(i => keys[i].Col);
                    int r0 = group.Min(i => keys[i].Row), r1 = group.Max(i => keys[i].Row);
                    var r = new RectangleF(10 + c0 * cw + 2, 10 + r0 * ch + 2, (c1 - c0 + 1) * cw - 4, (r1 - r0 + 1) * ch - 4);
                    var colors = group.Select(i => Color.FromArgb(255, Color.FromArgb(engine.Frame[i]))).ToArray();
                    var fills = colors.Select(c => Color.FromArgb(255, c.R / 3 + 10, c.G / 3 + 10, c.B / 3 + 10)).ToArray();
                    using (var path = Rounded(r, 5))
                    using (var fill = KeyBrush(r, fills, r1 > r0))
                    using (var edge = KeyBrush(r, colors, r1 > r0))
                    using (var pen = new Pen(edge, 2))
                    {
                        g.FillPath(fill, path);
                        g.DrawPath(pen, path);
                    }
                    string label = KeyLabels[keys[group[0]].Id];
                    if (label != null)
                        TextRenderer.DrawText(g, label, labelFont, Rectangle.Round(r), Color.FromArgb(235, 235, 245),
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
                }
            // Chassis lights on the free left part of the last row
            float y = 10 + 6 * ch + 4, d = ch - 8;
            using (var small = new Font("Segoe UI", 8f))
                for (int j = 0; j < LightEngine.ChassisCount; j++)
                {
                    float x = 10 + j * cw * 2.5f;
                    var c = Color.FromArgb(255, Color.FromArgb(engine.ChassisFrame[j]));
                    using (var br = new SolidBrush(c)) g.FillEllipse(br, x, y, d, d);
                    using (var pen = new Pen(Color.FromArgb(80, 80, 100))) g.DrawEllipse(pen, x, y, d, d);
                    TextRenderer.DrawText(g, ChassisLabels[j], small, new Point((int)(x + d + 4), (int)(y + d / 2 - 7)), Color.FromArgb(150, 150, 170));
                }
        }

        int lastPainted = -1;

        protected override void OnMouseDown(MouseEventArgs e) { base.OnMouseDown(e); lastPainted = -1; PaintAt(e); }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (e.Button != MouseButtons.None) PaintAt(e); }

        void PaintAt(MouseEventArgs e)
        {
            if (!PaintMode || (e.Button != MouseButtons.Left && e.Button != MouseButtons.Right)) return;
            int col = (int)Math.Floor((e.X - 10) / CellW), row = (int)Math.Floor((e.Y - 10) / CellH);
            int index = LightEngine.KeyAt(col, row);
            if (index < 0 || GroupOfKey[index] == lastPainted) return;
            lastPainted = GroupOfKey[index];
            if (KeyPainted != null) foreach (var i in Groups[lastPainted]) KeyPainted(i, e.Button);
        }

        static Brush KeyBrush(RectangleF r, Color[] colors, bool vertical)
        {
            if (colors.Length == 1) return new SolidBrush(colors[0]);
            var br = new LinearGradientBrush(new RectangleF(r.X - 1, r.Y - 1, r.Width + 2, r.Height + 2), colors[0], colors[colors.Length - 1], vertical ? 90f : 0f);
            br.InterpolationColors = new ColorBlend { Colors = colors, Positions = colors.Select((c, i) => i / (float)(colors.Length - 1)).ToArray() };
            return br;
        }

        static GraphicsPath Rounded(RectangleF r, float rad)
        {
            var p = new GraphicsPath(); float d = rad * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90); p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure(); return p;
        }
    }
}
