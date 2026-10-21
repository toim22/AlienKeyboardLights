using System;
using System.Collections.Generic;
using System.Globalization;

namespace AlienKeyboardLights
{
    // UI translations. The Portuguese text is the key; each entry lists en, fr, es, ja, zh, de, hi, it.
    static class L
    {
        public static readonly string[] Codes = { "pt", "en", "fr", "es", "ja", "zh", "de", "hi", "it" };
        public static readonly string[] Names = { "Português", "English", "Français", "Español", "日本語", "简体中文", "Deutsch", "हिन्दी", "Italiano" };
        public static int Current;

        public static string Code { get { return Codes[Current]; } }

        // Empty code = follow the Windows display language (English when it is not supported).
        public static void Set(string code)
        {
            if (string.IsNullOrEmpty(code)) code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            int i = Array.IndexOf(Codes, code);
            Current = i < 0 ? 1 : i;
        }

        public static string T(string pt)
        {
            string[] tr;
            return Current == 0 || !Map.TryGetValue(pt, out tr) ? pt : tr[Current - 1];
        }

        static readonly Dictionary<string, string[]> Map = Build();

        static Dictionary<string, string[]> Build()
        {
            var d = new Dictionary<string, string[]>();
            Action<string, string[]> E = (pt, tr) =>
            {
                if (tr.Length != 8) throw new InvalidOperationException("Translation count mismatch: " + pt);
                d[pt] = tr;
            };

            // Themes
            E("Halloween", new[] { "Halloween", "Halloween", "Halloween", "ハロウィン", "万圣节", "Halloween", "हैलोवीन", "Halloween" });
            E("Natal", new[] { "Christmas", "Noël", "Navidad", "クリスマス", "圣诞节", "Weihnachten", "क्रिसमस", "Natale" });
            E("Ano Novo", new[] { "New Year", "Nouvel An", "Año Nuevo", "お正月", "新年", "Neujahr", "नया साल", "Capodanno" });
            E("Carnaval", new[] { "Carnival", "Carnaval", "Carnaval", "カーニバル", "狂欢节", "Karneval", "कार्निवल", "Carnevale" });
            E("Páscoa", new[] { "Easter", "Pâques", "Pascua", "イースター", "复活节", "Ostern", "ईस्टर", "Pasqua" });
            E("Namorados", new[] { "Valentine's", "Saint-Valentin", "San Valentín", "バレンタイン", "情人节", "Valentinstag", "वैलेंटाइन", "San Valentino" });
            E("Festa Junina", new[] { "Festa Junina", "Festa Junina", "Festa Junina", "フェスタ・ジュニーナ", "六月节", "Festa Junina", "फ़ेस्टा जुनीना", "Festa Junina" });
            E("Brasil", new[] { "Brazil", "Brésil", "Brasil", "ブラジル", "巴西", "Brasilien", "ब्राज़ील", "Brasile" });
            E("4 de Julho", new[] { "4th of July", "4 Juillet", "4 de Julio", "7月4日", "7月4日", "4. Juli", "4 जुलाई", "4 luglio" });
            E("St. Patrick", new[] { "St. Patrick's", "Saint-Patrick", "San Patricio", "聖パトリック", "圣帕特里克节", "St. Patrick's Day", "सेंट पैट्रिक", "San Patrizio" });
            E("Ação de Graças", new[] { "Thanksgiving", "Thanksgiving", "Acción de Gracias", "感謝祭", "感恩节", "Thanksgiving", "थैंक्सगिविंग", "Ringraziamento" });
            E("Outubro Rosa", new[] { "Pink October", "Octobre Rose", "Octubre Rosa", "ピンクリボン", "粉红十月", "Pinktober", "गुलाबी अक्टूबर", "Ottobre Rosa" });
            E("Neon", new[] { "Neon", "Néon", "Neón", "ネオン", "霓虹", "Neon", "नियॉन", "Neon" });
            E("Fogo", new[] { "Fire", "Feu", "Fuego", "炎", "火焰", "Feuer", "आग", "Fuoco" });
            E("Oceano", new[] { "Ocean", "Océan", "Océano", "オーシャン", "海洋", "Ozean", "महासागर", "Oceano" });
            E("Aurora", new[] { "Aurora", "Aurore", "Aurora", "オーロラ", "极光", "Polarlicht", "ध्रुवीय ज्योति", "Aurora" });
            E("Matrix", new[] { "Matrix", "Matrix", "Matrix", "マトリックス", "Matrix", "Matrix", "मैट्रिक्स", "Matrix" });
            E("Céu estrelado", new[] { "Starry Sky", "Ciel étoilé", "Cielo estrellado", "星空", "星空", "Sternenhimmel", "तारों भरा आकाश", "Cielo stellato" });
            E("Música", new[] { "Music", "Musique", "Música", "ミュージック", "音乐", "Musik", "संगीत", "Musica" });
            E("Cinema (tela)", new[] { "Cinema (screen)", "Cinéma (écran)", "Cine (pantalla)", "シネマ（画面）", "影院（屏幕）", "Kino (Bildschirm)", "सिनेमा (स्क्रीन)", "Cinema (schermo)" });
            E("Monitor CPU", new[] { "CPU Monitor", "Moniteur CPU", "Monitor de CPU", "CPUモニター", "CPU 监视", "CPU-Monitor", "CPU मॉनिटर", "Monitor CPU" });
            E("Arco-íris", new[] { "Rainbow", "Arc-en-ciel", "Arcoíris", "レインボー", "彩虹", "Regenbogen", "इंद्रधनुष", "Arcobaleno" });
            E("Branco", new[] { "White", "Blanc", "Blanco", "ホワイト", "白色", "Weiß", "सफ़ेद", "Bianco" });

            // Effects
            E("Cor fixa", new[] { "Solid color", "Couleur fixe", "Color fijo", "単色", "纯色", "Einfarbig", "एक रंग", "Colore fisso" });
            E("Gradiente (fixo)", new[] { "Gradient (static)", "Dégradé (fixe)", "Degradado (fijo)", "グラデーション（固定）", "渐变（静态）", "Verlauf (statisch)", "ग्रेडिएंट (स्थिर)", "Sfumatura (fissa)" });
            E("Transição suave", new[] { "Smooth fade", "Fondu doux", "Transición suave", "スムーズフェード", "平滑渐变", "Sanfter Übergang", "धीमा बदलाव", "Dissolvenza" });
            E("Respirar", new[] { "Breathe", "Respiration", "Respiración", "呼吸", "呼吸", "Atmen", "सांस", "Respiro" });
            E("Onda", new[] { "Wave", "Vague", "Onda", "ウェーブ", "波浪", "Welle", "लहर", "Onda" });
            E("Piscar", new[] { "Blink", "Clignotement", "Parpadeo", "点滅", "闪烁", "Blinken", "झपकना", "Lampeggio" });
            E("Cintilar (pisca-pisca)", new[] { "Twinkle (string lights)", "Scintillement (guirlande)", "Centelleo (luces navideñas)", "きらめき（イルミネーション）", "闪耀（彩灯）", "Funkeln (Lichterkette)", "टिमटिमाना (झालर)", "Scintillio (lucine)" });
            E("Listras", new[] { "Stripes", "Rayures", "Rayas", "ストライプ", "条纹", "Streifen", "धारियाँ", "Strisce" });
            E("Onda radial", new[] { "Radial wave", "Vague radiale", "Onda radial", "放射状ウェーブ", "径向波浪", "Radiale Welle", "गोलाकार लहर", "Onda radiale" });
            E("Scanner (KITT)", new[] { "Scanner (KITT)", "Scanner (KITT)", "Escáner (KITT)", "スキャナー（KITT）", "扫描（KITT）", "Scanner (KITT)", "स्कैनर (KITT)", "Scanner (KITT)" });
            E("Chuva (Matrix)", new[] { "Rain (Matrix)", "Pluie (Matrix)", "Lluvia (Matrix)", "レイン（マトリックス）", "数字雨（Matrix）", "Regen (Matrix)", "बारिश (मैट्रिक्स)", "Pioggia (Matrix)" });
            E("Ondulação ao digitar", new[] { "Ripple on typing", "Ondulation à la frappe", "Ondas al escribir", "タイピング波紋", "打字涟漪", "Wellen beim Tippen", "टाइप करने पर लहर", "Increspature digitando" });
            E("Música (visualizador)", new[] { "Music (visualizer)", "Musique (visualiseur)", "Música (visualizador)", "ミュージック（ビジュアライザー）", "音乐（可视化）", "Musik (Visualizer)", "संगीत (विज़ुअलाइज़र)", "Musica (visualizzatore)" });
            E("Ambiente (cores da tela)", new[] { "Ambient (screen colors)", "Ambiance (couleurs de l'écran)", "Ambiente (colores de pantalla)", "アンビエント（画面の色）", "环境（屏幕颜色）", "Ambient (Bildschirmfarben)", "एम्बिएंट (स्क्रीन के रंग)", "Ambiente (colori dello schermo)" });
            E("Medidor de CPU", new[] { "CPU meter", "Jauge CPU", "Medidor de CPU", "CPUメーター", "CPU 仪表", "CPU-Anzeige", "CPU मीटर", "Indicatore CPU" });
            E("Medidor de RAM", new[] { "RAM meter", "Jauge RAM", "Medidor de RAM", "RAMメーター", "内存仪表", "RAM-Anzeige", "RAM मीटर", "Indicatore RAM" });
            E("Medidor de bateria", new[] { "Battery meter", "Jauge batterie", "Medidor de batería", "バッテリーメーター", "电池仪表", "Akku-Anzeige", "बैटरी मीटर", "Indicatore batteria" });
            E("Pintura por tecla", new[] { "Per-key painting", "Peinture par touche", "Pintura por tecla", "キーごとにペイント", "逐键绘制", "Tasten bemalen", "हर कुंजी पर रंग", "Pittura per tasto" });

            // Effect hints
            E("Todas as teclas com a primeira cor.", new[] {
                "All keys in the first color.", "Toutes les touches de la première couleur.", "Todas las teclas con el primer color.",
                "すべてのキーを1番目の色に。", "所有按键使用第一种颜色。", "Alle Tasten in der ersten Farbe.", "सभी कुंजियाँ पहले रंग में।", "Tutti i tasti nel primo colore." });
            E("As cores se espalham da esquerda para a direita.", new[] {
                "Colors spread from left to right.", "Les couleurs s'étalent de gauche à droite.", "Los colores se reparten de izquierda a derecha.",
                "色が左から右へ広がります。", "颜色从左到右分布。", "Die Farben verlaufen von links nach rechts.", "रंग बाएँ से दाएँ फैलते हैं।", "I colori si distribuiscono da sinistra a destra." });
            E("O teclado inteiro passa suavemente por cada cor.", new[] {
                "The whole keyboard smoothly fades through each color.", "Tout le clavier passe en douceur par chaque couleur.", "Todo el teclado pasa suavemente por cada color.",
                "キーボード全体が各色へなめらかに移り変わります。", "整个键盘平滑地依次切换每种颜色。", "Die ganze Tastatur wechselt sanft durch alle Farben.",
                "पूरा कीबोर्ड धीरे-धीरे हर रंग से गुज़रता है।", "Tutta la tastiera passa dolcemente per ogni colore." });
            E("Acende e apaga devagar, alternando as cores.", new[] {
                "Slowly fades in and out, alternating colors.", "S'allume et s'éteint lentement en alternant les couleurs.", "Se enciende y apaga lentamente, alternando los colores.",
                "色を切り替えながらゆっくり明滅します。", "缓慢亮起和熄灭，颜色轮流切换。", "Leuchtet langsam auf und ab, mit wechselnden Farben.",
                "रंग बदलते हुए धीरे-धीरे जलता और बुझता है।", "Si accende e si spegne lentamente, alternando i colori." });
            E("As cores correm pelo teclado como uma onda.", new[] {
                "Colors run across the keyboard like a wave.", "Les couleurs parcourent le clavier comme une vague.", "Los colores recorren el teclado como una ola.",
                "色が波のようにキーボードを流れます。", "颜色像波浪一样在键盘上流动。", "Die Farben laufen wie eine Welle über die Tastatur.",
                "रंग लहर की तरह कीबोर्ड पर दौड़ते हैं।", "I colori scorrono sulla tastiera come un'onda." });
            E("Arco-íris completo em movimento (não usa as cores escolhidas).", new[] {
                "Full moving rainbow (ignores the chosen colors).", "Arc-en-ciel complet en mouvement (n'utilise pas les couleurs choisies).",
                "Arcoíris completo en movimiento (no usa los colores elegidos).", "動く虹（選んだ色は使いません）。", "完整的动态彩虹（不使用所选颜色）。",
                "Vollständiger, bewegter Regenbogen (ignoriert die gewählten Farben).", "पूरा चलता इंद्रधनुष (चुने गए रंगों का उपयोग नहीं करता)।",
                "Arcobaleno completo in movimento (non usa i colori scelti)." });
            E("Troca de cor de repente, no ritmo da velocidade.", new[] {
                "Switches color abruptly, at the chosen speed.", "Change brusquement de couleur, au rythme de la vitesse.", "Cambia de color de golpe, al ritmo de la velocidad.",
                "速度に合わせて色がパッと切り替わります。", "按速度节奏突然切换颜色。", "Wechselt die Farbe schlagartig im Takt der Geschwindigkeit.",
                "गति के अनुसार रंग अचानक बदलता है।", "Cambia colore di colpo, al ritmo della velocità." });
            E("Cada tecla troca de cor aleatoriamente, como pisca-pisca de Natal.", new[] {
                "Each key changes color randomly, like Christmas lights.", "Chaque touche change de couleur au hasard, comme une guirlande de Noël.",
                "Cada tecla cambia de color al azar, como luces de Navidad.", "クリスマスのイルミネーションのように各キーの色がランダムに変わります。",
                "每个按键随机变色，就像圣诞彩灯。", "Jede Taste wechselt zufällig die Farbe, wie eine Weihnachtslichterkette.",
                "हर कुंजी क्रिसमस की झालर की तरह बेतरतीब रंग बदलती है।", "Ogni tasto cambia colore a caso, come le lucine di Natale." });
            E("Faixas diagonais com as cores, andando.", new[] {
                "Moving diagonal stripes in your colors.", "Bandes diagonales de couleurs qui défilent.", "Franjas diagonales de colores en movimiento.",
                "選んだ色の斜めストライプが流れます。", "移动的彩色斜条纹。", "Wandernde diagonale Streifen in deinen Farben.",
                "आपके रंगों की चलती तिरछी धारियाँ।", "Strisce diagonali colorate in movimento." });
            E("Ondas de cor saindo do centro do teclado.", new[] {
                "Color waves expanding from the center of the keyboard.", "Vagues de couleur partant du centre du clavier.", "Ondas de color que salen del centro del teclado.",
                "キーボードの中心から色の波が広がります。", "颜色波浪从键盘中心向外扩散。", "Farbwellen breiten sich von der Tastaturmitte aus.",
                "कीबोर्ड के बीच से रंग की लहरें निकलती हैं।", "Onde di colore che partono dal centro della tastiera." });
            E("Fundo escuro com teclas piscando como estrelas.", new[] {
                "Dark background with keys twinkling like stars.", "Fond sombre avec des touches qui scintillent comme des étoiles.",
                "Fondo oscuro con teclas que brillan como estrellas.", "暗い背景にキーが星のようにまたたきます。", "深色背景上，按键像星星一样闪烁。",
                "Dunkler Hintergrund mit Tasten, die wie Sterne funkeln.", "अंधेरी पृष्ठभूमि पर कुंजियाँ तारों की तरह टिमटिमाती हैं।",
                "Sfondo scuro con tasti che brillano come stelle." });
            E("Uma faixa de luz vai e volta (cada passada usa a próxima cor).", new[] {
                "A band of light sweeps back and forth (each pass uses the next color).",
                "Une bande de lumière fait des allers-retours (chaque passage prend la couleur suivante).",
                "Una franja de luz va y viene (cada pasada usa el siguiente color).", "光の帯が往復します（往復ごとに次の色）。",
                "一条光带来回扫动（每次换下一种颜色）。", "Ein Lichtbalken läuft hin und her (jeder Durchgang in der nächsten Farbe).",
                "रोशनी की एक पट्टी आगे-पीछे चलती है (हर बार अगला रंग)।", "Una barra di luce va avanti e indietro (ogni passata usa il colore successivo)." });
            E("Gotas caindo pelas colunas, estilo Matrix.", new[] {
                "Drops falling down the columns, Matrix style.", "Des gouttes tombent le long des colonnes, style Matrix.", "Gotas cayendo por las columnas, estilo Matrix.",
                "マトリックス風に列を雫が落ちていきます。", "沿列落下的雨滴，黑客帝国风格。", "Tropfen fallen die Spalten hinunter, im Matrix-Stil.",
                "कॉलम में गिरती बूँदें, मैट्रिक्स स्टाइल।", "Gocce che cadono lungo le colonne, stile Matrix." });
            E("Cada tecla pressionada solta uma onda de cor.", new[] {
                "Every key you press sends out a wave of color.", "Chaque touche pressée lance une vague de couleur.", "Cada tecla pulsada lanza una onda de color.",
                "押したキーから色の波紋が広がります。", "每按一个键都会激起一圈颜色波纹。", "Jede gedrückte Taste sendet eine Farbwelle aus.",
                "हर दबाई गई कुंजी से रंग की लहर निकलती है।", "Ogni tasto premuto lancia un'onda di colore." });
            E("Equalizador que reage ao som tocando no PC (cores: base → topo).", new[] {
                "Equalizer that reacts to the sound playing on the PC (colors: bottom → top).",
                "Égaliseur qui réagit au son joué sur le PC (couleurs : bas → haut).",
                "Ecualizador que reacciona al sonido del PC (colores: abajo → arriba).", "PCで再生中の音に反応するイコライザー（色：下→上）。",
                "随电脑播放的声音变化的均衡器（颜色：底部 → 顶部）。", "Equalizer, der auf den Ton am PC reagiert (Farben: unten → oben).",
                "PC पर चल रही आवाज़ पर प्रतिक्रिया देने वाला इक्वलाइज़र (रंग: नीचे → ऊपर)।", "Equalizzatore che reagisce al suono del PC (colori: basso → alto)." });
            E("Copia as cores da tela principal — ótimo para filmes e jogos.", new[] {
                "Mirrors the colors of the main screen — great for movies and games.",
                "Reproduit les couleurs de l'écran principal — idéal pour les films et les jeux.",
                "Copia los colores de la pantalla principal: ideal para películas y juegos.", "メイン画面の色をコピーします。映画やゲームに最適。",
                "复制主屏幕的颜色——非常适合电影和游戏。", "Übernimmt die Farben des Hauptbildschirms – ideal für Filme und Spiele.",
                "मुख्य स्क्रीन के रंग दिखाता है — फ़िल्मों और गेम के लिए बढ़िया।", "Copia i colori dello schermo principale: ottimo per film e giochi." });
            E("Barra com o uso da CPU (cores da esquerda = baixo, direita = alto).", new[] {
                "Bar showing CPU usage (left colors = low, right = high).", "Barre d'utilisation du CPU (couleurs de gauche = faible, droite = élevé).",
                "Barra con el uso de CPU (colores a la izquierda = bajo, derecha = alto).", "CPU使用率のバー（左の色＝低、右＝高）。",
                "显示 CPU 使用率的条（左侧颜色 = 低，右侧 = 高）。", "Balken mit der CPU-Auslastung (linke Farben = niedrig, rechte = hoch).",
                "CPU उपयोग की पट्टी (बाएँ रंग = कम, दाएँ = ज़्यादा)।", "Barra con l'uso della CPU (colori a sinistra = basso, a destra = alto)." });
            E("Barra com o uso da memória RAM.", new[] {
                "Bar showing RAM usage.", "Barre d'utilisation de la mémoire RAM.", "Barra con el uso de memoria RAM.", "RAM使用率のバー。",
                "显示内存使用率的条。", "Balken mit der RAM-Auslastung.", "RAM उपयोग की पट्टी।", "Barra con l'uso della RAM." });
            E("Barra com o nível da bateria.", new[] {
                "Bar showing the battery level.", "Barre du niveau de batterie.", "Barra con el nivel de batería.", "バッテリー残量のバー。",
                "显示电池电量的条。", "Balken mit dem Akkustand.", "बैटरी स्तर की पट्टी।", "Barra con il livello della batteria." });
            E("Clique ou arraste nas teclas da prévia para pintar. Botão direito apaga.", new[] {
                "Click or drag over the keys in the preview to paint. Right-click erases.",
                "Cliquez ou glissez sur les touches de l'aperçu pour peindre. Clic droit pour effacer.",
                "Haz clic o arrastra sobre las teclas de la vista previa para pintar. Clic derecho borra.",
                "プレビューのキーをクリックまたはドラッグしてペイント。右クリックで消去。", "在预览中点击或拖动按键进行绘制，右键擦除。",
                "Klicke oder ziehe über die Tasten in der Vorschau, um sie zu bemalen. Rechtsklick löscht.",
                "रंगने के लिए प्रीव्यू की कुंजियों पर क्लिक करें या खींचें। राइट-क्लिक मिटाता है।",
                "Fai clic o trascina sui tasti dell'anteprima per dipingere. Il tasto destro cancella." });

            // Tabs and main buttons
            E("Efeitos", new[] { "Effects", "Effets", "Efectos", "エフェクト", "效果", "Effekte", "इफ़ेक्ट", "Effetti" });
            E("Temas", new[] { "Themes", "Thèmes", "Temas", "テーマ", "主题", "Themen", "थीम", "Temi" });
            E("Chassi", new[] { "Chassis", "Châssis", "Chasis", "本体", "机身", "Gehäuse", "चेसिस", "Chassis" });
            E("Automação", new[] { "Automation", "Automatisation", "Automatización", "自動化", "自动化", "Automatik", "ऑटोमेशन", "Automazione" });
            E("Perfis", new[] { "Profiles", "Profils", "Perfiles", "プロファイル", "配置文件", "Profile", "प्रोफ़ाइल", "Profili" });
            E("Desligar luzes", new[] { "Turn lights off", "Éteindre", "Apagar luces", "ライトを消す", "关闭灯光", "Licht aus", "लाइट बंद करें", "Spegni luci" });
            E("Ligar luzes", new[] { "Turn lights on", "Allumer", "Encender luces", "ライトをつける", "打开灯光", "Licht an", "लाइट चालू करें", "Accendi luci" });
            E("Minimizar p/ bandeja", new[] { "Minimize to tray", "Réduire dans la barre", "Minimizar a la bandeja", "トレイに最小化", "最小化到托盘", "In den Tray", "ट्रे में छोटा करें", "Riduci a icona" });

            // Effects tab
            E("Efeito", new[] { "Effect", "Effet", "Efecto", "エフェクト", "效果", "Effekt", "इफ़ेक्ट", "Effetto" });
            E("Cores  (clique para editar, botão direito para remover)", new[] {
                "Colors  (click to edit, right-click to remove)", "Couleurs  (clic pour modifier, clic droit pour retirer)",
                "Colores  (clic para editar, clic derecho para quitar)", "色（クリックで編集、右クリックで削除）", "颜色（点击编辑，右键删除）",
                "Farben  (Klick zum Bearbeiten, Rechtsklick zum Entfernen)", "रंग (बदलने के लिए क्लिक, हटाने के लिए राइट-क्लिक)",
                "Colori  (clic per modificare, clic destro per rimuovere)" });
            E("Intensidade: ", new[] { "Brightness: ", "Intensité : ", "Intensidad: ", "明るさ: ", "亮度：", "Helligkeit: ", "चमक: ", "Intensità: " });
            E("Velocidade: ", new[] { "Speed: ", "Vitesse : ", "Velocidad: ", "速度: ", "速度：", "Geschwindigkeit: ", "गति: ", "Velocità: " });
            E("Cor de destaque", new[] { "Accent color", "Couleur d'accent", "Color de resalte", "アクセントカラー", "强调色", "Akzentfarbe", "हाइलाइट रंग", "Colore di risalto" });
            E("Teclas acendem ao digitar (na cor de destaque)", new[] {
                "Keys light up when typed (accent color)", "Les touches s'allument à la frappe (couleur d'accent)",
                "Las teclas se iluminan al escribir (color de resalte)", "入力したキーを光らせる（アクセントカラー）", "打字时按键亮起（强调色）",
                "Tasten leuchten beim Tippen (Akzentfarbe)", "टाइप करने पर कुंजियाँ चमकें (हाइलाइट रंग)", "I tasti si illuminano digitando (colore di risalto)" });
            E("Destacar WASD e setas (para jogos)", new[] {
                "Highlight WASD and arrows (for games)", "Mettre en évidence WASD et les flèches (jeux)", "Resaltar WASD y flechas (para juegos)",
                "WASDと矢印キーを強調（ゲーム用）", "突出显示 WASD 和方向键（游戏用）", "WASD und Pfeiltasten hervorheben (für Spiele)",
                "WASD और तीर कुंजियाँ हाइलाइट करें (गेम के लिए)", "Evidenzia WASD e frecce (per i giochi)" });
            E("Mostrar Caps Lock e Num Lock ligados", new[] {
                "Show when Caps Lock and Num Lock are on", "Indiquer Verr. Maj et Verr. Num actifs", "Mostrar Bloq Mayús y Bloq Num activos",
                "Caps LockとNum Lockのオンを表示", "显示大写锁定和数字锁定状态", "Feststell- und Num-Taste anzeigen, wenn aktiv",
                "Caps Lock और Num Lock चालू होने पर दिखाएँ", "Mostra Bloc Maiusc e Bloc Num attivi" });
            E("Pincel", new[] { "Brush", "Pinceau", "Pincel", "ブラシ", "画笔", "Pinsel", "ब्रश", "Pennello" });
            E("Preencher tudo", new[] { "Fill all", "Tout remplir", "Rellenar todo", "すべて塗る", "全部填充", "Alles füllen", "सब भरें", "Riempi tutto" });
            E("Limpar tudo", new[] { "Clear all", "Tout effacer", "Borrar todo", "すべて消す", "全部清除", "Alles löschen", "सब साफ़ करें", "Cancella tutto" });
            E("Copiar cores atuais da prévia", new[] {
                "Copy current preview colors", "Copier les couleurs de l'aperçu", "Copiar colores de la vista previa", "プレビューの色をコピー",
                "复制当前预览颜色", "Vorschaufarben übernehmen", "प्रीव्यू के मौजूदा रंग कॉपी करें", "Copia i colori dell'anteprima" });

            // Chassis tab
            E("Luzes do chassi: cabeça alien, touchpad, botão power e anel de luz (Tron)", new[] {
                "Chassis lights: alien head, touchpad, power button and light ring (Tron)",
                "Lumières du châssis : tête d'alien, pavé tactile, bouton d'alimentation et anneau lumineux (Tron)",
                "Luces del chasis: cabeza alien, touchpad, botón de encendido y anillo de luz (Tron)",
                "本体ライト：エイリアンヘッド、タッチパッド、電源ボタン、ライトリング（Tron）", "机身灯光：外星人头、触控板、电源按钮和灯环（Tron）",
                "Gehäuseleuchten: Alien-Kopf, Touchpad, Einschalttaste und Lichtring (Tron)",
                "चेसिस लाइटें: एलियन हेड, टचपैड, पावर बटन और लाइट रिंग (Tron)",
                "Luci del telaio: testa alien, touchpad, tasto di accensione e anello luminoso (Tron)" });
            E("Acompanhar as cores do teclado", new[] {
                "Follow the keyboard colors", "Suivre les couleurs du clavier", "Seguir los colores del teclado", "キーボードの色に合わせる",
                "跟随键盘颜色", "Tastaturfarben folgen", "कीबोर्ड के रंगों के साथ चलें", "Segui i colori della tastiera" });
            E("Usar a cor de destaque (aba Efeitos)", new[] {
                "Use the accent color (Effects tab)", "Utiliser la couleur d'accent (onglet Effets)", "Usar el color de resalte (pestaña Efectos)",
                "アクセントカラーを使用（エフェクトタブ）", "使用强调色（效果选项卡）", "Akzentfarbe verwenden (Reiter Effekte)",
                "हाइलाइट रंग का उपयोग करें (इफ़ेक्ट टैब)", "Usa il colore di risalto (scheda Effetti)" });
            E("Desligadas", new[] { "Off", "Éteintes", "Apagadas", "オフ", "关闭", "Aus", "बंद", "Spente" });
            E("Não controlar (deixar como estão)", new[] {
                "Don't control (leave as they are)", "Ne pas contrôler (laisser tel quel)", "No controlar (dejarlas como están)", "制御しない（そのまま）",
                "不控制（保持原样）", "Nicht steuern (so lassen)", "नियंत्रित न करें (जैसी हैं वैसी रहने दें)", "Non controllare (lasciale come sono)" });
            E("As luzes do chassi aparecem na parte de baixo da prévia. A intensidade e o desligamento automático valem para elas também.", new[] {
                "The chassis lights are shown at the bottom of the preview. Brightness and automatic turn-off apply to them too.",
                "Les lumières du châssis apparaissent en bas de l'aperçu. L'intensité et l'extinction automatique s'y appliquent aussi.",
                "Las luces del chasis aparecen en la parte inferior de la vista previa. La intensidad y el apagado automático también se aplican a ellas.",
                "本体ライトはプレビューの下部に表示されます。明るさと自動消灯も適用されます。",
                "机身灯光显示在预览底部。亮度和自动关闭同样适用于它们。",
                "Die Gehäuseleuchten werden unten in der Vorschau angezeigt. Helligkeit und automatisches Ausschalten gelten auch für sie.",
                "चेसिस लाइटें प्रीव्यू के नीचे दिखती हैं। चमक और अपने-आप बंद होना इन पर भी लागू होता है।",
                "Le luci del telaio sono mostrate in basso nell'anteprima. Intensità e spegnimento automatico valgono anche per loro." });
            E("Anel", new[] { "Ring", "Anneau", "Anillo", "リング", "灯环", "Ring", "रिंग", "Anello" });

            // Automation tab (sentences are split around the number/time fields)
            E("Apagar as luzes depois de", new[] {
                "Turn lights off after", "Éteindre après", "Apagar las luces tras", "PCを使わない時間が", "电脑闲置", "Licht ausschalten nach",
                "PC इस्तेमाल न होने पर", "Spegni le luci dopo" });
            E("minutos sem usar o PC (acendem ao mexer)", new[] {
                "minutes of inactivity (they come back on when you use the PC)", "minutes d'inactivité (se rallument à l'utilisation)",
                "minutos sin usar el PC (se encienden al usarlo)", "分続いたらライトを消す（操作で再点灯）", "分钟后关闭灯光（操作后重新亮起）",
                "Minuten ohne Nutzung (gehen bei Aktivität wieder an)", "मिनट बाद लाइट बंद करें (इस्तेमाल करने पर फिर जलेंगी)",
                "minuti di inattività (si riaccendono all'uso)" });
            E("Na bateria, reduzir a intensidade para", new[] {
                "On battery, reduce brightness to", "Sur batterie, réduire l'intensité à", "Con batería, reducir la intensidad a",
                "バッテリー駆動時は明るさを", "使用电池时，将亮度降低到", "Im Akkubetrieb Helligkeit reduzieren auf", "बैटरी पर चमक घटाकर",
                "A batteria, riduci l'intensità al" });
            E("%", new[] { "%", "%", "%", "% に下げる", "%", "%", "% करें", "%" });
            E("Modo noturno: desligar das", new[] {
                "Night mode: off from", "Mode nuit : éteindre de", "Modo nocturno: apagar de", "夜間モード：消灯", "夜间模式：关闭时段",
                "Nachtmodus: aus von", "नाइट मोड: बंद रहने का समय", "Modalità notte: spegni dalle" });
            E("às", new[] { "to", "à", "a", "～", "至", "bis", "से", "alle" });
            E("Iniciar com o Windows (fica na bandeja)", new[] {
                "Start with Windows (in the tray)", "Démarrer avec Windows (dans la barre)", "Iniciar con Windows (en la bandeja)",
                "Windows起動時に開始（トレイ）", "随 Windows 启动（托盘中）", "Mit Windows starten (im Tray)", "Windows के साथ शुरू करें (ट्रे में)",
                "Avvia con Windows (nell'area di notifica)" });
            E("Atalhos globais de teclado:", new[] {
                "Global keyboard shortcuts:", "Raccourcis clavier globaux :", "Atajos de teclado globales:", "グローバルショートカット：",
                "全局快捷键：", "Globale Tastenkürzel:", "ग्लोबल कीबोर्ड शॉर्टकट:", "Scorciatoie da tastiera globali:" });
            E("ligar/desligar luzes", new[] { "lights on/off", "allumer/éteindre", "encender/apagar luces", "ライトのオン/オフ", "开/关灯光", "Licht an/aus", "लाइट चालू/बंद", "accendi/spegni luci" });
            E("próximo efeito", new[] { "next effect", "effet suivant", "siguiente efecto", "次のエフェクト", "下一个效果", "nächster Effekt", "अगला इफ़ेक्ट", "effetto successivo" });
            E("próximo tema", new[] { "next theme", "thème suivant", "siguiente tema", "次のテーマ", "下一个主题", "nächstes Thema", "अगली थीम", "tema successivo" });
            E("diminuir / aumentar intensidade", new[] {
                "brightness down / up", "baisser / augmenter l'intensité", "bajar / subir intensidad", "明るさを下げる / 上げる", "降低 / 提高亮度",
                "Helligkeit runter / hoch", "चमक कम / ज़्यादा", "abbassa / alza intensità" });

            // Profiles tab
            E("Nome do perfil", new[] { "Profile name", "Nom du profil", "Nombre del perfil", "プロファイル名", "配置名称", "Profilname", "प्रोफ़ाइल का नाम", "Nome del profilo" });
            E("Ativar automaticamente com o app (opcional)", new[] {
                "Activate automatically with this app (optional)", "Activer automatiquement avec l'appli (facultatif)",
                "Activar automáticamente con la app (opcional)", "このアプリで自動的に有効化（任意）", "随此程序自动启用（可选）",
                "Automatisch mit dieser App aktivieren (optional)", "इस ऐप के साथ अपने-आप चालू करें (वैकल्पिक)", "Attiva automaticamente con l'app (facoltativo)" });
            E("Procurar...", new[] { "Browse...", "Parcourir...", "Buscar...", "参照...", "浏览...", "Durchsuchen...", "ब्राउज़ करें...", "Sfoglia..." });
            E("Programas (*.exe)|*.exe", new[] {
                "Programs (*.exe)|*.exe", "Programmes (*.exe)|*.exe", "Programas (*.exe)|*.exe", "プログラム (*.exe)|*.exe", "程序 (*.exe)|*.exe",
                "Programme (*.exe)|*.exe", "प्रोग्राम (*.exe)|*.exe", "Programmi (*.exe)|*.exe" });
            E("Salvar o visual atual neste perfil", new[] {
                "Save the current look to this profile", "Enregistrer l'apparence actuelle dans ce profil", "Guardar el aspecto actual en este perfil",
                "現在の見た目をこのプロファイルに保存", "将当前外观保存到此配置", "Aktuelles Aussehen in diesem Profil speichern",
                "मौजूदा लुक इस प्रोफ़ाइल में सहेजें", "Salva l'aspetto attuale in questo profilo" });
            E("Aplicar perfil selecionado", new[] {
                "Apply selected profile", "Appliquer le profil", "Aplicar perfil seleccionado", "選択したプロファイルを適用", "应用所选配置",
                "Profil anwenden", "चुनी गई प्रोफ़ाइल लागू करें", "Applica profilo selezionato" });
            E("Excluir perfil", new[] { "Delete profile", "Supprimer le profil", "Eliminar perfil", "プロファイルを削除", "删除配置", "Profil löschen", "प्रोफ़ाइल हटाएँ", "Elimina profilo" });
            E("Excluir o perfil \"{0}\"?", new[] {
                "Delete the profile \"{0}\"?", "Supprimer le profil « {0} » ?", "¿Eliminar el perfil \"{0}\"?", "プロファイル「{0}」を削除しますか？",
                "删除配置“{0}”？", "Profil „{0}“ löschen?", "प्रोफ़ाइल \"{0}\" हटाएँ?", "Eliminare il profilo \"{0}\"?" });
            E("Trocar de perfil sozinho quando o app estiver em primeiro plano", new[] {
                "Switch profiles automatically when the app is in the foreground", "Changer de profil automatiquement quand l'appli est au premier plan",
                "Cambiar de perfil solo cuando la app esté en primer plano", "アプリが前面にあるとき自動でプロファイルを切り替え",
                "程序在前台时自动切换配置", "Profil automatisch wechseln, wenn die App im Vordergrund ist",
                "ऐप सामने होने पर अपने-आप प्रोफ़ाइल बदलें", "Cambia profilo da solo quando l'app è in primo piano" });
            E("Ex.: crie um perfil \"Jogo\" com WASD destacado e associe ao .exe do jogo (como Diablo IV.exe). Ao sair do jogo, o visual normal volta sozinho.", new[] {
                "E.g.: create a \"Game\" profile with WASD highlighted and link it to the game's .exe (like Diablo IV.exe). When you leave the game, your normal look comes back automatically.",
                "Ex. : créez un profil « Jeu » avec WASD en évidence et associez-le au .exe du jeu (comme Diablo IV.exe). En quittant le jeu, l'apparence normale revient toute seule.",
                "Ej.: crea un perfil \"Juego\" con WASD resaltado y asócialo al .exe del juego (como Diablo IV.exe). Al salir del juego, el aspecto normal vuelve solo.",
                "例：WASDを強調した「ゲーム」プロファイルを作り、ゲームの.exe（Diablo IV.exe など）に関連付けます。ゲームを終えると通常の見た目に自動で戻ります。",
                "例如：创建一个突出显示 WASD 的“游戏”配置，并关联到游戏的 .exe（如 Diablo IV.exe）。退出游戏后会自动恢复正常外观。",
                "Beispiel: Erstelle ein Profil „Spiel“ mit hervorgehobenem WASD und verknüpfe es mit der .exe des Spiels (z. B. Diablo IV.exe). Nach dem Spiel kehrt das normale Aussehen automatisch zurück.",
                "उदाहरण: WASD हाइलाइट के साथ \"Game\" प्रोफ़ाइल बनाएँ और उसे गेम की .exe (जैसे Diablo IV.exe) से जोड़ें। गेम से बाहर आने पर सामान्य लुक अपने-आप लौट आता है।",
                "Es.: crea un profilo \"Gioco\" con WASD evidenziato e associalo al .exe del gioco (come Diablo IV.exe). Quando esci dal gioco, l'aspetto normale torna da solo." });
            E("Digite um nome para o perfil.", new[] {
                "Enter a name for the profile.", "Saisissez un nom pour le profil.", "Escribe un nombre para el perfil.", "プロファイル名を入力してください。",
                "请输入配置名称。", "Gib einen Namen für das Profil ein.", "प्रोफ़ाइल के लिए नाम लिखें।", "Inserisci un nome per il profilo." });
            E("Não foi possível salvar: ", new[] {
                "Could not save: ", "Impossible d'enregistrer : ", "No se pudo guardar: ", "保存できませんでした: ", "无法保存：",
                "Speichern fehlgeschlagen: ", "सहेजा नहीं जा सका: ", "Impossibile salvare: " });

            // Tray and status
            E("Abrir", new[] { "Open", "Ouvrir", "Abrir", "開く", "打开", "Öffnen", "खोलें", "Apri" });
            E("Ligar / desligar luzes", new[] { "Lights on / off", "Allumer / éteindre", "Encender / apagar luces", "ライトのオン / オフ", "开 / 关灯光", "Licht an / aus", "लाइट चालू / बंद", "Accendi / spegni luci" });
            E("Próximo tema", new[] { "Next theme", "Thème suivant", "Siguiente tema", "次のテーマ", "下一个主题", "Nächstes Thema", "अगली थीम", "Tema successivo" });
            E("Sair", new[] { "Exit", "Quitter", "Salir", "終了", "退出", "Beenden", "बाहर निकलें", "Esci" });
            E("Continua rodando aqui na bandeja.", new[] {
                "Still running here in the tray.", "Continue de tourner ici, dans la barre.", "Sigue funcionando aquí en la bandeja.", "トレイで動作中です。",
                "仍在托盘中运行。", "Läuft weiter hier im Tray.", "यह यहाँ ट्रे में चल रहा है।", "Continua a funzionare qui nell'area di notifica." });
            E("perfil automático: ", new[] { "auto profile: ", "profil auto : ", "perfil automático: ", "自動プロファイル: ", "自动配置：", "Auto-Profil: ", "ऑटो प्रोफ़ाइल: ", "profilo automatico: " });
            E("O Alien Keyboard Lights já está aberto (veja a bandeja do sistema).", new[] {
                "Alien Keyboard Lights is already running (check the system tray).",
                "Alien Keyboard Lights est déjà ouvert (voir la zone de notification).",
                "Alien Keyboard Lights ya está abierto (mira la bandeja del sistema).",
                "Alien Keyboard Lights はすでに起動しています（タスクトレイを確認してください）。",
                "Alien Keyboard Lights 已在运行（请查看系统托盘）。", "Alien Keyboard Lights läuft bereits (siehe Infobereich).",
                "Alien Keyboard Lights पहले से चल रहा है (सिस्टम ट्रे देखें)।", "Alien Keyboard Lights è già aperto (guarda l'area di notifica)." });

            // Engine status
            E("Procurando teclado...", new[] { "Looking for the keyboard...", "Recherche du clavier...", "Buscando el teclado...", "キーボードを検索中...", "正在查找键盘...", "Suche Tastatur...", "कीबोर्ड खोजा जा रहा है...", "Ricerca della tastiera..." });
            E("Teclado conectado", new[] { "Keyboard connected", "Clavier connecté", "Teclado conectado", "キーボード接続済み", "键盘已连接", "Tastatur verbunden", "कीबोर्ड जुड़ा है", "Tastiera connessa" });
            E(" · chassi conectado", new[] { " · chassis connected", " · châssis connecté", " · chasis conectado", " · 本体ライト接続済み", " · 机身已连接", " · Gehäuse verbunden", " · चेसिस जुड़ा है", " · telaio connesso" });
            E("Teclado parou de responder, reconectando...", new[] {
                "Keyboard stopped responding, reconnecting...", "Le clavier ne répond plus, reconnexion...", "El teclado dejó de responder, reconectando...",
                "キーボードが応答しません。再接続中...", "键盘无响应，正在重新连接...", "Tastatur reagiert nicht, verbinde neu...",
                "कीबोर्ड जवाब नहीं दे रहा, फिर से जोड़ रहे हैं...", "La tastiera non risponde, riconnessione..." });
            E("Teclado AlienFX não encontrado", new[] {
                "AlienFX keyboard not found", "Clavier AlienFX introuvable", "No se encontró el teclado AlienFX", "AlienFXキーボードが見つかりません",
                "未找到 AlienFX 键盘", "AlienFX-Tastatur nicht gefunden", "AlienFX कीबोर्ड नहीं मिला", "Tastiera AlienFX non trovata" });
            E("Erro no efeito: ", new[] { "Effect error: ", "Erreur d'effet : ", "Error en el efecto: ", "エフェクトエラー: ", "效果错误：", "Effektfehler: ", "इफ़ेक्ट त्रुटि: ", "Errore nell'effetto: " });
            E("Bateria ", new[] { "Battery ", "Batterie ", "Batería ", "バッテリー ", "电池 ", "Akku ", "बैटरी ", "Batteria " });
            E("Áudio ", new[] { "Audio ", "Audio ", "Audio ", "オーディオ ", "音频 ", "Audio ", "ऑडियो ", "Audio " });
            return d;
        }
    }
}
