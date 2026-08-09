// Moved out of frmGeometry.cs into the shared AeroPlot engine. Device-independent drawing
// surface: forwards to a System.Drawing.Graphics for on-screen raster while simultaneously
// recording an SVG document and a PDF content stream, so a plot can be exported as PNG
// (the raster), SVG, or PDF from a single set of draw calls. Used by both AVL and XFOIL.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.Text;

namespace AeroPlot
{
    public class SvgGraphics : IDisposable
    {

        public int Width;
        public int Height;
        public Graphics g;

        // When False, all SVG/PDF string building is skipped (normal interactive renders).
        // Set to True only when exporting.
        public bool CaptureVectors = false;

        // Multiplier applied to every drawn AND measured font size (1 = no change). Lets the plot
        // window offer a font-size control without the renderers touching each individual Font;
        // because DrawString and MeasureString scale together, layout that positions text from
        // MeasureString stays consistent. The caller keeps this within a sane range.
        public float FontScale = 1f;

        private bool FontScaleActive => System.Math.Abs(FontScale - 1f) > 1e-4f && FontScale > 0f;

        private StringBuilder sbSvg = null;
        private StringBuilder sbPdf = null;

        public SvgGraphics(int w, int h, Graphics realGraphics, bool captureVectors = false)
        {
            Width = w;
            Height = h;
            g = realGraphics;
            // Must be "Me.CaptureVectors" - VB is case-insensitive, so a bare
            // "CaptureVectors = captureVectors" here resolves BOTH sides to the
            // local parameter (self-assignment, a no-op) rather than the field,
            // since the parameter shadows the field for the rest of this
            // constructor. That silently left the field stuck at False forever,
            // so every method past the constructor (Clear/DrawLine/DrawString/...)
            // skipped vector emission entirely - only the constructor's own
            // initial white background rect ever made it into the SVG/PDF,
            // which is exactly the reported "blank white page" export.
            CaptureVectors = captureVectors;

            if (captureVectors)
            {
                sbSvg = new StringBuilder();
                sbPdf = new StringBuilder();
                sbSvg.AppendLine($"<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"{w}\" height=\"{h}\" viewBox=\"0 0 {w} {h}\">");
                sbSvg.AppendLine($"  <rect width=\"{w}\" height=\"{h}\" fill=\"white\" />");
                sbPdf.AppendLine("q");
                // WriteVectorPdf sizes the page's MediaBox at 72/96 of the pixel
                // dimensions (converting 96-DPI pixels to 72-DPI PDF points), but
                // every content-stream coordinate below is still emitted in raw
                // pixel units. Without also scaling this initial transform by the
                // same 72/96 factor, content drawn out to the full pixel width/
                // height extends past the smaller page and gets clipped - that's
                // the reported "cropped" export.
                double pdfScale = 72.0d / 96.0d;
                sbPdf.AppendLine($"{pdfScale.ToString(System.Globalization.CultureInfo.InvariantCulture)} 0 0 {(-pdfScale).ToString(System.Globalization.CultureInfo.InvariantCulture)} 0 {(h * pdfScale).ToString(System.Globalization.CultureInfo.InvariantCulture)} cm");
                sbPdf.AppendLine("1 w");
                sbPdf.AppendLine("0 0 0 RG");
                sbPdf.AppendLine("0 0 0 rg");
            }
        }

        public void Clear(Color c)
        {
            if (g is not null)
                g.Clear(c);
            if (!CaptureVectors)
                return;
            string cHex = ColorToHex(c);
            sbSvg.AppendLine($"  <rect width=\"{Width}\" height=\"{Height}\" fill=\"{cHex}\" />");
            sbPdf.AppendLine($"{ColorToPdfColor(c)} rg");
            sbPdf.AppendLine($"0 0 {Width} {Height} re");
            sbPdf.AppendLine("f");
        }

