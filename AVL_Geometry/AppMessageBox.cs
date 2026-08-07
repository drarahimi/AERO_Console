using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;

namespace AERO_Console
{

    /// <summary>
/// Drop-in replacement for MessageBox.Show, styled to match the rest of the app (flat white/
/// WhiteSmoke buttons, Segoe UI, light/dark theme via the same My.Settings.DarkTheme flag every
/// other themed view in the app already reads) instead of the raw OS message box. Same call
/// signatures as MessageBox.Show, so existing call sites only need "MessageBox.Show(" swapped
/// for "AppMessageBox.Show(" - no argument changes required.
/// </summary>
    public sealed class AppMessageBox
    {
        private AppMessageBox()
        {
        }

        public static DialogResult Show(string text)
        {
            return ShowCore(text, "", MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);
        }

        public static DialogResult Show(string text, string caption)
        {
            return ShowCore(text, caption, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);
        }

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
        {
            return ShowCore(text, caption, buttons, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);
        }

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            return ShowCore(text, caption, buttons, icon, MessageBoxDefaultButton.Button1);
        }

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
        {
            return ShowCore(text, caption, buttons, icon, defaultButton);
        }

        private static DialogResult ShowCore(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton)
        {
            using (var dlg = new AppMessageBoxDialog(text, caption, buttons, icon, defaultButton))
            {
                var owner = Form.ActiveForm;
                if (owner is not null && !ReferenceEquals(owner, dlg))
                {
                    return dlg.ShowDialog(owner);
                }
                return dlg.ShowDialog();
            }
        }
    }

    /// <summary>
