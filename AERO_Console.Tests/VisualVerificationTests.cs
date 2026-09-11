using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Xunit;

namespace AERO_Console.Tests
{
    public class VisualVerificationTests
    {
        private static readonly string ScreenshotDir = @"C:\Users\afshi\.gemini\antigravity-ide\brain\26382cd6-231f-46d0-a5e4-ba837e61d8fe\screenshots";

        [Fact]
        public void CaptureAllFormsScreenshots()
        {
            Directory.CreateDirectory(ScreenshotDir);

            var staThread = new Thread(() =>
            {
                CaptureForm("frmAbout", isDark => new frmAbout(), 540, 360);
                CaptureForm("frmHelp", isDark => new frmHelp(), 740, 600);
                CaptureForm("frmInfo", isDark => new frmInfo(), 540, 310);
                CaptureForm("frmUpdate", isDark => new frmUpdate(), 500, 195);
                CaptureForm("frmXfoilAnalysis", isDark => new frmXfoilAnalysis(), 1100, 750);
                CaptureForm("frmGeometry", isDark => new frmGeometry(), 1200, 800);
                CaptureForm("frmGeometry_Trefftz", isDark => new frmGeometry(), 1200, 800, f =>
                {
                    var tc = f.Controls.Find("tc1", true);
                    if (tc.Length > 0 && tc[0] is TabControl tabControl && tabControl.TabCount > 3)
                    {
                        tabControl.SelectedIndex = 3;
                    }
                });
                CaptureForm("frmGeometry_Polar", isDark => new frmGeometry(), 1200, 800, f =>
                {
                    var tc = f.Controls.Find("tc1", true);
                    if (tc.Length > 0 && tc[0] is TabControl tabControl && tabControl.TabCount > 5)
                    {
                        tabControl.SelectedIndex = 5;
                    }
                });
                // frmMain already captured and has background console thread that blocks in unit test
            });

            staThread.SetApartmentState(ApartmentState.STA);
            staThread.Start();
            staThread.Join();
        }

        private static void CaptureForm(string name, Func<bool, Form> createForm, int width, int height, Action<Form> setup = null)
        {
            foreach (bool dark in new[] { true, false })
            {
                UI.Theme.Apply(dark);
                Form form = null;
                try
                {
                    form = createForm(dark);
                    form.Size = new Size(width, height);
                    form.StartPosition = FormStartPosition.Manual;
                    form.Location = new Point(0, 0);
                    form.Show();
                    setup?.Invoke(form);
                    for (int i = 0; i < 5; i++)
                    {
                        Application.DoEvents();
                        Thread.Sleep(30);
                    }

                    using var bmp = new Bitmap(width, height);
                    form.DrawToBitmap(bmp, new Rectangle(0, 0, width, height));

                    using (var g = Graphics.FromImage(bmp))
                    {
                        foreach (Control c in form.Controls)
                        {
                            if (c is ToolStrip ts && ts.Visible && ts.Width > 0 && ts.Height > 0)
                            {
                                using var tsbmp = new Bitmap(ts.Width, ts.Height);
                                ts.DrawToBitmap(tsbmp, new Rectangle(0, 0, ts.Width, ts.Height));
                                g.DrawImage(tsbmp, ts.Location);
                            }
                        }
                        var tc = form.Controls.Find("tc1", true);
                        if (tc.Length > 0 && tc[0] is TabControl tctrl && tctrl.Visible && tctrl.Width > 0 && tctrl.Height > 0)
                        {
                            Point pt = form.PointToClient(tctrl.PointToScreen(Point.Empty));
                            using var tcbmp = new Bitmap(tctrl.Width, tctrl.Height);
                            tctrl.DrawToBitmap(tcbmp, new Rectangle(0, 0, tctrl.Width, tctrl.Height));
                            g.DrawImage(tcbmp, pt);
                        }
                    }

                    string themeName = dark ? "dark" : "light";
                    string filePath = Path.Combine(ScreenshotDir, $"{name}_{themeName}.png");
                    bmp.Save(filePath, ImageFormat.Png);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error capturing {name} (dark={dark}): {ex}");
                }
                finally
                {
                    try
                    {
                        if (form != null)
                        {
                            form.Close();
                            form.Dispose();
                        }
                    }
                    catch { }
                    Application.DoEvents();
                }
            }
        }
    }
}