        public void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
        {
            if (g is not null)
                g.DrawLine(pen, x1, y1, x2, y2);
            if (!CaptureVectors)
                return;
            string cHex = ColorToHex(pen.Color);
            string dash = GetSvgDashArray(pen);
            sbSvg.AppendLine($"  <line x1=\"{x1.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" y1=\"{y1.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" x2=\"{x2.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" y2=\"{y2.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" stroke=\"{cHex}\" stroke-width=\"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" {(string.IsNullOrEmpty(dash) ? "" : $"stroke-dasharray=\"{dash}\"")} />");
            sbPdf.AppendLine("q");
            sbPdf.AppendLine($"{ColorToPdfColor(pen.Color)} RG");
            sbPdf.AppendLine($"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)} w");
            string pdfDash = GetPdfDashArray(pen);
            if (!string.IsNullOrEmpty(pdfDash))
                sbPdf.AppendLine(pdfDash);
            sbPdf.AppendLine($"{x1.ToString(System.Globalization.CultureInfo.InvariantCulture)} {y1.ToString(System.Globalization.CultureInfo.InvariantCulture)} m");
            sbPdf.AppendLine($"{x2.ToString(System.Globalization.CultureInfo.InvariantCulture)} {y2.ToString(System.Globalization.CultureInfo.InvariantCulture)} l");
            sbPdf.AppendLine("S");
            sbPdf.AppendLine("Q");
        }

        public void DrawLine(Pen pen, PointF p1, PointF p2)
        {
            DrawLine(pen, p1.X, p1.Y, p2.X, p2.Y);
        }

        public void DrawEllipse(Pen pen, RectangleF rect)
        {
            DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawEllipse(Pen pen, Rectangle rect)
        {
            DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void FillEllipse(Brush brush, RectangleF rect)
        {
            FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void FillEllipse(Brush brush, Rectangle rect)
        {
            FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawRectangle(Pen pen, Rectangle rect)
        {
            DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawRectangle(Pen pen, RectangleF rect)
        {
            DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void FillRectangle(Brush brush, RectangleF rect)
        {
            FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawLines(Pen pen, PointF[] points)
        {
            if (g is not null)
                g.DrawLines(pen, points);
            if (!CaptureVectors)
                return;
            if (points.Length < 2)
                return;

            string ptsStr = PointsToString(points);
            string cHex = ColorToHex(pen.Color);
            string dash = GetSvgDashArray(pen);
            sbSvg.AppendLine($"  <polyline points=\"{ptsStr}\" fill=\"none\" stroke=\"{cHex}\" stroke-width=\"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" {(string.IsNullOrEmpty(dash) ? "" : $"stroke-dasharray=\"{dash}\"")} />");

            sbPdf.AppendLine("q");
            sbPdf.AppendLine($"{ColorToPdfColor(pen.Color)} RG");
            sbPdf.AppendLine($"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)} w");
            string pdfDash = GetPdfDashArray(pen);
            if (!string.IsNullOrEmpty(pdfDash))
                sbPdf.AppendLine(pdfDash);

            sbPdf.AppendLine($"{points[0].X.ToString(System.Globalization.CultureInfo.InvariantCulture)} {points[0].Y.ToString(System.Globalization.CultureInfo.InvariantCulture)} m");
            for (int i = 1, loopTo = points.Length - 1; i <= loopTo; i++)
                sbPdf.AppendLine($"{points[i].X.ToString(System.Globalization.CultureInfo.InvariantCulture)} {points[i].Y.ToString(System.Globalization.CultureInfo.InvariantCulture)} l");
            sbPdf.AppendLine("S");
            sbPdf.AppendLine("Q");
        }

        public void DrawPolygon(Pen pen, PointF[] points)
        {
            if (g is not null)
                g.DrawPolygon(pen, points);
            if (!CaptureVectors)
                return;
            if (points.Length < 2)
                return;

            string ptsStr = PointsToString(points);
            string cHex = ColorToHex(pen.Color);
            string dash = GetSvgDashArray(pen);
            sbSvg.AppendLine($"  <polygon points=\"{ptsStr}\" fill=\"none\" stroke=\"{cHex}\" stroke-width=\"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" {(string.IsNullOrEmpty(dash) ? "" : $"stroke-dasharray=\"{dash}\"")} />");

            sbPdf.AppendLine("q");
            sbPdf.AppendLine($"{ColorToPdfColor(pen.Color)} RG");
            sbPdf.AppendLine($"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)} w");
            string pdfDash = GetPdfDashArray(pen);
            if (!string.IsNullOrEmpty(pdfDash))
                sbPdf.AppendLine(pdfDash);

            sbPdf.AppendLine($"{points[0].X.ToString(System.Globalization.CultureInfo.InvariantCulture)} {points[0].Y.ToString(System.Globalization.CultureInfo.InvariantCulture)} m");
            for (int i = 1, loopTo = points.Length - 1; i <= loopTo; i++)
                sbPdf.AppendLine($"{points[i].X.ToString(System.Globalization.CultureInfo.InvariantCulture)} {points[i].Y.ToString(System.Globalization.CultureInfo.InvariantCulture)} l");
            sbPdf.AppendLine("h S");
            sbPdf.AppendLine("Q");
        }

        public void FillPolygon(Brush brush, PointF[] points)
        {
            if (g is not null)
                g.FillPolygon(brush, points);
            if (!CaptureVectors)
                return;
            if (points.Length < 2)
                return;

            string ptsStr = PointsToString(points);
            var color = GetBrushColor(brush);
            string cHex = ColorToHex(color);
            double opacity = color.A / 255.0d;
            sbSvg.AppendLine($"  <polygon points=\"{ptsStr}\" fill=\"{cHex}\" opacity=\"{opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" />");

            sbPdf.AppendLine("q");
            sbPdf.AppendLine($"{ColorToPdfColor(color)} rg");
            sbPdf.AppendLine($"{points[0].X.ToString(System.Globalization.CultureInfo.InvariantCulture)} {points[0].Y.ToString(System.Globalization.CultureInfo.InvariantCulture)} m");
            for (int i = 1, loopTo = points.Length - 1; i <= loopTo; i++)
                sbPdf.AppendLine($"{points[i].X.ToString(System.Globalization.CultureInfo.InvariantCulture)} {points[i].Y.ToString(System.Globalization.CultureInfo.InvariantCulture)} l");
            sbPdf.AppendLine("f");
            sbPdf.AppendLine("Q");
        }

        public void DrawEllipse(Pen pen, float x, float y, float w, float h)
        {
            if (g is not null)
                g.DrawEllipse(pen, x, y, w, h);
            if (!CaptureVectors)
                return;
            float cx = x + w / 2f;
            float cy = y + h / 2f;
            float rx = w / 2f;
            float ry = h / 2f;
            string cHex = ColorToHex(pen.Color);
            sbSvg.AppendLine($"  <ellipse cx=\"{cx.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" cy=\"{cy.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" rx=\"{rx.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" ry=\"{ry.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" stroke=\"{cHex}\" stroke-width=\"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" fill=\"none\" />");

            sbPdf.AppendLine("q");
            sbPdf.AppendLine($"{ColorToPdfColor(pen.Color)} RG");
            sbPdf.AppendLine($"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)} w");
            AppendPdfEllipse(x, y, w, h, "S");
            sbPdf.AppendLine("Q");
        }

        public void FillEllipse(Brush brush, float x, float y, float w, float h)
        {
            if (g is not null)
                g.FillEllipse(brush, x, y, w, h);
            if (!CaptureVectors)
                return;
            float cx = x + w / 2f;
            float cy = y + h / 2f;
            float rx = w / 2f;
            float ry = h / 2f;
            var color = GetBrushColor(brush);
            string cHex = ColorToHex(color);
            double opacity = color.A / 255.0d;
            sbSvg.AppendLine($"  <ellipse cx=\"{cx.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" cy=\"{cy.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" rx=\"{rx.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" ry=\"{ry.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" fill=\"{cHex}\" opacity=\"{opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" />");

            sbPdf.AppendLine("q");
            sbPdf.AppendLine($"{ColorToPdfColor(color)} rg");
            AppendPdfEllipse(x, y, w, h, "f");
            sbPdf.AppendLine("Q");
        }

        public void DrawRectangle(Pen pen, float x, float y, float w, float h)
        {
            if (g is not null)
                g.DrawRectangle(pen, x, y, w, h);
            if (!CaptureVectors)
                return;
            string cHex = ColorToHex(pen.Color);
            sbSvg.AppendLine($"  <rect x=\"{x.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" y=\"{y.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" width=\"{w.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" height=\"{h.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" stroke=\"{cHex}\" stroke-width=\"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" fill=\"none\" />");

            sbPdf.AppendLine("q");
            sbPdf.AppendLine($"{ColorToPdfColor(pen.Color)} RG");
            sbPdf.AppendLine($"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)} w");
            sbPdf.AppendLine($"{x.ToString(System.Globalization.CultureInfo.InvariantCulture)} {y.ToString(System.Globalization.CultureInfo.InvariantCulture)} {w.ToString(System.Globalization.CultureInfo.InvariantCulture)} {h.ToString(System.Globalization.CultureInfo.InvariantCulture)} re");
            sbPdf.AppendLine("S");
            sbPdf.AppendLine("Q");
        }

        public void FillRectangle(Brush brush, float x, float y, float w, float h)
        {
            if (g is not null)
                g.FillRectangle(brush, x, y, w, h);
            if (!CaptureVectors)
                return;
            var color = GetBrushColor(brush);
            string cHex = ColorToHex(color);
            double opacity = color.A / 255.0d;
            sbSvg.AppendLine($"  <rect x=\"{x.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" y=\"{y.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" width=\"{w.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" height=\"{h.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" fill=\"{cHex}\" opacity=\"{opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" />");

            sbPdf.AppendLine("q");
            sbPdf.AppendLine($"{ColorToPdfColor(color)} rg");
            sbPdf.AppendLine($"{x.ToString(System.Globalization.CultureInfo.InvariantCulture)} {y.ToString(System.Globalization.CultureInfo.InvariantCulture)} {w.ToString(System.Globalization.CultureInfo.InvariantCulture)} {h.ToString(System.Globalization.CultureInfo.InvariantCulture)} re");
            sbPdf.AppendLine("f");
            sbPdf.AppendLine("Q");
        }

        public void FillRectangle(Brush brush, Rectangle rect)
        {
            FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawPath(Pen pen, GraphicsPath path)
        {
            if (g is not null)
                g.DrawPath(pen, path);
            if (!CaptureVectors)
                return;
            string d = PathDataToSvgD(path.PathData);
            string cHex = ColorToHex(pen.Color);
            sbSvg.AppendLine($"  <path d=\"{d}\" stroke=\"{cHex}\" stroke-width=\"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" fill=\"none\" {(string.IsNullOrEmpty(GetSvgDashArray(pen)) ? "" : $"stroke-dasharray=\"{GetSvgDashArray(pen)}\"")} />");

            sbPdf.AppendLine("q");
            sbPdf.AppendLine($"{ColorToPdfColor(pen.Color)} RG");
            sbPdf.AppendLine($"{pen.Width.ToString(System.Globalization.CultureInfo.InvariantCulture)} w");
            string pdfDash = GetPdfDashArray(pen);
            if (!string.IsNullOrEmpty(pdfDash))
                sbPdf.AppendLine(pdfDash);
            AppendPdfPath(path.PathData);
            sbPdf.AppendLine("S");
            sbPdf.AppendLine("Q");
        }

        public void FillPath(Brush brush, GraphicsPath path)
        {
            if (g is not null)
                g.FillPath(brush, path);
            if (!CaptureVectors)
                return;
            string d = PathDataToSvgD(path.PathData);
            var color = GetBrushColor(brush);
            string cHex = ColorToHex(color);
            double opacity = color.A / 255.0d;
            sbSvg.AppendLine($"  <path d=\"{d}\" fill=\"{cHex}\" opacity=\"{opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" />");

            sbPdf.AppendLine("q");
            sbPdf.AppendLine($"{ColorToPdfColor(color)} rg");
            AppendPdfPath(path.PathData);
            sbPdf.AppendLine("f");
            sbPdf.AppendLine("Q");
        }

        public void DrawString(string s, Font font, Brush brush, float x, float y)
        {
            // Apply the font-size multiplier once, here, so both the on-screen draw and the
            // SVG/PDF capture below use the same scaled size. Disposed at the end if we made one.
            Font ownFont = null;
            if (FontScaleActive)
                font = ownFont = new Font(font.FontFamily, font.Size * FontScale, font.Style);
            try
            {
                DrawStringCore(s, font, brush, x, y);
            }
            finally
            {
                ownFont?.Dispose();
            }
        }

        private void DrawStringCore(string s, Font font, Brush brush, float x, float y)
        {
            if (g is not null)
                g.DrawString(s, font, brush, x, y);
            if (!CaptureVectors)
                return;
            var color = GetBrushColor(brush);
            string cHex = ColorToHex(color);
            double opacity = color.A / 255.0d;
            string escaped = EscapeXml(s);

            bool isBold = font.Bold;
            bool isItalic = font.Italic;
            string fontName = font.Name;

            // GDI renders a font of Size S POINTS at S*(DPI/72) PIXELS on the 96-DPI bitmap, but the
            // SVG/PDF content is emitted in raw pixel units - so emitting the raw point Size makes
            // exported text ~25% smaller than what's on screen. Emit the pixel-equivalent size so the
            // export matches the on-screen rendering (and scales with FontScale, already applied to
            // `font` by the caller).
            float emitSize = font.Size * ((g?.DpiX ?? 96f) / 72f);
            string emitSizeStr = emitSize.ToString(System.Globalization.CultureInfo.InvariantCulture);

            sbSvg.AppendLine($"  <text x=\"{x.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" y=\"{y.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" font-family=\"{fontName}\" font-size=\"{emitSizeStr}px\" {(isBold ? "font-weight=\"bold\"" : "")} {(isItalic ? "font-style=\"italic\"" : "")} fill=\"{cHex}\" opacity=\"{opacity.ToString(System.Globalization.CultureInfo.InvariantCulture)}\" dominant-baseline=\"hanging\">{escaped}</text>");

            string pdfFontRef = "F1";
            if (fontName.Contains("Consolas") || fontName.Contains("Monospace") || fontName.Contains("Courier"))
            {
                pdfFontRef = isBold ? "F2" : "F1";
            }
            else
            {
                pdfFontRef = isBold ? "F4" : "F3";
            }

            // PDF text strings are written as a single-byte-per-char literal (see
            // WriteVectorPdf/Encoding.Latin1), so anything outside Latin-1 - like the
            // Greek alpha/beta/gamma this app's 3D-view angle readout uses - can't be
            // written directly. Adobe's standard "Symbol" font (always available in a
            // PDF viewer, no embedding needed) maps its own a/b/g/... codes to Greek
            // lowercase letters, so those characters are split into their own run and
            // rendered through /F5 (Symbol) instead, with everything else going
            // through the normal font unchanged. Run widths are measured with the
            // actual Unicode font (Segoe UI/Consolas both render Greek glyphs fine on
            // Windows) rather than Symbol's real metrics - close enough at this
            // overlay's small point size to keep runs visually contiguous.
            float baselineY = y + emitSize * 0.8f;
            float curX = x;
            sbPdf.AppendLine("BT");
            foreach (var run in SplitPdfTextRuns(s))
            {
                string runFontRef = run.IsSymbol ? "F5" : pdfFontRef;
                string runText = run.IsSymbol ? run.Text : EscapePdfString(run.Text);
                sbPdf.AppendLine($"/{runFontRef} {emitSizeStr} Tf");
                sbPdf.AppendLine($"{ColorToPdfColor(color)} rg");
                sbPdf.AppendLine($"1 0 0 -1 {curX.ToString(System.Globalization.CultureInfo.InvariantCulture)} {baselineY.ToString(System.Globalization.CultureInfo.InvariantCulture)} Tm");
                sbPdf.AppendLine($"({runText}) Tj");
                // Advance by the width at the EMITTED (pixel-equivalent) size, so multi-run text
                // (Greek symbol substitutions) stays contiguous now that emitSize > font.Size.
                curX += MeasureStringRaw(run.OriginalText, font).Width * ((g?.DpiX ?? 96f) / 72f);
            }
            sbPdf.AppendLine("ET");
        }

        // Adobe Symbol-font encoding: its own a-w code points render as these Greek
        // lowercase letters, independent of any WinAnsi/Latin-1 text encoding.
        private static readonly Dictionary<char, char> GreekToSymbolMap = new Dictionary<char, char>() { { (char)(0x3B1), 'a' }, { (char)(0x3B2), 'b' }, { (char)(0x3B3), 'g' }, { (char)(0x3B4), 'd' }, { (char)(0x3B5), 'e' }, { (char)(0x3B6), 'z' }, { (char)(0x3B7), 'h' }, { (char)(0x3B8), 'q' }, { (char)(0x3B9), 'i' }, { (char)(0x3BA), 'k' }, { (char)(0x3BB), 'l' }, { (char)(0x3BC), 'm' }, { (char)(0x3BD), 'n' }, { (char)(0x3BE), 'x' }, { (char)(0x3BF), 'o' }, { (char)(0x3C0), 'p' }, { (char)(0x3C1), 'r' }, { (char)(0x3C3), 's' }, { (char)(0x3C4), 't' }, { (char)(0x3C5), 'u' }, { (char)(0x3C6), 'f' }, { (char)(0x3C7), 'c' }, { (char)(0x3C8), 'y' }, { (char)(0x3C9), 'w' } };

        // Splits a string into alternating runs of "ordinary" text and single Greek
        // letters that need the Symbol-font substitution (see DrawString/GreekToSymbolMap).
        // OriginalText is kept per-run purely for width measurement (with the real font);
        // Text is what actually gets written into the PDF content stream for that run.
        private List<(string OriginalText, string Text, bool IsSymbol)> SplitPdfTextRuns(string s)
        {
            var runs = new List<(string OriginalText, string Text, bool IsSymbol)>();
            var plain = new StringBuilder();
            foreach (var c in s)
            {
                if (GreekToSymbolMap.ContainsKey(c))
                {
                    if (plain.Length > 0)
                    {
                        runs.Add((plain.ToString(), plain.ToString(), false));
                        plain.Clear();
                    }
                    runs.Add((c.ToString(), GreekToSymbolMap[c].ToString(), true));
                }
                else
                {
                    plain.Append(c);
                }
            }
            if (plain.Length > 0)
            {
                runs.Add((plain.ToString(), plain.ToString(), false));
            }
            return runs;
        }

        public void DrawString(string s, Font font, Brush brush, PointF pt)
        {
            DrawString(s, font, brush, pt.X, pt.Y);
        }

        public SizeF MeasureString(string text, Font font)
        {
            if (!FontScaleActive)
                return MeasureStringRaw(text, font);
            using var sf = new Font(font.FontFamily, font.Size * FontScale, font.Style);
            return MeasureStringRaw(text, sf);
        }

        // Measures without applying FontScale. Used internally by DrawStringCore, which has already
        // scaled its font (calling the public MeasureString there would scale a second time).
        private SizeF MeasureStringRaw(string text, Font font)
        {
            if (g is not null)
            {
                return g.MeasureString(text, font);
            }
            else
            {
                using (var tempBmp = new Bitmap(1, 1))
                {
                    using (var tempG = Graphics.FromImage(tempBmp))
                    {
                        return tempG.MeasureString(text, font);
                    }
                }
            }
        }

        public SmoothingMode SmoothingMode
        {
            get
            {
                if (g is not null)
                    return g.SmoothingMode;
                return SmoothingMode.Default;
            }
            set
            {
                if (g is not null)
                    g.SmoothingMode = value;
            }
        }

        public TextRenderingHint TextRenderingHint
        {
            get
            {
                if (g is not null)
                    return g.TextRenderingHint;
                return TextRenderingHint.SystemDefault;
            }
            set
            {
                if (g is not null)
                    g.TextRenderingHint = value;
            }
        }

        public PixelOffsetMode PixelOffsetMode
        {
            get
            {
                if (g is not null)
                    return g.PixelOffsetMode;
                return PixelOffsetMode.Default;
            }
            set
            {
                if (g is not null)
                    g.PixelOffsetMode = value;
            }
        }

        public float DpiX
        {
            get
            {
                if (g is not null)
                    return g.DpiX;
                return 96.0f;
            }
        }

        public string GetSvgContent()
        {
            if (sbSvg is null)
                return "";
            return sbSvg.ToString() + "</svg>";
        }

        public string GetPdfContentStream()
        {
            if (sbPdf is null)
                return "";
            return sbPdf.ToString() + "Q" + "\n";
        }

        private string ColorToHex(Color c)
        {
            return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }

        private string ColorToPdfColor(Color c)
        {
            double r = c.R / 255.0d;
            double g = c.G / 255.0d;
            double b = c.B / 255.0d;
            return $"{r.ToString(System.Globalization.CultureInfo.InvariantCulture)} {g.ToString(System.Globalization.CultureInfo.InvariantCulture)} {b.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
        }

        private Color GetBrushColor(Brush brush)
        {
            if (brush is SolidBrush)
            {
                return ((SolidBrush)brush).Color;
            }
            return Color.Black;
        }

        private string GetSvgDashArray(Pen pen)
        {
            if (pen.DashStyle == DashStyle.Dash)
            {
                return "4,4";
            }
            else if (pen.DashStyle == DashStyle.Dot)
            {
                return "1,3";
            }
            else if (pen.DashStyle == DashStyle.DashDot)
            {
                return "4,3,1,3";
            }
            return "";
        }

        private string GetPdfDashArray(Pen pen)
        {
            if (pen.DashStyle == DashStyle.Dash)
            {
                return "[4 4] 0 d";
            }
            else if (pen.DashStyle == DashStyle.Dot)
            {
                return "[1 3] 0 d";
            }
            else if (pen.DashStyle == DashStyle.DashDot)
            {
                return "[4 3 1 3] 0 d";
            }
            return "";
        }

        private string PointsToString(PointF[] points)
        {
            var sbPoints = new StringBuilder();
            foreach (var pt in points)
                sbPoints.Append($"{pt.X.ToString(System.Globalization.CultureInfo.InvariantCulture)},{pt.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)} ");
            return sbPoints.ToString().Trim();
        }

        private string PathDataToSvgD(PathData pathData)
        {
            var sbD = new StringBuilder();
            PointF[] pts = pathData.Points;
            byte[] types = pathData.Types;

            int i = 0;
            while (i < pts.Length)
            {
                byte @type = types[i];
                var pt = pts[i];
                string xStr = pt.X.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string yStr = pt.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);

                if ((type & 0x7) == 0)
                {
                    sbD.Append($"M {xStr} {yStr} ");
                    i += 1;
                }
                else if ((type & 0x7) == 1)
                {
                    sbD.Append($"L {xStr} {yStr} ");
                    if ((type & 0x80) == 0x80)
                        sbD.Append("Z ");
                    i += 1;
                }
                else if ((type & 0x7) == 3)
                {
                    if (i + 2 < pts.Length)
                    {
                        var cp1 = pts[i];
                        var cp2 = pts[i + 1];
                        var ep = pts[i + 2];
                        string cp1x = cp1.X.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string cp1y = cp1.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string cp2x = cp2.X.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string cp2y = cp2.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string epx = ep.X.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string epy = ep.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);

                        sbD.Append($"C {cp1x} {cp1y}, {cp2x} {cp2y}, {epx} {epy} ");
                        byte epType = types[i + 2];
                        if ((epType & 0x80) == 0x80)
                            sbD.Append("Z ");
                        i += 3;
                    }
                    else
                    {
                        i += 1;
                    }
                }
                else
                {
                    i += 1;
                }
            }
            return sbD.ToString().Trim();
        }

        private void AppendPdfPath(PathData pathData)
        {
            PointF[] pts = pathData.Points;
            byte[] types = pathData.Types;

            int i = 0;
            while (i < pts.Length)
            {
                byte @type = types[i];
                var pt = pts[i];
                string xStr = pt.X.ToString(System.Globalization.CultureInfo.InvariantCulture);
                string yStr = pt.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);

                if ((type & 0x7) == 0)
                {
                    sbPdf.AppendLine($"{xStr} {yStr} m");
                    i += 1;
                }
                else if ((type & 0x7) == 1)
                {
                    sbPdf.AppendLine($"{xStr} {yStr} l");
                    if ((type & 0x80) == 0x80)
                        sbPdf.AppendLine("h");
                    i += 1;
                }
                else if ((type & 0x7) == 3)
                {
                    if (i + 2 < pts.Length)
                    {
                        var cp1 = pts[i];
                        var cp2 = pts[i + 1];
                        var ep = pts[i + 2];
                        string cp1x = cp1.X.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string cp1y = cp1.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string cp2x = cp2.X.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string cp2y = cp2.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string epx = ep.X.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        string epy = ep.Y.ToString(System.Globalization.CultureInfo.InvariantCulture);

                        sbPdf.AppendLine($"{cp1x} {cp1y} {cp2x} {cp2y} {epx} {epy} c");
                        byte epType = types[i + 2];
                        if ((epType & 0x80) == 0x80)
                            sbPdf.AppendLine("h");
                        i += 3;
                    }
                    else
                    {
                        i += 1;
                    }
                }
                else
                {
                    i += 1;
                }
            }
        }

        private void AppendPdfEllipse(float x, float y, float w, float h, string op)
        {
            float cx = x + w / 2f;
            float cy = y + h / 2f;
            float rx = w / 2f;
            float ry = h / 2f;

            double kappa = 0.55228474983079345d;
            double ox = (double)rx * kappa;
            double oy = (double)ry * kappa;

            string cxStr = cx.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string cyStr = cy.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string rxStr = rx.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string ryStr = ry.ToString(System.Globalization.CultureInfo.InvariantCulture);

            string xM = (cx - rx).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string xP = (cx + rx).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string yM = (cy - ry).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string yP = (cy + ry).ToString(System.Globalization.CultureInfo.InvariantCulture);

            string cpXM_ox = ((double)(cx - rx) + ox).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string cpXP_ox = ((double)(cx + rx) - ox).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string cpYM_oy = ((double)(cy - ry) + oy).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string cpYP_oy = ((double)(cy + ry) - oy).ToString(System.Globalization.CultureInfo.InvariantCulture);

            string cpCX_ox = ((double)cx - ox).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string cpCX_pox = ((double)cx + ox).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string cpCY_oy = ((double)cy - oy).ToString(System.Globalization.CultureInfo.InvariantCulture);
            string cpCY_poy = ((double)cy + oy).ToString(System.Globalization.CultureInfo.InvariantCulture);

            sbPdf.AppendLine($"{xM} {cyStr} m");
            sbPdf.AppendLine($"{xM} {cpCY_oy} {cpCX_ox} {yM} {cxStr} {yM} c");
            sbPdf.AppendLine($"{cpCX_pox} {yM} {xP} {cpCY_oy} {xP} {cyStr} c");
            sbPdf.AppendLine($"{xP} {cpCY_poy} {cpCX_pox} {yP} {cxStr} {yP} c");
            sbPdf.AppendLine($"{cpCX_ox} {yP} {xM} {cpCY_poy} {xM} {cyStr} c");
            sbPdf.AppendLine($"h {op}");
        }

        private string EscapeXml(string s)
        {
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");
        }

        private string EscapePdfString(string s)
        {
            return s.Replace(@"\", @"\\").Replace("(", @"\(").Replace(")", @"\)");
        }

        public void Dispose()
        {
            if (g is not null)
            {
                g.Dispose();
            }
        }
    }
}