/// The actual dialog behind AppMessageBox.Show - not meant to be used directly, use
/// AppMessageBox.Show instead. Built entirely in code (no Designer) to match how most of this
/// app's UI is constructed.
/// </summary>
    internal class AppMessageBoxDialog : Form
    {

        public AppMessageBoxDialog(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon, MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1)
        {
            // Must be set before anything else: this form has no Designer-generated
            // InitializeComponent, so WinForms never gets a proper design-time baseline for
            // AutoScaleMode.Font (the default) to rescale against - left enabled, it can silently
            // rescale/reposition every control after the constructor runs, distorting or
            // collapsing the pixel layout computed below (this is what was making the message
            // text disappear).
            AutoScaleMode = AutoScaleMode.None;

            bool isDark = My.MySettingsProperty.Settings.DarkTheme;
            var fg = isDark ? Color.White : Color.Black;
            var bg = isDark ? Color.FromArgb(32, 32, 32) : Color.White;

            // Me.Text, not Text - "Text" alone here would resolve to the "text" constructor
            // parameter (VB is case-insensitive, and a parameter shadows an inherited member of
            // the same name), silently overwriting the message with the caption instead of
            // setting the form's title bar. This was the actual bug behind the message body
            // appearing to show the caption instead of the real message.
            Text = string.IsNullOrEmpty(caption) ? "AERO Console" : caption;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9.0f);
            BackColor = bg;
            KeyPreview = true;

            // --- Icon (owner-drawn circle + glyph, no external image assets - consistent with
            // the rest of the app's owner-drawn tooltips/gizmos rather than embedding bitmaps) ---
            string iconChar = "";
            var iconColor = fg;
            switch (icon)
            {
                case MessageBoxIcon.Information:
                    {
                        iconChar = "i";
                        iconColor = Color.FromArgb(0, 120, 215);
                        break;
                    }
                case MessageBoxIcon.Warning:
                    {
                        iconChar = "!";
                        iconColor = Color.FromArgb(230, 150, 20);
                        break;
                    }
                case MessageBoxIcon.Error:
                    {
                        iconChar = Conversions.ToString(Strings.ChrW(0xD7));
                        iconColor = Color.FromArgb(210, 60, 60); // multiplication sign, reads as a clean "x"
                        break;
                    }
                case MessageBoxIcon.Question:
                    {
                        iconChar = "?";
                        iconColor = Color.FromArgb(0, 120, 215);
                        break;
                    }
            }

            int leftMargin = 20;
            int textLeft = 20;

            if (!string.IsNullOrEmpty(iconChar))
            {
                var pic = new PictureBox() { Size = new Size(40, 40), Location = new Point(leftMargin, 22) };
                pic.Paint += (s, e) =>
                    {
                        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                        using (var b = new SolidBrush(iconColor))
                        {
                            e.Graphics.FillEllipse(b, 0, 0, 40, 40);
                        }
                        using (var f = new Font("Georgia", 17.0f, FontStyle.Bold))
                        {
                            var sz = e.Graphics.MeasureString(iconChar, f);
                            e.Graphics.DrawString(iconChar, f, Brushes.White, (40f - sz.Width) / 2.0f, (40f - sz.Height) / 2.0f - 1f);
                        }
                    };
                Controls.Add(pic);
                textLeft = pic.Right + 16;
            }

            // --- Message text ---
            // Explicitly measured (AutoSize=False, sized from TextRenderer.MeasureText) instead of
            // relying on Label.AutoSize: reading a freshly-constructed AutoSize control's computed
            // .Height/.Bottom immediately after construction can return a stale pre-layout value
            // (the same class of bug fixed for the Polar tab's overlapping fields) - for a short
            // message that stale value is close enough to go unnoticed, but for a long multi-
            // paragraph message (e.g. the Dynamics tab's stability tips) it can be drastically too
            // small, so the button row below ends up positioned over most of the actual text instead
            // of below it.
            var msgFont = new Font("Segoe UI", 9.5f);
            const int maxTextWidth = 360;
            var measured = TextRenderer.MeasureText(text, msgFont, new Size(maxTextWidth, int.MaxValue), TextFormatFlags.WordBreak);

            var lbl = new Label()
            {
                Text = text,
                AutoSize = false,
                Size = new Size(Math.Min(maxTextWidth, measured.Width + 4), measured.Height + 4),
                Location = new Point(textLeft, 26),
                ForeColor = fg,
                Font = msgFont
            };
            Controls.Add(lbl);

            int bottomOfContent = Math.Max(lbl.Bottom, 22 + 40) + 24;
            int rightOfContent = Math.Max(lbl.Right, textLeft) + leftMargin;

            // --- Buttons (flat, matching the app's established button styling) ---
            var btnPanel = new FlowLayoutPanel()
            {
                FlowDirection = FlowDirection.RightToLeft,
                Location = new Point(0, bottomOfContent),
                Size = new Size(rightOfContent, 40),
                WrapContents = false
            };

            Button MakeButton(string label, DialogResult result)
            {
                var btn = new Button()
                {
                    Text = label,
                    DialogResult = result,
                    Size = new Size(92, 28),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = isDark ? Color.FromArgb(60, 60, 60) : Color.White,
                    ForeColor = fg,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(8, 4, 0, 4)
                };
                btn.FlatAppearance.BorderColor = isDark ? Color.FromArgb(90, 90, 90) : Color.LightGray;
                btn.FlatAppearance.BorderSize = 1;
                return btn;
            };

            // "logicalOrder" matches standard MessageBox semantics for MessageBoxDefaultButton
            // (Button1/2/3 = first/second/third in this logical, not visual, order) - the
            // FlowLayoutPanel then lays them out right-to-left as usual.
            var logicalOrder = new List<Button>();
            Button cancelBtn = null;

            switch (buttons)
            {
                case MessageBoxButtons.OKCancel:
                    {
                        var ok = MakeButton("OK", DialogResult.OK);
                        var cancel = MakeButton("Cancel", DialogResult.Cancel);
                        logicalOrder.Add(ok);
                        logicalOrder.Add(cancel);
                        cancelBtn = cancel;
                        break;
                    }
                case MessageBoxButtons.YesNo:
                    {
                        var yes = MakeButton("Yes", DialogResult.Yes);
                        var no = MakeButton("No", DialogResult.No);
                        logicalOrder.Add(yes);
                        logicalOrder.Add(no);
                        cancelBtn = no;
                        break;
                    }
                case MessageBoxButtons.YesNoCancel:
                    {
                        var yes = MakeButton("Yes", DialogResult.Yes);
                        var no = MakeButton("No", DialogResult.No);
                        var cancel = MakeButton("Cancel", DialogResult.Cancel);
                        logicalOrder.Add(yes);
                        logicalOrder.Add(no);
                        logicalOrder.Add(cancel);
                        cancelBtn = cancel; // OK
                        break;
                    }

                default:
                    {
                        var ok = MakeButton("OK", DialogResult.OK);
                        logicalOrder.Add(ok);
                        cancelBtn = ok;
                        break;
                    }
            }

            // Add to the panel in reverse logical order so FlowDirection.RightToLeft still puts
            // the first logical button (Yes/OK) on the right, matching the native MessageBox.
            for (int i = logicalOrder.Count - 1; i >= 0; i -= 1)
                btnPanel.Controls.Add(logicalOrder[i]);

            int defaultIndex = Math.Min(logicalOrder.Count - 1, Math.Max(0, Conversions.ToInteger(defaultButton.ToString().Replace("Button", "")) - 1));
            var defaultBtn = logicalOrder[defaultIndex];
            AcceptButton = defaultBtn;
            CancelButton = cancelBtn;
            Shown += (s, e) => defaultBtn.Focus();

            Controls.Add(btnPanel);
            // ClientSize already accounts for the title bar/borders and resizes the outer window
            // accordingly - a trailing "Size = ClientSize" here was overwriting that outer Size with
            // the client-area dimensions, shrinking the real client area by the title bar's height
            // and clipping the button row (which sits at the very bottom) almost entirely off-window.
            ClientSize = new Size(Math.Max(rightOfContent, 280), bottomOfContent + 44);
        }
    }
}