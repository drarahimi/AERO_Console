using AeroPlot;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace AERO_Console
{
    // Detached-window support for the AVL spanwise-loads plot (OPER "VM" strip shear/moment).
    // Static 2D plot -> render + PNG/SVG/PDF export + resize, no rotate/zoom/fit. Data is the
    // "VM" screen-output text the native session already produced (parsed, not re-run).
    public partial class frmGeometry
    {
        private frmPlotWindow _plotLoadsWindow;

        /// <summary>Native-AVL console (OPER "VM" -> screen): parse the strip shear/moment text the
        /// session printed and pop the interactive spanwise-loads window.</summary>
        public void ShowConsoleLoadsPlot(string vmText)
        {
            EnsureFormInitialized();
            try
            {
                _lastVmSurfaces = ParseVmText(vmText);
                RenderLoadsPlot(); // refresh the docked pLoads tab too (self-guards if not built)
            }
            catch
            {
            }
            OpenLoadsWindow();
        }

        // ParseVmFile reads from disk; the console path has the text in-hand, so stage it to a
        // temp file and reuse the exact same parser.
        private System.Collections.Generic.List<VmSurface> ParseVmText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new System.Collections.Generic.List<VmSurface>();
            string tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(tmp, text);
                return ParseVmFile(tmp);
            }
            finally
            {
                try { File.Delete(tmp); } catch { }
            }
        }

        /// <summary>Opens (or focuses) the detached, resizable spanwise-loads window.</summary>
        public void OpenLoadsWindow()
        {
            if (_plotLoadsWindow is not null && !_plotLoadsWindow.IsDisposed)
            {
                _plotLoadsWindow.Focus();
                _plotLoadsWindow.RequestRender();
                return;
            }

            _plotLoadsWindow = new frmPlotWindow(new DelegatePlotSource(
                "AVL Spanwise Loads - Shear & Bending Moment",
                render: (w, h, view) => PopoutRender(() => BuildLoadsBitmap(w, h, view, false, out _, out _)),
                export: ExportLoads,
                setTheme: SetPopoutTheme, initialDark: IsDarkTheme, setFontScale: SetPopoutFontScale))
            {
                Icon = this.Icon,
            };
            _plotLoadsWindow.FormClosed += (_, __) => _plotLoadsWindow = null;
            _plotLoadsWindow.Show(this);
        }

        /// <summary>Export handler for the detached spanwise-loads window's "Export ▾" menu.</summary>
        public void ExportLoads(string format, int width, int height)
            => PlotExportHelper(format, width, height, "Spanwise_Loads",
                (w, h, cap) => { var b = BuildLoadsBitmap(w, h, AeroPlot.PlotView.Identity, cap, out var svg, out var pdf); return (b, svg, pdf); });

        /// <summary>Shared PNG/SVG/PDF save for the detached plot windows: renders with vector
        /// capture, prompts for a filename (defaulting to {project}_{baseName}), and writes PNG
        /// (raster), SVG (text), or PDF (WriteVectorPdf). Used by every AVL plot's Export handler.</summary>
        internal void PlotExportHelper(string format, int width, int height, string baseName,
            Func<int, int, bool, (Bitmap bmp, string svg, string pdf)> render)
        {
            Bitmap bmp = null;
            _popoutRenderActive = true; // render the export at the pop-out's chosen font size
            try
            {
                var (b, svg, pdf) = render(width, height, true);
                bmp = b;

                if (format != "PNG" && string.IsNullOrEmpty(svg))
                {
                    AppMessageBox.Show("The view has not finished rendering. Please try again in a moment.",
                        "Export View", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using var sfd = new SaveFileDialog { Title = $"Export {baseName.Replace('_', ' ')} as {format}" };
                string projName = (projectName ?? "").Trim();
                if (string.IsNullOrEmpty(projName) || projName.Contains("Enter AVL Project") || projName.Contains("Enter NACA"))
                    sfd.FileName = baseName;
                else
                {
                    foreach (var c in Path.GetInvalidFileNameChars())
                        projName = projName.Replace(c, '_');
                    sfd.FileName = $"{projName}_{baseName}";
                }
                switch (format)
                {
                    case "PNG": sfd.Filter = "PNG Image (*.png)|*.png"; sfd.DefaultExt = "png"; break;
                    case "SVG": sfd.Filter = "SVG Image (*.svg)|*.svg"; sfd.DefaultExt = "svg"; break;
                    case "PDF": sfd.Filter = "PDF Document (*.pdf)|*.pdf"; sfd.DefaultExt = "pdf"; break;
                }

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    switch (format)
                    {
                        case "PNG":
                            using (var copy = new Bitmap(bmp))
                                copy.Save(sfd.FileName, ImageFormat.Png);
                            break;
                        case "SVG":
                            File.WriteAllText(sfd.FileName, svg, Encoding.UTF8);
                            break;
                        case "PDF":
                            WriteVectorPdf(pdf, width, height, sfd.FileName);
                            break;
                    }
                    AppToast.ShowExported($"{format} exported to " + Path.GetFileName(sfd.FileName), sfd.FileName);
                }
            }
            catch (Exception ex)
            {
                AppMessageBox.Show("Error exporting file: " + ex.Message, "Export Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _popoutRenderActive = false;
                bmp?.Dispose();
            }
        }
    }
}
