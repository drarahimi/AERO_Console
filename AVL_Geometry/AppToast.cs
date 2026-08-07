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
            var toast = new AppToastForm(message, icon, durationMs, owner);
            toast.Show();
        }
    }

    internal class AppToastForm : Form
    {

        private readonly Timer _showTimer = new Timer();
        private readonly Timer _fadeTimer = new Timer();
        private bool _fadingOut = false;

        public AppToastForm(string message, MessageBoxIcon icon, int durationMs, Form owner)
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

            int w = Math.Max(180, msgLbl.Right + 16);
            int h = Math.Max(52, msgLbl.Bottom + 14);
            ClientSize = new Size(w, h);

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