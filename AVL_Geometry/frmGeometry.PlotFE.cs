using AeroPlot;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace AERO_Console
{
    // Detached-window support for the AVL FE element-forces plot (OPER "FE" -> chordwise dCp for
    // one span station). Static 2D plot -> render + PNG/SVG/PDF export + resize. Data is the "FE"
    // screen-output text the native session already produced (parsed, not re-run). The window
    // shows the strip currently selected in the docked cmbFeStrip (index 0 from the console).
    public partial class frmGeometry
    {
        private frmPlotWindow _plotFeWindow;

        /// <summary>Native-AVL console (OPER "FE" -> screen): parse the element-forces text the
        /// session printed, select the first span station, and pop the interactive FE window.</summary>
        public void ShowConsoleFEPlot(string feText)
        {
            EnsureFormInitialized();
            try
            {
                _lastFeStrips = ParseFeText(feText);

                cmbFeStrip.Items.Clear();
                if (_lastFeStrips is not null)
                {
                    foreach (var s in _lastFeStrips)
                        cmbFeStrip.Items.Add($"{s.SurfaceName}  Y={s.Yle:0.000}  (strip {s.StripIndex})");
                }
                if (_lastFeStrips is not null && _lastFeStrips.Count > 0)
                {
                    cmbFeStrip.Enabled = true;
                    cmbFeStrip.SelectedIndex = 0; // triggers RenderFEPlot via SelectedIndexChanged
                }
                else
                {
                    cmbFeStrip.Enabled = false;
                    RenderFEPlot();
                }
            }
            catch
            {
            }
            OpenFeWindow();
        }

        private List<FeStrip> ParseFeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<FeStrip>();
            string tmp = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                File.WriteAllText(tmp, text);
                return ParseFeFile(tmp);
            }
            finally
            {
                try { File.Delete(tmp); } catch { }
            }
        }

        /// <summary>Opens (or focuses) the detached, resizable FE (chordwise dCp) window.</summary>
        public void OpenFeWindow()
        {
            if (_plotFeWindow is not null && !_plotFeWindow.IsDisposed)
            {
                _plotFeWindow.Focus();
                _plotFeWindow.RequestRender();
                return;
            }

            _plotFeWindow = new frmPlotWindow(new DelegatePlotSource(
                "AVL Element Forces - Chordwise dCp",
                render: (w, h, view) => PopoutRender(() => BuildFEBitmap(w, h, view, false, out _, out _)),
                export: ExportFe,
                setTheme: SetPopoutTheme, initialDark: IsDarkTheme))
            {
                Icon = this.Icon,
            };
            _plotFeWindow.FormClosed += (_, __) => _plotFeWindow = null;
            _plotFeWindow.Show(this);
        }

        /// <summary>Export handler for the detached FE window's "Export ▾" menu.</summary>
        public void ExportFe(string format, int width, int height)
            => PlotExportHelper(format, width, height, "Element_Forces",
                (w, h, cap) => { var b = BuildFEBitmap(w, h, AeroPlot.PlotView.Identity, cap, out var svg, out var pdf); return (b, svg, pdf); });
    }
}
