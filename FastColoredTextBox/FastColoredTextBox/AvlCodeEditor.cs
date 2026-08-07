using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace FastColoredTextBoxNS
{
    /// <summary>Which AVL text format the editor currently holds. Drives context-aware autocomplete.</summary>
    public enum AvlFileKind
    {
        Geometry,
        Mass,
        Run
    }

    public enum AvlDiagnosticSeverity
    {
        Error,
        Warning,
        Info
    }

    /// <summary>Draws a rounded-rectangle badge around a range - used to mark AVL !begin.../!end... markers.</summary>
    public class EllipseStyle : Style
    {
        private readonly Color _lineColor;
        private readonly int _lineWidth;

        public EllipseStyle() : this(Color.Red, 1) { }
        public EllipseStyle(Color color) : this(color, 1) { }
        public EllipseStyle(int width) : this(Color.Red, width) { }

        public EllipseStyle(Color color, int width)
        {
            _lineColor = color;
            _lineWidth = width;
        }

        public override void Draw(Graphics gr, Point position, Range range)
        {
            var size = GetSizeOfRange(range);
            var rect = new Rectangle(position, size);
            rect.Inflate(1, 0);
            using (var pen = new Pen(_lineColor, _lineWidth))
                gr.DrawPath(pen, GetRoundedRectangle(rect, 7));
        }
    }

    /// <summary>A single inline finding to render under a line. <see cref="Line"/> is 1-based; values &lt;= 0
    /// apply to the whole file and are not drawn inline.</summary>
    public sealed class AvlDiagnostic
    {
        public int Line { get; }
        public AvlDiagnosticSeverity Severity { get; }
        public string Message { get; }

        public AvlDiagnostic(int line, AvlDiagnosticSeverity severity, string message)
        {
            Line = line;
            Severity = severity;
            Message = message;
        }
    }

    /// <summary>
    /// A <see cref="FastColoredTextBox"/> specialized for Mark Drela's AVL/XFOIL input files.
    /// Bundles the behaviour that used to live in frmGeometry: DPI-aware modern rendering,
    /// AVL-aware syntax highlighting (via .NET source-generated regexes), block folding,
    /// keyword + snippet autocomplete, and inline squiggly diagnostics fed by an external validator.
    /// </summary>
    public partial class AvlCodeEditor : FastColoredTextBox
    {
        private float _dpiScale = 1.0f;

        // --- syntax styles ---
        private readonly Style _keywordStyle = new TextStyle(Brushes.Blue, null, FontStyle.Regular);
        private readonly Style _blockStyle = new TextStyle(Brushes.Green, null, FontStyle.Bold);
        private readonly Style _commentStyle = new TextStyle(Brushes.Green, null, FontStyle.Regular);
        private readonly Style _bracketStyle = new TextStyle(Brushes.Red, null, FontStyle.Italic);
        private readonly Style _surfaceMarker = new EllipseStyle(Color.Red);
        private readonly Style _sectionMarker = new EllipseStyle(Color.Blue);
        private readonly Style _controlMarker = new EllipseStyle(Color.Cyan);
        private readonly Style _geometryMarker = new EllipseStyle(Color.Magenta);

        // --- diagnostic (squiggle) styles ---
        private readonly Style _errorWavy = new WavyLineStyle(255, Color.FromArgb(220, 53, 69));
        private readonly Style _warnWavy = new WavyLineStyle(255, Color.FromArgb(210, 150, 0));
        private readonly Style _infoWavy = new WavyLineStyle(255, Color.FromArgb(13, 110, 253));

        // Most-severe diagnostic per 1-based line, for inline rendering + hover lookup.
        private readonly Dictionary<int, AvlDiagnostic> _diagnosticsByLine = new Dictionary<int, AvlDiagnostic>();

        private AutocompleteMenu _autocomplete;
        private AvlFileKind _fileKind = AvlFileKind.Geometry;

        public AvlCodeEditor()
        {
            using (var g = CreateGraphics())
                _dpiScale = g.DpiX / 96.0f;

            Font = new Font("Consolas", 10f * _dpiScale);
            ForeColor = Color.Black;
            BackColor = Color.White;
            ShowLineNumbers = true;
            DoubleBuffered = true;
        }

        /// <summary>The AVL format currently loaded. Setting it re-scopes autocomplete to the relevant keywords.</summary>
        public AvlFileKind FileKind
        {
            get => _fileKind;
            set
            {
                if (_fileKind == value)
                    return;
                _fileKind = value;
                if (_autocomplete != null)
                    RebuildAutocompleteItems();
            }
        }

        // ClearType / anti-aliased text, carried over from the old ModernFastColoredTextBox.
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            base.OnPaint(e);
        }

        // ---------------------------------------------------------------------
        //  Syntax highlighting
        // ---------------------------------------------------------------------

        /// <summary>Re-applies AVL syntax highlighting and folding to the whole document, preserving any
        /// diagnostics already set. Safe to call on every delayed text change.</summary>
        public void ApplyAvlHighlighting()
        {
            var r = Range;
            r.ClearStyle(StyleIndex.All);
            r.ClearFoldingMarkers();

            // Keyword families (all share one blue style). The leading (?<![!#].*) lookbehind keeps
            // keywords that appear inside a ! or # comment from being coloured.
            r.SetStyle(_keywordStyle, GeometryKeywordRegex());
            r.SetStyle(_keywordStyle, MassKeywordRegex());
            r.SetStyle(_keywordStyle, RunKeywordRegex());
            r.SetStyle(_keywordStyle, RunDotKeywordRegex());

            // Block headers, comments, bracketed tokens.
            r.SetStyle(_blockStyle, BlockWordRegex());
            r.SetStyle(_commentStyle, HashCommentRegex());
            r.SetStyle(_commentStyle, BangCommentRegex());
            r.SetStyle(_bracketStyle, BracketRegex());

            // Begin/End block markers get coloured ellipse badges.
            r.SetStyle(_surfaceMarker, SurfaceMarkerRegex());
            r.SetStyle(_sectionMarker, SectionMarkerRegex());
            r.SetStyle(_controlMarker, ControlMarkerRegex());
            r.SetStyle(_geometryMarker, GeometryMarkerRegex());

            // Folding: braces plus the !begin.../!end... block pairs.
            r.SetFoldingMarkers("{", "}");
            r.SetFoldingMarkers(@"!beginsurface\b", @"!endsurface\b", RegexOptions.IgnoreCase);
            r.SetFoldingMarkers(@"!beginsection\b", @"!endsection\b", RegexOptions.IgnoreCase);
            r.SetFoldingMarkers(@"!begincontrol\b", @"!endcontrol\b", RegexOptions.IgnoreCase);
            r.SetFoldingMarkers(@"!begingeometry\b", @"!endgeometry\b", RegexOptions.IgnoreCase);

            // ClearStyle(All) above also wiped the squiggles - put them back.
            ReapplyDiagnosticStyles();

            AdjustFolding();
        }

        // Source-generated regexes: compiled once by the .NET regex source generator (net10), so the
        // per-keystroke highlight no longer rebuilds regex strings or pays interpreter start-up cost.
        [GeneratedRegex(@"(?<![!#].*)\b(Mach|IYsym|IZsym|Zsym|Sref|Cref|Bref|Xref|Yref|Zref|Nchordwise|Cspace|Nspanwise|Sspace|Xle|Yle|Zle|Chord|Ainc|ANGLE|YDUPLICATE|SCALE|TRANSLATE|Cname|Cgain|Xhinge|HingeVec|SgnDup)\b", RegexOptions.IgnoreCase)]
        private static partial Regex GeometryKeywordRegex();

        [GeneratedRegex(@"(?<![!#].*)\b(mass|Lunit|Munit|Tunit|g|rho|x|y|z|X_cg|Y_cg|Z_cg|Ixx|Iyy|Izz|Ixy|Iyz|Izx)\b", RegexOptions.IgnoreCase)]
        private static partial Regex MassKeywordRegex();

        [GeneratedRegex(@"(?<![!#].*)\b(alpha|beta|pb/2V|qc/2V|rb/2V|aileron|flap|elevator|rudder|CL|CDo|visc|bank|elevation|heading|velocity|density|CL_a|CL_u|CM_a|CM_u|Cl|roll|mom|Cm|pitch|Cn|yaw|deg|m/s|m/s\^2|kg/m\^3|kg-m\^2|kg|m)\b", RegexOptions.IgnoreCase)]
        private static partial Regex RunKeywordRegex();

        [GeneratedRegex(@"(?<![!#].*)\b(grav\.acc\.|turn_rad\.|load_fac\.)(?=\s|$)", RegexOptions.IgnoreCase)]
        private static partial Regex RunDotKeywordRegex();

        [GeneratedRegex(@"\b(surface|section|control)\b", RegexOptions.IgnoreCase)]
        private static partial Regex BlockWordRegex();

        [GeneratedRegex(@"#.*", RegexOptions.IgnoreCase)]
        private static partial Regex HashCommentRegex();

        [GeneratedRegex(@"!.*$", RegexOptions.Multiline)]
        private static partial Regex BangCommentRegex();

        [GeneratedRegex(@"\[[^\]]*\]")]
        private static partial Regex BracketRegex();

        [GeneratedRegex(@"!beginsurface|!endsurface", RegexOptions.IgnoreCase)]
        private static partial Regex SurfaceMarkerRegex();

        [GeneratedRegex(@"!beginsection|!endsection", RegexOptions.IgnoreCase)]
        private static partial Regex SectionMarkerRegex();

        [GeneratedRegex(@"!begincontrol|!endcontrol", RegexOptions.IgnoreCase)]
        private static partial Regex ControlMarkerRegex();

        [GeneratedRegex(@"!begingeometry|!endgeometry", RegexOptions.IgnoreCase)]
        private static partial Regex GeometryMarkerRegex();

        // ---------------------------------------------------------------------
        //  Inline diagnostics (squiggly underlines + hover lookup)
        // ---------------------------------------------------------------------

        /// <summary>Replaces the current set of inline diagnostics. Each line-anchored finding is drawn as a
        /// coloured squiggle under the line's text; whole-file findings (Line &lt;= 0) are ignored here.</summary>
        public void SetDiagnostics(IEnumerable<AvlDiagnostic> diagnostics)
        {
            _diagnosticsByLine.Clear();
            if (diagnostics != null)
            {
                foreach (var d in diagnostics)
                {
                    if (d == null || d.Line < 1)
                        continue;
                    // Keep only the most severe finding per line (Error < Warning < Info by enum order).
                    if (!_diagnosticsByLine.TryGetValue(d.Line, out var existing) || d.Severity < existing.Severity)
                        _diagnosticsByLine[d.Line] = d;
                }
            }
            ApplyDiagnosticStyles();
        }

        /// <summary>Removes all inline diagnostics.</summary>
        public void ClearDiagnostics()
        {
            _diagnosticsByLine.Clear();
            Range.ClearStyle(_errorWavy, _warnWavy, _infoWavy);
            Invalidate();
        }

        /// <summary>Returns the diagnostic anchored to the line under <paramref name="place"/>, or null.
        /// Lets the host's ToolTipNeeded handler surface the finding on hover.</summary>
        public AvlDiagnostic GetDiagnosticAt(Place place)
        {
            return _diagnosticsByLine.TryGetValue(place.iLine + 1, out var d) ? d : null;
        }

        private void ApplyDiagnosticStyles()
        {
            Range.ClearStyle(_errorWavy, _warnWavy, _infoWavy);
            ReapplyDiagnosticStyles();
            Invalidate();
        }

        // Draws the stored squiggles without clearing first - used after a full re-highlight, which has
        // already cleared every style.
        private void ReapplyDiagnosticStyles()
        {
            int lineCount = LinesCount;
            foreach (var kv in _diagnosticsByLine)
            {
                int li = kv.Key - 1;
                if (li < 0 || li >= lineCount)
                    continue;

                string text = GetLineText(li);
                int start = 0;
                while (start < text.Length && char.IsWhiteSpace(text[start]))
                    start++;
                int end = text.Length;
                while (end > start && char.IsWhiteSpace(text[end - 1]))
                    end--;
                if (end <= start)
                    continue; // blank line - nothing visible to underline

                GetRange(new Place(start, li), new Place(end, li)).SetStyle(StyleForSeverity(kv.Value.Severity));
            }
        }

        private Style StyleForSeverity(AvlDiagnosticSeverity severity)
        {
            switch (severity)
            {
                case AvlDiagnosticSeverity.Error: return _errorWavy;
                case AvlDiagnosticSeverity.Warning: return _warnWavy;
                default: return _infoWavy;
            }
        }

        // ---------------------------------------------------------------------
        //  Autocomplete
        // ---------------------------------------------------------------------

        /// <summary>Turns on the AVL keyword + snippet autocomplete popup (idempotent).</summary>
        public void EnableAutocomplete()
        {
            if (_autocomplete != null)
                return;

            _autocomplete = new AutocompleteMenu(this)
            {
                MinFragmentLength = 1,
                AllowTabKey = true,
                AppearInterval = 100,
            };
            RebuildAutocompleteItems();
        }

        private void RebuildAutocompleteItems()
        {
            var items = new List<AutocompleteItem>();

            switch (_fileKind)
            {
                case AvlFileKind.Mass:
                    AddKeywords(items, MassKeywords);
                    break;
                case AvlFileKind.Run:
                    AddKeywords(items, RunKeywords);
                    break;
                default: // Geometry
                    AddKeywords(items, GeometryKeywords);
                    foreach (var snip in GeometrySnippets)
                        items.Add(new SnippetAutocompleteItem(snip) { ImageIndex = -1 });
                    break;
            }

            _autocomplete.Items.SetAutocompleteItems(items);
        }

        private static void AddKeywords(List<AutocompleteItem> items, string[] keywords)
        {
            foreach (var kw in keywords)
                items.Add(new AutocompleteItem(kw));
        }

        private static readonly string[] GeometryKeywords =
        {
            "SURFACE", "SECTION", "CONTROL", "COMPONENT", "INDEX", "YDUPLICATE", "SCALE", "TRANSLATE",
            "ANGLE", "NOWAKE", "NOALBE", "NOLOAD", "AIRFOIL", "AFILE", "CLAF", "CDCL",
            "Mach", "IYsym", "IZsym", "Zsym", "Sref", "Cref", "Bref", "Xref", "Yref", "Zref",
            "Nchordwise", "Cspace", "Nspanwise", "Sspace",
            "Xle", "Yle", "Zle", "Chord", "Ainc",
            "Cname", "Cgain", "Xhinge", "HingeVec", "SgnDup",
        };

        private static readonly string[] MassKeywords =
        {
            "Lunit", "Munit", "Tunit", "g", "rho", "mass",
            "x", "y", "z", "X_cg", "Y_cg", "Z_cg",
            "Ixx", "Iyy", "Izz", "Ixy", "Iyz", "Izx",
        };

        private static readonly string[] RunKeywords =
        {
            "alpha", "beta", "pb/2V", "qc/2V", "rb/2V",
            "aileron", "flap", "elevator", "rudder",
            "CL", "CDo", "bank", "elevation", "heading", "Mach", "velocity", "density",
            "grav.acc.", "turn_rad.", "load_fac.",
            "X_cg", "Y_cg", "Z_cg", "mass",
        };

        // '^' marks where the caret lands after the snippet is inserted (see SnippetAutocompleteItem).
        private static readonly string[] GeometrySnippets =
        {
            "SURFACE\n^Wing\n!Nchordwise  Cspace   Nspanwise  Sspace\n 8           1.0      16         1.0\n",
            "SECTION\n!Xle     Yle     Zle     Chord   Ainc\n ^0.0     0.0     0.0     1.0     0.0\n",
            "CONTROL\n!Cname   Cgain  Xhinge  HingeVec      SgnDup\n ^flap    1.0    0.7     0. 1. 0.      +1\n",
            "YDUPLICATE\n^0.0\n",
            "AIRFOIL\n^",
        };
    }
}
