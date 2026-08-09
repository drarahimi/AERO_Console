using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace AERO_Console
{

    /// <summary>
/// A small, non-blocking notification for things that don't need a decision or even
/// acknowledgment - "exported successfully", "project copied", etc. Fades in near the bottom-
/// right corner of the active window, stays a few seconds, fades out, and closes itself; the
/// user never has to click anything. Use AppMessageBox instead for anything that needs a
/// response (confirmations, errors, questions).
/// </summary>
    public sealed class AppToast
    {
        private AppToast()
        {
        }

        public static void Show(string message, MessageBoxIcon icon = MessageBoxIcon.Information, int durationMs = 2600)
        {
            var owner = Form.ActiveForm;
            var toast = new AppToastForm(message, icon, durationMs, owner, null);
            toast.Show();
        }

        /// <summary>Shows a toast with a clickable action link (e.g. "Open"). Stays up longer and
        /// pauses its auto-dismiss while hovered, so the user has time to click.</summary>
        public static void Show(string message, Action action, string actionText,
            MessageBoxIcon icon = MessageBoxIcon.Information, int durationMs = 5200)
        {
            var owner = Form.ActiveForm;
            var toast = new AppToastForm(message, icon, durationMs, owner,
                new[] { (actionText, action) });
            toast.Show();
        }

        /// <summary>Convenience for "file exported" toasts: adds "Open" (launch the saved file with
        /// its default app) and "Show in folder" (reveal it in File Explorer) links.</summary>
        public static void ShowExported(string message, string filePath, int durationMs = 5200)
        {
            var owner = Form.ActiveForm;
            var links = new (string, Action)[]
            {
                ("Open", () => OpenPath(filePath)),
                ("Show in folder", () => RevealPath(filePath)),
            };
            var toast = new AppToastForm(message, MessageBoxIcon.Information, durationMs, owner, links);
            toast.Show();
        }

        private static void OpenPath(string path)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                AppMessageBox.Show("Could not open the file:\n" + ex.Message, "Open File",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Opens File Explorer with the file selected (highlighted) in its containing folder.
        private static void RevealPath(string path)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe",
                    $"/select,\"{path}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                AppMessageBox.Show("Could not open the folder:\n" + ex.Message, "Show in Folder",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }

    internal class AppToastForm : Form
    {

        private readonly Timer _showTimer = new Timer();
        private readonly Timer _fadeTimer = new Timer();
        private bool _fadingOut = false;

        // A toast with an action link shouldn't grab focus from the plot/editor the user is in.
        protected override bool ShowWithoutActivation => true;

        public AppToastForm(string message, MessageBoxIcon icon, int durationMs, Form owner,
            (string text, Action action)[] links)
        {
            // See the same fix on AppMessageBoxDialog: this form has no Designer-generated
            // InitializeComponent, so AutoScaleMode.Font (the default) has no proper baseline to
            // rescale against and can silently distort the hand-computed pixel layout below.
            AutoScaleMode = AutoScaleMode.None;

            bool isDark = My.MySettingsProperty.Settings.DarkTheme;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            BackColor = isDark ? Color.FromArgb(45, 45, 45) : Color.FromArgb(250, 250, 250);
            Opacity = 0.0d;

            var fg = isDark ? Color.White : Color.Black;
            Color accent;
            string glyph;
            switch (icon)
            {
                case MessageBoxIcon.Warning:
                    {
                        accent = Color.FromArgb(230, 150, 20);
                        glyph = "!";
                        break;
                    }
                case MessageBoxIcon.Error:
                    {
                        accent = Color.FromArgb(210, 60, 60);
                        glyph = Conversions.ToString(Strings.ChrW(0xD7));
                        break;
                    }

                default:
                    {
                        accent = Color.FromArgb(60, 170, 90);
                        glyph = Conversions.ToString(Strings.ChrW(0x2713)); // checkmark
                        break;
                    }
            }

            var stripe = new Panel() { Dock = DockStyle.Left, Width = 4, BackColor = accent };
            Controls.Add(stripe);

            var iconLbl = new Label()
            {
                Text = glyph,
                AutoSize = false,
                Size = new Size(26, 26),
                Location = new Point(14, 13),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = accent,
                Font = new Font("Segoe UI", 13.0f, FontStyle.Bold)
            };
            Controls.Add(iconLbl);

            // Explicitly measured rather than AutoSize=True - see the comment on AppMessageBoxDialog's
            // message Label for why relying on a freshly-constructed AutoSize control's computed
            // size can be wrong before it's been through a real layout pass.
            var msgFont = new Font("Segoe UI", 9.5f);
            const int maxTextWidth = 280;
            var measured = TextRenderer.MeasureText(message, msgFont, new Size(maxTextWidth, int.MaxValue), TextFormatFlags.WordBreak);

            var msgLbl = new Label()
            {
                Text = message,
                AutoSize = false,
                Size = new Size(Math.Min(maxTextWidth, measured.Width + 4), measured.Height + 4),
                Location = new Point(48, 15),
                ForeColor = fg,
                Font = msgFont
            };
            Controls.Add(msgLbl);

            // Optional clickable action links (e.g. "Open", "Show in folder"), laid out in a row
            // under the message.
            bool hasLinks = links is not null && links.Length > 0;
            int linksRight = 0;
            int linksBottom = msgLbl.Bottom;
            if (hasLinks)
            {
                var linkFont = new Font("Segoe UI", 9.5f, FontStyle.Bold);
                int x = 48;
                int y = msgLbl.Bottom + 6;
                for (int i = 0; i < links.Length; i++)
                {
                    var (text, action) = links[i];
                    if (action is null || string.IsNullOrEmpty(text))
                        continue;

                    // A subtle separator between links.
                    if (x > 48)
                    {
                        var sep = new Label()
                        {
                            Text = "|",
                            AutoSize = false,
                            Size = new Size(8, 20),
                            Location = new Point(x, y),
                            TextAlign = ContentAlignment.MiddleCenter,
                            ForeColor = Color.FromArgb(120, fg),
                            Font = linkFont
                        };
                        Controls.Add(sep);
                        x = sep.Right + 4;
                    }

                    var lm = TextRenderer.MeasureText(text, linkFont);
                    var linkLbl = new LinkLabel()
                    {
                        Text = text,
                        AutoSize = false,
                        Size = new Size(lm.Width + 6, lm.Height + 4),
                        Location = new Point(x, y),
                        Font = linkFont,
                        LinkBehavior = LinkBehavior.HoverUnderline,
                        LinkColor = accent,
                        ActiveLinkColor = accent,
                        VisitedLinkColor = accent,
                        TabStop = false
                    };
                    var act = action; // capture per-iteration
                    linkLbl.LinkClicked += (s, e) =>
                    {
                        try { act(); }
                        finally { Close(); }
                    };
                    Controls.Add(linkLbl);
                    x = linkLbl.Right + 6;
                    linksRight = Math.Max(linksRight, linkLbl.Right);
                    linksBottom = Math.Max(linksBottom, linkLbl.Bottom);
                }
            }

            int contentRight = Math.Max(msgLbl.Right, linksRight);
            int contentBottom = Math.Max(msgLbl.Bottom, linksBottom);
            int w = Math.Max(180, contentRight + 16);
            int h = Math.Max(52, contentBottom + 14);
            ClientSize = new Size(w, h);

            // Pause the auto-dismiss while the user hovers (so they can reach the links), resume on
            // leave. Attached to the form and every child, since children capture the mouse.
            if (hasLinks)
                HookHover(this);

            var workArea = owner is not null ? Screen.FromControl(owner).WorkingArea : Screen.PrimaryScreen.WorkingArea;
            Location = new Point(workArea.Right - Width - 20, workArea.Bottom - Height - 20);

            Load += (s, e) => _fadeTimer.Start();

            _fadeTimer.Interval = 30;
            _fadeTimer.Tick += FadeTick;

            _showTimer.Interval = durationMs;
            _showTimer.Tick += (s, e) =>
                {
                    _showTimer.Stop();
                    _fadingOut = true;
                    _fadeTimer.Start();
                };
        }

        // Recursively wire mouse enter/leave on the form and every child so hovering anywhere over
        // the toast pauses its dismissal (and cancels a fade already in progress).
        private void HookHover(Control c)
        {
            c.MouseEnter += OnHoverEnter;
            c.MouseLeave += OnHoverLeave;
            foreach (Control child in c.Controls)
                HookHover(child);
        }

        private void OnHoverEnter(object sender, EventArgs e)
        {
            _showTimer.Stop();
            if (_fadingOut)
            {
                // User came back while it was fading out - restore to fully visible.
                _fadingOut = false;
                _fadeTimer.Stop();
                Opacity = 1.0d;
            }
        }

        private void OnHoverLeave(object sender, EventArgs e)
        {
            // Moving between the form and its children fires spurious leaves; only resume once the
            // cursor is genuinely outside the toast.
            if (!ClientRectangle.Contains(PointToClient(Cursor.Position)) && !_fadingOut)
                _showTimer.Start();
        }

        private void FadeTick(object sender, EventArgs e)
        {
            if (!_fadingOut)
            {
                Opacity = Math.Min(1.0d, Opacity + 0.12d);
                if (Opacity >= 1.0d)
                {
                    _fadeTimer.Stop();
                    _showTimer.Start();
                }
            }
            else
            {
                Opacity = Math.Max(0.0d, Opacity - 0.08d);
                if (Opacity <= 0.0d)
                {
                    _fadeTimer.Stop();
                    Close();
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _showTimer.Dispose();
            _fadeTimer.Dispose();
            base.OnFormClosed(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var p = new Pen(Color.FromArgb(60, 0, 0, 0)))
            {
                e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
            }
        }
    }
}