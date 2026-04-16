using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Win11Optimizer
{
    // ── Theme definition ──────────────────────────────────────────────────
    public class AppTheme
    {
        public Color BG, SURFACE, CARD, BORDER, ACCENT, ACCENT2, WARN, DANGER, TEXT, TEXTDIM;
        public string Name;

        public static AppTheme Dark => new AppTheme
        {
            Name    = "Dark",
            BG      = Color.FromArgb(10, 10, 14),
            SURFACE = Color.FromArgb(18, 18, 24),
            CARD    = Color.FromArgb(24, 24, 32),
            BORDER  = Color.FromArgb(42, 42, 58),
            ACCENT  = Color.FromArgb(0, 210, 140),
            ACCENT2 = Color.FromArgb(0, 160, 255),
            WARN    = Color.FromArgb(255, 180, 0),
            DANGER  = Color.FromArgb(255, 70, 70),
            TEXT    = Color.FromArgb(230, 230, 240),
            TEXTDIM = Color.FromArgb(120, 120, 140),
        };

        public static AppTheme Light => new AppTheme
        {
            Name    = "Light",
            BG      = Color.FromArgb(242, 243, 247),
            SURFACE = Color.FromArgb(255, 255, 255),
            CARD    = Color.FromArgb(250, 250, 253),
            BORDER  = Color.FromArgb(210, 212, 225),
            ACCENT  = Color.FromArgb(0, 170, 110),
            ACCENT2 = Color.FromArgb(0, 120, 210),
            WARN    = Color.FromArgb(200, 130, 0),
            DANGER  = Color.FromArgb(210, 50, 50),
            TEXT    = Color.FromArgb(20, 22, 35),
            TEXTDIM = Color.FromArgb(110, 115, 140),
        };
    }

    public class MainForm : Form
    {
        // ── Current theme (default: dark) ─────────────────────────────────
        AppTheme T = AppTheme.Dark;

        // ── Fonts ─────────────────────────────────────────────────────────
        static readonly Font FONT_HEAD  = new Font("Segoe UI", 22f, FontStyle.Bold);
        static readonly Font FONT_SUB   = new Font("Segoe UI", 9f,  FontStyle.Regular);
        static readonly Font FONT_LABEL = new Font("Segoe UI", 12f, FontStyle.Bold);
        static readonly Font FONT_BODY  = new Font("Segoe UI", 11f, FontStyle.Regular);
        static readonly Font FONT_LOG   = new Font("Consolas", 11f, FontStyle.Regular);
        static readonly Font FONT_BTN   = new Font("Segoe UI", 11f, FontStyle.Bold);

        // ── Controls ─────────────────────────────────────────────────────
#pragma warning disable CS8618
        CheckBox chkPerf, chkPrivacy, chkResponsive, chkGaming, chkNetwork, chkBloat;
        GlowButton btnRunSelected, btnRunAll;
        DarkRichTextBox logBox;
        Panel progressBar;
        Label lblStatus;
        Panel sideAccent;
        Label _passLabel, _failLabel;
        ThemeToggleButton _themeBtn;

        // All themed controls we need to repaint on theme switch
        List<Action> _themeApplicators = new();
        List<Panel>   _themedPanels    = new();
        List<Label>   _themedLabels    = new();
        List<CheckBox> _themedChecks   = new();
#pragma warning restore CS8618

        int _totalTweaks, _doneTweaks;

        // Undo buttons — one per category
        GlowButton _undoPerf, _undoPrivacy, _undoResponsive, _undoGaming, _undoNetwork;

        public MainForm()
        {
            InitUI();
        }

        // ── Apply theme to all registered controls ────────────────────────
        void ApplyTheme()
        {
            BackColor = T.BG;
            ForeColor = T.TEXT;
            sideAccent.BackColor = T.ACCENT;
            logBox.BackColor     = T.BG;
            logBox.ForeColor     = T.ACCENT;
            lblStatus.BackColor  = T.BG;
            lblStatus.ForeColor  = T.TEXTDIM;
            progressBar.BackColor = T.ACCENT;
            if (progressBar.Parent != null)
                progressBar.Parent.BackColor = T.BORDER;

            foreach (var apply in _themeApplicators) apply();

            // Refresh all glow buttons with new theme accent colours
            foreach (Control c in GetAllControls(this))
            {
                if (c is GlowButton gb) gb.ApplyTheme(T);
                if (c is ThemeToggleButton tb) tb.IsDark = T.Name == "Dark";
            }

            Invalidate(true);
        }

        IEnumerable<Control> GetAllControls(Control root)
        {
            foreach (Control c in root.Controls)
            {
                yield return c;
                foreach (var child in GetAllControls(c)) yield return child;
            }
        }

        void InitUI()
        {
            Text            = "Win11 Optimizer";
            Size            = new Size(820, 680);
            MinimumSize     = new Size(820, 680);
            StartPosition   = FormStartPosition.CenterScreen;
            BackColor       = T.BG;
            ForeColor       = T.TEXT;
            Font            = FONT_BODY;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox     = true;

            // ── Left accent bar ───────────────────────────────────────────
            sideAccent = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = 4,
                BackColor = T.ACCENT
            };
            Controls.Add(sideAccent);

            // ── Outer layout: header on top, body in middle, log at bottom ─
            var outer = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 1,
                RowCount    = 3,
                BackColor   = T.BG,
                Padding     = new Padding(0)
            };
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));
            Controls.Add(outer);

            _themeApplicators.Add(() => outer.BackColor = T.BG);

            // ── Header ────────────────────────────────────────────────────
            var header = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = T.SURFACE
            };
            _themeApplicators.Add(() => header.BackColor = T.SURFACE);

            header.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode     = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                using var br = new LinearGradientBrush(
                    new Rectangle(0, header.Height - 2, header.Width, 2),
                    T.ACCENT, T.ACCENT2, LinearGradientMode.Horizontal);
                g.FillRectangle(br, 0, header.Height - 2, header.Width, 2);

                float titleSize = Math.Max(14f, header.Height * 0.30f);
                float subSize   = Math.Max(8f,  header.Height * 0.13f);

                using var titleFont = new Font("Segoe UI", titleSize, FontStyle.Bold);
                using var subFont   = new Font("Segoe UI", subSize,   FontStyle.Regular);
                using var textBrush = new SolidBrush(T.TEXT);
                using var dimBrush  = new SolidBrush(T.TEXTDIM);

                g.DrawString("WIN11  OPTIMIZER", titleFont, textBrush, new PointF(20, header.Height * 0.12f));
                g.DrawString("Performance · Privacy · Gaming · Network", subFont, dimBrush, new PointF(24, header.Height * 0.62f));
            };

            // ── Theme toggle button in header ─────────────────────────────
            _themeBtn = new ThemeToggleButton { IsDark = true };
            _themeBtn.Size     = new Size(80, 34);
            _themeBtn.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
            _themeBtn.Click   += (s, e) =>
            {
                T = T.Name == "Dark" ? AppTheme.Light : AppTheme.Dark;
                _themeBtn.IsDark = T.Name == "Dark";
                ApplyTheme();
            };

            header.SizeChanged += (s, e) =>
            {
                _themeBtn.Location = new Point(header.Width - _themeBtn.Width - 14,
                                               (header.Height - _themeBtn.Height) / 2);
            };
            header.Controls.Add(_themeBtn);
            outer.Controls.Add(header, 0, 0);

            // ── Body ──────────────────────────────────────────────────────
            var body = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 1,
                BackColor   = T.BG,
                Padding     = new Padding(8)
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            outer.Controls.Add(body, 0, 1);
            _themeApplicators.Add(() => body.BackColor = T.BG);

            // ── Left column ───────────────────────────────────────────────
            var leftCol = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 1,
                RowCount    = 3,
                BackColor   = T.BG,
                Padding     = new Padding(0, 0, 4, 0)
            };
            leftCol.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            leftCol.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            leftCol.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            body.Controls.Add(leftCol, 0, 0);
            _themeApplicators.Add(() => leftCol.BackColor = T.BG);

            // Select tweaks card
            var selectCard = MakeCardDock("SELECT TWEAKS");
            leftCol.Controls.Add(selectCard, 0, 0);

            var checkContainer = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = T.CARD,
                Padding   = new Padding(10, 34, 10, 10)
            };
            _themeApplicators.Add(() => checkContainer.BackColor = T.CARD);
            selectCard.Controls.Add(checkContainer);

            var checkLayout = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 1,
                RowCount    = 6,
                BackColor   = T.CARD
            };
            _themeApplicators.Add(() => checkLayout.BackColor = T.CARD);
            for (int i = 0; i < 6; i++)
                checkLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 6));
            checkContainer.Controls.Add(checkLayout);

            chkPerf       = MakeCheckRowDock(checkLayout, 0, "⚡  Performance",       "Power plan, NTFS, visual effects, startup",  "Performance",    out _undoPerf);
            chkPrivacy    = MakeCheckRowDock(checkLayout, 1, "🔒  Privacy & Telemetry","Disable tracking, ad ID, data collection",   "Privacy",         out _undoPrivacy);
            chkResponsive = MakeCheckRowDock(checkLayout, 2, "🖥  Responsiveness",     "Menu speed, shutdown timers, high-res clock", "Responsiveness",  out _undoResponsive);
            chkGaming     = MakeCheckRowDock(checkLayout, 3, "🎮  Gaming",             "HAGS, Game Mode, priority, DVR off",          "Gaming",          out _undoGaming);
            chkNetwork    = MakeCheckRowDock(checkLayout, 4, "🌐  Network",            "Nagle off, TCP tuning, throttle index",       "Network",         out _undoNetwork);
            chkBloat      = MakeCheckRowDock(checkLayout, 5, "🗑  Remove Bloatware",   "Strips pre-installed junk & ads",             "Bloatware",       out _);
            chkPerf.Checked = chkPrivacy.Checked = chkResponsive.Checked = true;

            // Wire undo buttons
            _undoPerf.Click       += async (s, e) => await RunUndo("Performance",    TweakEngine.UndoPerformanceTweaks,    _undoPerf);
            _undoPrivacy.Click    += async (s, e) => await RunUndo("Privacy",        TweakEngine.UndoPrivacyTweaks,        _undoPrivacy);
            _undoResponsive.Click += async (s, e) => await RunUndo("Responsiveness", TweakEngine.UndoResponsivenessTweaks, _undoResponsive);
            _undoGaming.Click     += async (s, e) => await RunUndo("Gaming",         TweakEngine.UndoGamingTweaks,         _undoGaming);
            _undoNetwork.Click    += async (s, e) => await RunUndo("Network",        TweakEngine.UndoNetworkTweaks,        _undoNetwork);

            TweakEngine.LoadBackups();
            RefreshUndoButtons();

            // Buttons row
            var btnPanel = new Panel { Dock = DockStyle.Fill, BackColor = T.BG, Padding = new Padding(0, 6, 0, 0) };
            _themeApplicators.Add(() => btnPanel.BackColor = T.BG);
            leftCol.Controls.Add(btnPanel, 0, 1);

            btnRunSelected = new GlowButton("RUN SELECTED", T.ACCENT,  new Rectangle(0, 0, 0, 42), T);
            btnRunAll      = new GlowButton("RUN ALL",      T.ACCENT2, new Rectangle(0, 0, 0, 42), T);

            btnRunSelected.Dock  = DockStyle.Left;
            btnRunAll.Dock       = DockStyle.Left;
            btnRunSelected.Width = 160;
            btnRunAll.Width      = 130;
            btnRunSelected.Margin = new Padding(0, 0, 8, 0);

            btnRunSelected.Click += async (s, e) => await RunTweaks(selectedOnly: true);
            btnRunAll.Click      += async (s, e) => await RunTweaks(selectedOnly: false);
            btnPanel.Controls.Add(btnRunAll);
            btnPanel.Controls.Add(btnRunSelected);

            // Progress + status row
            var progPanel = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                BackColor   = T.BG,
                ColumnCount = 1,
                RowCount    = 2,
                Padding     = new Padding(0, 6, 0, 0)
            };
            progPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
            progPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            leftCol.Controls.Add(progPanel, 0, 2);
            _themeApplicators.Add(() => progPanel.BackColor = T.BG);

            var progBg = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = T.BORDER
            };
            _themeApplicators.Add(() => progBg.BackColor = T.BORDER);
            progressBar = new Panel { Bounds = new Rectangle(0, 0, 0, 8), BackColor = T.ACCENT };
            progBg.Controls.Add(progressBar);
            progPanel.Controls.Add(progBg, 0, 0);

            lblStatus = new Label
            {
                Text      = "Ready.",
                Font      = FONT_BODY,
                ForeColor = T.TEXTDIM,
                AutoSize  = true,
                Dock      = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = T.BG
            };
            progPanel.Controls.Add(lblStatus, 0, 1);

            // ── Right column ──────────────────────────────────────────────
            var rightCol = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 1,
                RowCount    = 3,
                BackColor   = T.BG,
                Padding     = new Padding(4, 0, 0, 0)
            };
            rightCol.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            rightCol.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            rightCol.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
            body.Controls.Add(rightCol, 1, 0);
            _themeApplicators.Add(() => rightCol.BackColor = T.BG);

            // System info card
            var sysCard = MakeCardDock("SYSTEM INFO");
            rightCol.Controls.Add(sysCard, 0, 0);
            var sysInner = new Panel { Dock = DockStyle.Fill, BackColor = T.CARD, Padding = new Padding(10, 34, 10, 6) };
            _themeApplicators.Add(() => sysInner.BackColor = T.CARD);
            sysCard.Controls.Add(sysInner);
            BuildSystemInfoPanel(sysInner);

            // Notes card
            var infoCard = MakeCardDock("NOTES");
            rightCol.Controls.Add(infoCard, 0, 1);
            var infoInner = new Panel { Dock = DockStyle.Fill, BackColor = T.CARD, Padding = new Padding(10, 34, 10, 10) };
            _themeApplicators.Add(() => infoInner.BackColor = T.CARD);
            infoCard.Controls.Add(infoInner);
            AddInfoLineDock(infoInner, T.ACCENT,  "• Run as Administrator for registry & service access.");
            AddInfoLineDock(infoInner, T.WARN,    "• A reboot is required for HAGS & timer tweaks.");
            AddInfoLineDock(infoInner, T.TEXTDIM, "• Bloatware removal also strips provisioned packages.");
            AddGithubLink(infoInner);

            // Summary card
            var sumCard = MakeCardDock("LAST RUN SUMMARY");
            rightCol.Controls.Add(sumCard, 0, 2);
            var sumInner = new Panel { Dock = DockStyle.Fill, BackColor = T.CARD, Padding = new Padding(10, 34, 10, 10) };
            _themeApplicators.Add(() => sumInner.BackColor = T.CARD);
            sumCard.Controls.Add(sumInner);

            var sumBoxes = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                BackColor   = T.CARD,
                ColumnCount = 2,
                RowCount    = 1
            };
            _themeApplicators.Add(() => sumBoxes.BackColor = T.CARD);
            sumBoxes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            sumBoxes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            sumBoxes.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            sumInner.Controls.Add(sumBoxes);
            _passLabel = AddSummaryBoxDock(sumBoxes, 0, T.ACCENT, "0", "Succeeded", "✔");
            _failLabel = AddSummaryBoxDock(sumBoxes, 1, T.DANGER, "0", "Failed",    "✘");

            // ── Log box ───────────────────────────────────────────────────
            var logCard = MakeCardDock("OUTPUT LOG");
            outer.Controls.Add(logCard, 0, 2);

            var logInner = new Panel { Dock = DockStyle.Fill, BackColor = T.BG, Padding = new Padding(8, 36, 8, 8) };
            _themeApplicators.Add(() => logInner.BackColor = T.BG);
            logCard.Controls.Add(logInner);

            logBox = new DarkRichTextBox
            {
                Dock        = DockStyle.Fill,
                BackColor   = T.BG,
                ForeColor   = T.ACCENT,
                Font        = FONT_LOG,
                BorderStyle = BorderStyle.None,
                ReadOnly    = true,
                ScrollBars  = RichTextBoxScrollBars.Vertical,
                WordWrap    = false
            };
            logInner.Controls.Add(logBox);

            Log("Win11 Optimizer ready. Select tweaks and click Run.", T.TEXTDIM);
        }

        // ── Card factory (themed) ─────────────────────────────────────────
        Panel MakeCardDock(string title)
        {
            var card = new Panel { Dock = DockStyle.Fill, BackColor = T.CARD, Margin = new Padding(4) };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(T.BORDER);
                e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                using var br = new LinearGradientBrush(
                    new Rectangle(0, 0, card.Width, 2), T.ACCENT, T.ACCENT2, LinearGradientMode.Horizontal);
                e.Graphics.FillRectangle(br, 0, 0, card.Width, 2);
            };
            var lbl = new Label
            {
                Text      = title,
                Font      = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = T.TEXTDIM,
                AutoSize  = true,
                Location  = new Point(12, 10),
                BackColor = T.CARD
            };
            _themeApplicators.Add(() =>
            {
                card.BackColor = T.CARD;
                lbl.ForeColor  = T.TEXTDIM;
                lbl.BackColor  = T.CARD;
                card.Invalidate();
            });
            card.Controls.Add(lbl);
            return card;
        }

        CheckBox MakeCheckRowDock(TableLayoutPanel parent, int row, string title, string subtitle,
                                  string category, out GlowButton undoBtn)
        {
            var cell = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                BackColor   = T.CARD,
                ColumnCount = 2,
                RowCount    = 1
            };
            cell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            cell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            _themeApplicators.Add(() => cell.BackColor = T.CARD);

            var left = new Panel { Dock = DockStyle.Fill, BackColor = T.CARD };
            _themeApplicators.Add(() => left.BackColor = T.CARD);

            var chk = new CheckBox
            {
                Text      = title,
                Font      = FONT_LABEL,
                ForeColor = T.TEXT,
                AutoSize  = true,
                Location  = new Point(6, 4),
                BackColor = T.CARD,
                FlatStyle = FlatStyle.Flat
            };
            chk.FlatAppearance.BorderColor        = T.BORDER;
            chk.FlatAppearance.CheckedBackColor   = T.ACCENT;
            chk.FlatAppearance.MouseOverBackColor = T.CARD;
            _themeApplicators.Add(() =>
            {
                chk.ForeColor = T.TEXT;
                chk.BackColor = T.CARD;
                chk.FlatAppearance.BorderColor        = T.BORDER;
                chk.FlatAppearance.CheckedBackColor   = T.ACCENT;
                chk.FlatAppearance.MouseOverBackColor = T.CARD;
            });

            var desc = new Label
            {
                Text      = subtitle,
                Font      = FONT_BODY,
                ForeColor = T.TEXTDIM,
                AutoSize  = true,
                Location  = new Point(26, 24),
                BackColor = T.CARD
            };
            _themeApplicators.Add(() =>
            {
                desc.ForeColor = T.TEXTDIM;
                desc.BackColor = T.CARD;
            });

            left.Controls.Add(chk);
            left.Controls.Add(desc);

            var btnWrapper = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = T.CARD,
                Padding   = new Padding(4, 0, 4, 0)
            };
            _themeApplicators.Add(() => btnWrapper.BackColor = T.CARD);

            undoBtn = new GlowButton("↩ UNDO", T.DANGER, new Rectangle(0, 0, 72, 30), T)
            {
                Visible = TweakEngine.HasBackup(category)
            };

            var btn = undoBtn;
            btnWrapper.Resize += (s, e) =>
            {
                btn.Width    = btnWrapper.Width - 8;
                btn.Height   = 30;
                btn.Location = new Point(4, (btnWrapper.Height - btn.Height) / 2);
            };

            btnWrapper.Controls.Add(btn);
            cell.Controls.Add(left,       0, 0);
            cell.Controls.Add(btnWrapper, 1, 0);
            parent.Controls.Add(cell, 0, row);
            return chk;
        }

        void BuildSystemInfoPanel(Panel parent)
        {
            var layout = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                BackColor   = T.CARD
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
            _themeApplicators.Add(() => layout.BackColor = T.CARD);

            var keys = new[] { "OS", "CPU", "RAM", "GPU" };
            var keyLabels = new List<Label>();
            var valueLabels = new Dictionary<string, Label>();

            foreach (var key in keys)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
                layout.RowCount++;

                var kl = new Label
                {
                    Text         = key,
                    Font         = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor    = T.TEXTDIM,
                    Dock         = DockStyle.Fill,
                    TextAlign    = ContentAlignment.MiddleLeft,
                    BackColor    = T.CARD,
                    Padding      = new Padding(4, 0, 0, 0)
                };
                keyLabels.Add(kl);
                _themeApplicators.Add(() => { kl.ForeColor = T.TEXTDIM; kl.BackColor = T.CARD; });

                var valLbl = new Label
                {
                    Text         = "Loading…",
                    Font         = new Font("Segoe UI", 9f, FontStyle.Regular),
                    ForeColor    = T.TEXT,
                    Dock         = DockStyle.Fill,
                    TextAlign    = ContentAlignment.MiddleLeft,
                    BackColor    = T.CARD,
                    AutoEllipsis = true
                };
                _themeApplicators.Add(() => { valLbl.ForeColor = T.TEXT; valLbl.BackColor = T.CARD; });

                layout.Controls.Add(kl);
                layout.Controls.Add(valLbl);
                valueLabels[key] = valLbl;
            }

            parent.Controls.Add(layout);

            Task.Run(() =>
            {
                try
                {
                    string os  = ReadRegistry(
                        @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                        "ProductName", "Unknown Windows");
                    string build = ReadRegistry(
                        @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                        "CurrentBuildNumber", "");
                    string displayVersion = ReadRegistry(
                        @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                        "DisplayVersion", "");
                    string cpu = ReadRegistry(
                        @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0",
                        "ProcessorNameString", "Unknown CPU").Trim();
                    string ram = RunAndCapture("powershell",
                        "-NoProfile -Command \"[math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory/1GB)\"");
                    ram = string.IsNullOrWhiteSpace(ram) ? "Unknown" : $"{ram.Trim()} GB";
                    string gpu = ReadRegistry(
                        @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Video", "", "");
                    if (string.IsNullOrWhiteSpace(gpu) || gpu == "Unknown")
                        gpu = RunAndCapture("powershell",
                            "-NoProfile -Command \"(Get-CimInstance Win32_VideoController | Select-Object -First 1).Name\"").Trim();
                    if (string.IsNullOrWhiteSpace(gpu)) gpu = "Unknown";

                    string osDisplay = string.IsNullOrWhiteSpace(displayVersion)
                        ? $"{os} (Build {build})"
                        : $"{os} {displayVersion} (Build {build})";

                    Invoke(new Action(() =>
                    {
                        valueLabels["OS"].Text  = osDisplay;
                        valueLabels["CPU"].Text = cpu;
                        valueLabels["RAM"].Text = ram;
                        valueLabels["GPU"].Text = gpu;
                    }));
                }
                catch (Exception ex)
                {
                    Invoke(new Action(() =>
                    {
                        foreach (var lbl in valueLabels.Values) lbl.Text = "Unavailable";
                        Debug.WriteLine($"[SYSINFO] {ex.Message}");
                    }));
                }
            });
        }

        static string ReadRegistry(string keyPath, string valueName, string fallback)
        {
            try { var val = Microsoft.Win32.Registry.GetValue(keyPath, valueName, null); return val?.ToString() ?? fallback; }
            catch { return fallback; }
        }

        static string RunAndCapture(string exe, string args)
        {
            try
            {
                var psi = new ProcessStartInfo(exe, args)
                { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true };
                using var p = Process.Start(psi);
                string output = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                return output;
            }
            catch { return ""; }
        }

        void AddInfoLineDock(Panel parent, Color col, string text)
        {
            var flow = parent.Controls.Count == 0
                ? new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, BackColor = T.CARD, WrapContents = false }
                : (FlowLayoutPanel)parent.Controls[0];
            if (parent.Controls.Count == 0) parent.Controls.Add(flow);
            _themeApplicators.Add(() => flow.BackColor = T.CARD);

            var lbl = new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", 11f, FontStyle.Regular),
                ForeColor = col,
                AutoSize  = true,
                BackColor = T.CARD,
                Margin    = new Padding(0, 4, 0, 4)
            };
            // For notes that use theme-relative colours, track them too
            _themeApplicators.Add(() => lbl.BackColor = T.CARD);
            flow.Controls.Add(lbl);
        }

        void AddGithubLink(Panel parent)
        {
            var flow = parent.Controls.Count == 0
                ? new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, BackColor = T.CARD, WrapContents = false }
                : (FlowLayoutPanel)parent.Controls[0];
            if (parent.Controls.Count == 0) parent.Controls.Add(flow);

            var link = new LinkLabel
            {
                Text      = "⭐  GitHub: github.com/ConnorCorn07/win11op",
                Font      = new Font("Segoe UI", 11f, FontStyle.Regular),
                AutoSize  = true,
                BackColor = T.CARD,
                Margin    = new Padding(0, 8, 0, 4)
            };
            link.LinkColor        = T.ACCENT2;
            link.ActiveLinkColor  = T.ACCENT;
            link.VisitedLinkColor = T.ACCENT2;
            link.LinkBehavior     = LinkBehavior.HoverUnderline;
            link.Click += (s, e) =>
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                { FileName = "https://github.com/ConnorCorn07/win11op", UseShellExecute = true });
            _themeApplicators.Add(() =>
            {
                link.BackColor        = T.CARD;
                link.LinkColor        = T.ACCENT2;
                link.ActiveLinkColor  = T.ACCENT;
                link.VisitedLinkColor = T.ACCENT2;
            });
            flow.Controls.Add(link);
        }

        Label AddSummaryBoxDock(TableLayoutPanel parent, int col, Color accentCol, string count, string title, string icon)
        {
            var box = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(30, accentCol.R, accentCol.G, accentCol.B),
                Margin    = new Padding(4)
            };
            // Re-capture accent for paint so it picks up theme changes
            box.Paint += (s, e) =>
            {
                var g = e.Graphics;
                using var stripeBr = new SolidBrush(accentCol);
                g.FillRectangle(stripeBr, 0, 0, 5, box.Height);
                using var pen = new Pen(Color.FromArgb(80, accentCol.R, accentCol.G, accentCol.B), 1.5f);
                g.DrawRectangle(pen, 1, 1, box.Width - 3, box.Height - 3);
            };

            var sub = new Label
            {
                Text      = title.ToUpper(),
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, accentCol.R, accentCol.G, accentCol.B),
                TextAlign = ContentAlignment.MiddleLeft,
                Dock      = DockStyle.Bottom,
                Height    = 30,
                BackColor = Color.FromArgb(30, accentCol.R, accentCol.G, accentCol.B),
                Padding   = new Padding(14, 0, 0, 0)
            };

            var middle = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 1,
                BackColor   = Color.FromArgb(30, accentCol.R, accentCol.G, accentCol.B)
            };
            middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

            var num = new Label
            {
                Text      = count,
                Font      = new Font("Segoe UI", 42f, FontStyle.Bold),
                ForeColor = accentCol,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(30, accentCol.R, accentCol.G, accentCol.B),
                Padding   = new Padding(14, 0, 0, 0)
            };

            var lblIcon = new Label
            {
                Text      = icon,
                Font      = new Font("Segoe UI", 32f, FontStyle.Regular),
                ForeColor = Color.FromArgb(70, accentCol.R, accentCol.G, accentCol.B),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock      = DockStyle.Fill,
                BackColor = Color.FromArgb(30, accentCol.R, accentCol.G, accentCol.B)
            };

            middle.Controls.Add(num,     0, 0);
            middle.Controls.Add(lblIcon, 1, 0);
            box.Controls.Add(sub);
            box.Controls.Add(middle);
            parent.Controls.Add(box, col, 0);
            return num;
        }

        // ── Undo ──────────────────────────────────────────────────────────
        void RefreshUndoButtons()
        {
            if (InvokeRequired) { Invoke(new Action(RefreshUndoButtons)); return; }
            _undoPerf.Visible       = TweakEngine.HasBackup("Performance");
            _undoPrivacy.Visible    = TweakEngine.HasBackup("Privacy");
            _undoResponsive.Visible = TweakEngine.HasBackup("Responsiveness");
            _undoGaming.Visible     = TweakEngine.HasBackup("Gaming");
            _undoNetwork.Visible    = TweakEngine.HasBackup("Network");
        }

        async Task RunUndo(string category, Func<List<TweakEngine.TweakResult>> undoAction, GlowButton btn)
        {
            btn.Enabled = false;
            Log($"↩ Undoing {category} tweaks…", T.WARN);
            var results = await Task.Run(undoAction);
            int ok  = results.Count(r => r.Success);
            int bad = results.Count(r => !r.Success);
            Log($"┌─ UNDO {category.ToUpper()}  ({ok} restored, {bad} failed)", T.TEXTDIM);
            foreach (var r in results)
            {
                if (r.Success) Log($"│  ✔  {r.Name}", T.ACCENT);
                else           Log($"│  ✘  {r.Name}: {r.Error}", T.DANGER);
            }
            Log($"└─────────────────────────────────────", T.BORDER);
            Log($"↩ {category} undone. Reboot recommended.", T.ACCENT);
            SetStatus($"{category} tweaks undone. Reboot recommended.", T.ACCENT);
            RefreshUndoButtons();
            btn.Enabled = true;
        }

        // ── Logging ───────────────────────────────────────────────────────
        void Log(string msg, Color? col = null)
        {
            if (InvokeRequired) { Invoke(new Action(() => Log(msg, col))); return; }
            logBox.SelectionColor = col ?? T.ACCENT;
            logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            logBox.ScrollToCaret();
        }

        void SetProgress(int done, int total)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetProgress(done, total))); return; }
            int w = total == 0 ? 0 : (int)((double)progressBar.Parent.Width * done / total);
            progressBar.Width = w;
            if (total > 0 && done < total) lblStatus.Text = $"Running… {done}/{total}";
            else if (total == 0)           lblStatus.Text = "Ready.";
        }

        void SetStatus(string msg, Color? col = null)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetStatus(msg, col))); return; }
            lblStatus.Text      = msg;
            lblStatus.ForeColor = col ?? T.TEXTDIM;
        }

        // ── Run tweaks ────────────────────────────────────────────────────
        async Task RunTweaks(bool selectedOnly)
        {
            btnRunSelected.Enabled = btnRunAll.Enabled = false;
            TweakEngine.ClearResults();
            logBox.Clear();
            progressBar.Width     = 0;
            progressBar.BackColor = T.WARN;
            _passLabel.Text       = "0";
            _failLabel.Text       = "0";

            bool doPerf    = !selectedOnly || chkPerf.Checked;
            bool doPrivacy = !selectedOnly || chkPrivacy.Checked;
            bool doRespond = !selectedOnly || chkResponsive.Checked;
            bool doGaming  = !selectedOnly || chkGaming.Checked;
            bool doNetwork = !selectedOnly || chkNetwork.Checked;
            bool doBloat   = !selectedOnly || chkBloat.Checked;

            _totalTweaks = (doPerf ? 9 : 0) + (doPrivacy ? 25 : 0) + (doRespond ? 8 : 0) +
                           (doGaming ? 10 : 0) + (doNetwork ? 8 : 0) + (doBloat ? 40 : 0);
            _doneTweaks = 0;
            SetProgress(0, _totalTweaks);

            int prevCount = 0;
            void LogSectionResults(string sectionName)
            {
                var all     = TweakEngine.GetResults();
                var section = all.Skip(prevCount).ToList();
                prevCount   = all.Count;
                int ok  = section.Count(r => r.Success);
                int bad = section.Count(r => !r.Success);
                Log($"┌─ {sectionName}  ({ok} ok, {bad} failed)", T.TEXTDIM);
                foreach (var r in section)
                {
                    if (r.Success) Log($"│  ✔  {r.Name}", T.ACCENT);
                    else           Log($"│  ✘  {r.Name}: {r.Error}", T.DANGER);
                }
                Log($"└─────────────────────────────────────", T.BORDER);
            }

            await Task.Run(() =>
            {
                void Tick(string msg) { _doneTweaks++; SetProgress(_doneTweaks, _totalTweaks); Log(msg, T.TEXTDIM); }

                if (doPerf)    { Tick("→ Applying Performance tweaks…");    TweakEngine.ApplyPerformanceTweaks();    Invoke(new Action(() => LogSectionResults("PERFORMANCE"))); }
                if (doPrivacy) { Tick("→ Applying Privacy & Telemetry tweaks…"); TweakEngine.ApplyPrivacyTweaks(); Invoke(new Action(() => LogSectionResults("PRIVACY & TELEMETRY"))); }
                if (doRespond) { Tick("→ Applying Responsiveness tweaks…"); TweakEngine.ApplySystemResponsiveness(); Invoke(new Action(() => LogSectionResults("RESPONSIVENESS"))); }
                if (doGaming)  { Tick("→ Applying Gaming tweaks…");         TweakEngine.ApplyGamingTweaks();         Invoke(new Action(() => LogSectionResults("GAMING"))); }
                if (doNetwork) { Tick("→ Applying Network tweaks…");        TweakEngine.ApplyNetworkTweaks();        Invoke(new Action(() => LogSectionResults("NETWORK"))); }
                if (doBloat)   { TweakEngine.RemoveBloatware(msg => { Tick(msg); }); Invoke(new Action(() => LogSectionResults("BLOATWARE REMOVAL"))); }
            });

            var results = TweakEngine.GetResults();
            int pass = results.Count(r => r.Success);
            int fail = results.Count(r => !r.Success);

            _passLabel.Text = pass.ToString();
            _failLabel.Text = fail.ToString();

            Log($"══ COMPLETE: {pass} succeeded, {fail} failed. Reboot recommended. ══", T.ACCENT);
            SetStatus($"Complete — {pass} succeeded, {fail} failed. Reboot recommended.", T.ACCENT);
            SetProgress(_totalTweaks, _totalTweaks);
            progressBar.BackColor = T.ACCENT;

            btnRunSelected.Enabled = btnRunAll.Enabled = true;
            RefreshUndoButtons();
            PromptReboot();
        }

        void PromptReboot()
        {
            var result = MessageBox.Show(
                "Some tweaks require a reboot to take full effect.\n\nWould you like to reboot now?",
                "Reboot Required", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result == DialogResult.Yes)
            {
                Log("Initiating reboot…", T.WARN);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                { FileName = "shutdown.exe", Arguments = "/r /t 10 /c \"Win11 Optimizer: Rebooting to apply tweaks.\"", UseShellExecute = false, CreateNoWindow = true });
                MessageBox.Show("Your PC will reboot in 10 seconds.\n\nTo cancel, open a command prompt and run:\n  shutdown /a",
                    "Rebooting in 10 seconds", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    // ── Theme-aware glow button ───────────────────────────────────────────
    public class GlowButton : Control
    {
        Color _accent;
        bool _hover, _pressed;
        AppTheme _theme;

        public GlowButton(string text, Color accent, Rectangle bounds, AppTheme theme = null)
        {
            Text    = text;
            _accent = accent;
            _theme  = theme ?? AppTheme.Dark;
            Bounds  = bounds;
            Font    = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            ForeColor   = Color.FromArgb(10, 10, 14);
            BackColor   = _theme.BG;
            Cursor      = Cursors.Hand;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer, true);
        }

        public void ApplyTheme(AppTheme t)
        {
            _theme    = t;
            BackColor = t.BG;
            // Re-map accent: UNDO buttons keep DANGER, run buttons keep their assigned accent
            // We keep _accent as-is (it was set at construction) — no auto-remap needed
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e) { _hover   = true;  Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { _hover   = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) { _pressed = true;  Invalidate(); }
        protected override void OnMouseUp(MouseEventArgs e)   { _pressed = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g  = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rc = new Rectangle(0, 0, Width, Height);

            Color fill = _pressed ? Color.FromArgb(180, _accent.R, _accent.G, _accent.B)
                       : _hover   ? _accent
                       : Color.FromArgb(220, _accent.R, _accent.G, _accent.B);

            using var br = new SolidBrush(fill);
            g.FillRectangle(br, rc);

            if (_hover && !_pressed)
            {
                using var pen = new Pen(Color.FromArgb(160, _accent.R, _accent.G, _accent.B), 1.5f);
                g.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);
            }

            // For light mode, ensure text is readable (dark text on light accent)
            Color textCol = _theme.Name == "Light"
                ? Color.FromArgb(20, 20, 30)
                : Color.FromArgb(10, 10, 14);

            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            using var tf = new SolidBrush(textCol);
            g.DrawString(Text, Font, tf, rc, sf);
        }
    }

    // ── Sun/Moon toggle button ────────────────────────────────────────────
    public class ThemeToggleButton : Control
    {
        bool _isDark = true;
        bool _hover;

        public bool IsDark
        {
            get => _isDark;
            set { _isDark = value; Invalidate(); }
        }

        public ThemeToggleButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.DoubleBuffer, true);
            Cursor    = Cursors.Hand;
            BackColor = Color.Transparent;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true;  Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g  = e.Graphics;
            g.SmoothingMode    = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Pill background
            Color pillBg  = _isDark  ? Color.FromArgb(_hover ? 60 : 40, 255, 255, 255)
                                      : Color.FromArgb(_hover ? 60 : 40, 0,   0,   0);
            Color pillBdr = _isDark  ? Color.FromArgb(80, 255, 255, 255)
                                      : Color.FromArgb(80, 0,   0,   0);
            using var bgBr  = new SolidBrush(pillBg);
            using var bdrPen = new Pen(pillBdr, 1.5f);
            g.FillRoundedRectangle(bgBr, 0, 0, Width - 1, Height - 1, 8);
            g.DrawRoundedRectangle(bdrPen, 0, 0, Width - 1, Height - 1, 8);

            // Icon + label
            string icon  = _isDark ? "🌙" : "☀";
            string label = _isDark ? " Dark" : " Light";
            Color textC  = _isDark ? Color.FromArgb(200, 200, 220) : Color.FromArgb(40, 40, 60);

            using var iconFont  = new Font("Segoe UI Emoji", 13f);
            using var labelFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            using var textBrush = new SolidBrush(textC);

            var iconSf  = new StringFormat { Alignment = StringAlignment.Far,   LineAlignment = StringAlignment.Center };
            var labelSf = new StringFormat { Alignment = StringAlignment.Near,  LineAlignment = StringAlignment.Center };

            int mid = Width / 2;
            g.DrawString(icon,  iconFont,  textBrush, new RectangleF(0, 0, mid, Height), iconSf);
            g.DrawString(label, labelFont, textBrush, new RectangleF(mid, 0, mid, Height), labelSf);
        }
    }

    // ── Graphics extension for rounded rects ─────────────────────────────
    static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush b, int x, int y, int w, int h, int r)
        {
            using var path = RoundedRect(x, y, w, h, r);
            g.FillPath(b, path);
        }
        public static void DrawRoundedRectangle(this Graphics g, Pen p, int x, int y, int w, int h, int r)
        {
            using var path = RoundedRect(x, y, w, h, r);
            g.DrawPath(p, path);
        }
        static System.Drawing.Drawing2D.GraphicsPath RoundedRect(int x, int y, int w, int h, int r)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(x, y, r * 2, r * 2, 180, 90);
            path.AddArc(x + w - r * 2, y, r * 2, r * 2, 270, 90);
            path.AddArc(x + w - r * 2, y + h - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(x, y + h - r * 2, r * 2, r * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // ── Dark scrollbar RichTextBox ────────────────────────────────────────
    public class DarkRichTextBox : RichTextBox
    {
        [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SetWindowTheme(Handle, "DarkMode_Explorer", null);
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.ThreadException += (s, e) => LogCrash(e.Exception);
                AppDomain.CurrentDomain.UnhandledException += (s, e) => LogCrash(e.ExceptionObject as Exception);
                Application.Run(new MainForm());
            }
            catch (Exception ex) { LogCrash(ex); }
        }

        static void LogCrash(Exception ex)
        {
            try
            {
                string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}]\n{ex}\n\n");
                MessageBox.Show($"Crash logged to:\n{logPath}\n\n{ex?.Message}", "Win11Optimizer — Startup Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { }
        }
    }
}