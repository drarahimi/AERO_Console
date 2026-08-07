using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;

namespace AERO_Console
{

    public enum IssueSeverity
    {
        Error,
        Warning,
        Info
    }

    /// <summary>
/// One finding from FileValidator: what's wrong, where, and how to fix it.
/// LineNumber is 1-based; 0 means the issue applies to the file as a whole (no single line).
/// </summary>
    public class ValidationIssue
    {
        public IssueSeverity Severity { get; set; }
        public int LineNumber { get; set; }
        public string Message { get; set; }
        public string FixHint { get; set; }

        public ValidationIssue(IssueSeverity severity, int lineNumber, string message, string fixHint)
        {
            Severity = severity;
            LineNumber = lineNumber;
            Message = message;
            FixHint = fixHint;
        }
    }

    /// <summary>
/// Line-based structural checker for AVL's .avl/.mass/.run text formats, cross-checked directly
/// against AVL's own bundled documentation (appdata/avl_doc.txt) rather than assumption - e.g. the
/// "at least 2 SECTIONs per surface", "YDUPLICATE requires IYsym=0", "spacing parameters must be in
/// -3..3", and mass file "*"/"+" multiplier-row syntax rules below are all taken directly from it.
/// This is deliberately a second, independent read of the grammar from the app's existing parsers
/// (findPoints/ScanGeometryBlocks in frmGeometry.vb) - those are optimized to degrade silently on
/// malformed input (skip a bad token, keep going) so the 3D view never crashes on a half-edited file.
/// A validator needs the opposite instinct: notice the malformed token and explain it.
/// Not a full AVL grammar/parser - covers the structural mistakes users actually make.
/// </summary>
    public static class FileValidator
    {

        private struct ContentLine
        {
            public string Text;
            public int LineNo;
        }

        private readonly static HashSet<string> KnownAvlKeywords = new HashSet<string>() { "SURFACE", "BODY", "SECTION", "CONTROL", "NACA", "AFILE", "AFIL", "AIRFOIL", "CLAF", "CDCL", "DESIGN", "YDUPLICATE", "SCALE", "TRANSLATE", "ANGLE", "NOWAKE", "NOALBE", "NOLOAD", "COMPONENT", "INDEX", "BFILE" };

        public static List<ValidationIssue> ValidateFile(string text, string fileKind, string projectDir)
        {
            switch (fileKind ?? "")
            {
                case "Geometry":
                    {
                        return ValidateAvl(text, projectDir);
                    }
                case "Mass":
                    {
                        return ValidateMass(text);
                    }
                case "Run":
                    {
                        return ValidateRun(text);
                    }

                default:
                    {
                        return new List<ValidationIssue>();
                    }
            }
        }

        #region Shared helpers

        private static string[] Tokens(string s)
        {
            return s.Split(new char[] { ' ', ControlChars.Tab }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool IsNum(string s)
        {
            double d;
            return double.TryParse(s, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out d);
        }

        private static double ParseNum(string s)
        {
            return double.Parse(s, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        }

        private static string StripInlineComment(string line)
        {
            int idx = line.IndexOf('!');
            if (idx >= 0)
                return line.Substring(0, idx);
            return line;
        }

        private static string StripQuotes(string s)
        {
            string t = s.Trim();
            if (t.Length >= 2 && t.StartsWith("\"") && t.EndsWith("\""))
                return t.Substring(1, t.Length - 2);
            return t;
        }

        /// <summary>Blank lines and full-line "#"/"!" comments removed; inline "! ..." trailing comments stripped.</summary>
        private static List<ContentLine> ExtractContentLines(string text)
        {
            var result = new List<ContentLine>();
            string[] rawLines = text.Replace(Constants.vbCr, "").Split(Constants.vbLf);
            for (int li = 0, loopTo = rawLines.Length - 1; li <= loopTo; li++)
            {
                string s = StripInlineComment(rawLines[li]).Trim();
                if (s.Length == 0)
                    continue;
                if (s.StartsWith("#") || s.StartsWith("!"))
                    continue;
                result.Add(new ContentLine() { Text = s, LineNo = li + 1 });
            }
            return result;
        }

        /// <summary>
    /// AVL matches keywords by only their first 4 characters (e.g. "SURF" for SURFACE, "COMP" for
    /// COMPONENT) - this maps any such abbreviation back to its canonical full keyword so the rest
    /// of the parser's plain string comparisons ("SURFACE", "SECTION", ...) keep working unchanged.
    /// </summary>
        private static string KeyOf(string s)
        {
            string[] toks = Tokens(s);
            if (toks.Length == 0)
                return "";
            string token = toks[0].ToUpperInvariant();
            if (token.Length < 4)
                return token;
            string prefix4 = token.Substring(0, 4);
            foreach (var kw in KnownAvlKeywords)
            {
                if ((kw.Substring(0, 4) ?? "") == (prefix4 ?? ""))
                    return kw;
            }
            return token;
        }

        #endregion

        #region .avl validation

        private static bool IsKnownAvlKeyword(string key)
        {
            return KnownAvlKeywords.Contains(key);
        }

        private static void CheckNumericLine(List<ContentLine> content, int idx, int expectedCount, string label, List<ValidationIssue> issues)
        {
            if (idx >= content.Count)
            {
                int lastLine = content.Count > 0 ? content[content.Count - 1].LineNo : 0;
                issues.Add(new ValidationIssue(IssueSeverity.Error, lastLine, $"File ends before the required '{label}' line.", $"Add a line with {expectedCount} numeric value(s): {label}."));
                return;
            }
            string[] toks = Tokens(content[idx].Text);
            int numericCount = toks.Take(expectedCount).Count(t => IsNum(t));
            if (toks.Length < expectedCount || numericCount < expectedCount)
            {
                issues.Add(new ValidationIssue(IssueSeverity.Error, content[idx].LineNo, $"Expected {expectedCount} numeric value(s) for '{label}', found: '{content[idx].Text.Trim()}'.", $"This line should contain {expectedCount} number(s) - {label} - separated by spaces."));
            }
        }

        private static void CheckPositiveLine(List<ContentLine> content, int idx, int expectedCount, string label, List<ValidationIssue> issues)
        {
            CheckNumericLine(content, idx, expectedCount, label, issues);
            if (idx < content.Count)
            {
                foreach (var t in Tokens(content[idx].Text).Take(expectedCount))
                {
                    if (IsNum(t) && ParseNum(t) <= 0d)
                    {
                        issues.Add(new ValidationIssue(IssueSeverity.Warning, content[idx].LineNo, $"'{label}' contains a zero or negative value ({t}).", "Reference area/chord/span (Sref, Cref, Bref) are normally positive."));
                        break;
                    }
                }
            }
        }

        /// <summary>Bare-integer token check by literal text (e.g. "1"), not by numeric value - "1.00" is
    /// numerically whole but is still written as a decimal, which is what actually matters here.</summary>
        private static bool IsIntegerToken(string s)
        {
            return Regex.IsMatch(s, @"^[+-]?\d+$");
        }

        /// <summary>
    /// IYsym and IZsym are read as whole numbers restricted to -1/0/1, unlike Zsym on the same line
    /// which is a real-valued Z location - AVL's doc: "iYsym = 1 ... = -1 ... = 0" (avl_doc.txt:260-266).
    /// Returns the parsed IYsym value if it's a clean integer, for the YDUPLICATE cross-check below.
    /// </summary>
        private static int? CheckIYsymIZsymAreIntegers(List<ContentLine> content, int idx, List<ValidationIssue> issues)
        {
            if (idx >= content.Count)
                return default;
            string[] toks = Tokens(content[idx].Text);
            int? iYsymValue = default;

            if (toks.Length >= 1 && IsNum(toks[0]))
            {
                if (!IsIntegerToken(toks[0]))
                {
                    issues.Add(new ValidationIssue(IssueSeverity.Error, content[idx].LineNo, $"IYsym ('{toks[0]}') is written as a decimal; AVL expects it as a whole number.", "Write IYsym with no decimal point: 0 (no symmetry), 1 (mirror about Y=0), or -1 (mirror, flow-reversed). E.g. '0' instead of '0.00'."));
                }
                else
                {
                    int v = (int)Math.Round(ParseNum(toks[0]));
                    iYsymValue = v;
                    if (v < -1 || v > 1)
                    {
                        issues.Add(new ValidationIssue(IssueSeverity.Warning, content[idx].LineNo, $"IYsym ({v}) is outside the documented range.", "IYsym must be -1, 0, or 1: 0 = no symmetry, 1 = mirror about Y=0, -1 = mirror with flow reversed."));
                    }
                }
            }

            if (toks.Length >= 2 && IsNum(toks[1]))
            {
                if (!IsIntegerToken(toks[1]))
                {
                    issues.Add(new ValidationIssue(IssueSeverity.Error, content[idx].LineNo, $"IZsym ('{toks[1]}') is written as a decimal; AVL expects it as a whole number.", "Write IZsym with no decimal point: 0 (no symmetry), 1 (mirror about Z=Zsym), or -1 (mirror, flow-reversed). E.g. '0' instead of '1.00'."));
                }
                else
                {
                    int v = (int)Math.Round(ParseNum(toks[1]));
                    if (v < -1 || v > 1)
                    {
                        issues.Add(new ValidationIssue(IssueSeverity.Warning, content[idx].LineNo, $"IZsym ({v}) is outside the documented range.", "IZsym must be -1, 0, or 1: 0 = no symmetry, 1 = mirror about Z=Zsym, -1 = mirror with flow reversed."));
                    }
                }
            }

            return iYsymValue;
        }

        /// <summary>Spacing parameters (Cspace, Sspace, Bspace) must fall in -3.0..+3.0 (avl_doc.txt:1004).</summary>
        private static void CheckSpacingRange(List<ContentLine> content, int idx, int tokenIndex, string label, List<ValidationIssue> issues)
        {
            if (idx >= content.Count)
                return;
            string[] toks = Tokens(content[idx].Text);
            if (toks.Length <= tokenIndex || !IsNum(toks[tokenIndex]))
                return;
            double v = ParseNum(toks[tokenIndex]);
            if (v < -3.0d || v > 3.0d)
            {
                issues.Add(new ValidationIssue(IssueSeverity.Error, content[idx].LineNo, $"{label} ({toks[tokenIndex]}) is outside the valid range.", $"{label} is a vortex-spacing parameter and must fall between -3.0 and +3.0 (3=equal, 1=cosine, -2=-sine, etc.)."));
            }
        }

        public static List<ValidationIssue> ValidateAvl(string text, string projectDir)
        {
            var issues = new List<ValidationIssue>();
            var content = ExtractContentLines(text);

            if (content.Count == 0)
            {
                issues.Add(new ValidationIssue(IssueSeverity.Info, 0, "File is empty.", "Add a title line, then Mach / IYsym IZsym Zsym / Sref Cref Bref / Xref Yref Zref header lines, then at least one SURFACE block."));
                return issues;
            }

            int i = 1; // content(0) is the free-text title line - nothing to validate there

            CheckNumericLine(content, i, 1, "Mach number", issues);
            i += 1;
            CheckNumericLine(content, i, 3, "IYsym  IZsym  Zsym", issues);
            var iYsymValue = CheckIYsymIZsymAreIntegers(content, i, issues);
            i += 1;
            CheckPositiveLine(content, i, 3, "Sref  Cref  Bref", issues);
            i += 1;
            CheckNumericLine(content, i, 3, "Xref  Yref  Zref", issues);
            i += 1;

            // Optional CDp line: present iff the next content line isn't a SURFACE/BODY keyword.
            if (i < content.Count && KeyOf(content[i].Text) != "SURFACE" && KeyOf(content[i].Text) != "BODY")
            {
                CheckNumericLine(content, i, 1, "CDp (optional profile drag)", issues);
                i += 1;
            }

            int surfaceCount = 0;
            int sectionCount = 0;
            int controlCount = 0;

            while (i < content.Count)
            {
                string key = KeyOf(content[i].Text);
                switch (key ?? "")
                {
                    case "SURFACE":
                        {
                            surfaceCount += 1;
                            ParseSurfaceBlock(content, ref i, issues, projectDir, ref sectionCount, ref controlCount, iYsymValue);
                            break;
                        }
                    case "BODY":
                        {
                            ParseBodyBlock(content, ref i, issues, projectDir, iYsymValue);
                            break;
                        }

                    default:
                        {
                            issues.Add(new ValidationIssue(IssueSeverity.Warning, content[i].LineNo, $"Unexpected line outside any SURFACE/BODY block: '{content[i].Text.Trim()}'.", "Every block after the header should start with SURFACE or BODY. If this was meant to be a keyword, check its spelling."));
                            i += 1;
                            break;
                        }
                }
            }

            if (surfaceCount == 0)
            {
                issues.Add(new ValidationIssue(IssueSeverity.Error, 0, "No SURFACE (or BODY) blocks found.", "AVL geometry files need at least one SURFACE block to define lifting/aero surfaces."));
            }
            else
            {
                issues.Add(new ValidationIssue(IssueSeverity.Info, 0, $"Found {surfaceCount} surface(s), {sectionCount} section(s), {controlCount} control(s).", ""));
            }

            return issues;
        }

        private static void ParseSurfaceBlock(List<ContentLine> content, ref int i, List<ValidationIssue> issues, string projectDir, ref int sectionCount, ref int controlCount, int? iYsymValue)
        {
            var surfaceLine = content[i];
            i += 1; // consume SURFACE keyword line

            if (i >= content.Count)
            {
                issues.Add(new ValidationIssue(IssueSeverity.Error, surfaceLine.LineNo, "SURFACE keyword is missing its name and Nchord/Cspace lines.", "Add a name line, then a line with Nchord and Cspace (e.g. '8  1.0')."));
                return;
            }
            string surfaceName = content[i].Text.Trim();
            i += 1; // consume name line

            CheckNumericLine(content, i, 2, "Nchord  Cspace", issues);
            if (i < content.Count)
            {
                string[] toks = Tokens(content[i].Text);
                if (toks.Length >= 1 && IsNum(toks[0]))
                {
                    double nch = ParseNum(toks[0]);
                    if (nch <= 0d || nch != Math.Floor(nch))
                    {
                        issues.Add(new ValidationIssue(IssueSeverity.Warning, content[i].LineNo, $"Nchord ({toks[0]}) should be a positive whole number.", "Nchord is the number of chordwise vortex panels, e.g. 8 or 12."));
                    }
                }
                CheckSpacingRange(content, i, 1, "Cspace", issues);
                // Optional 3rd/4th fields: Nspan  Sspace. A suspiciously large Nspan is the
                // classic symptom of a missing space merging "Nspan Sspace" into one token
                // (e.g. "24" and "1.0" becoming "241.0") - CheckNumericLine can't catch this since
                // it only requires the first 2 (mandatory) fields to be numeric.
                if (toks.Length >= 3 && IsNum(toks[2]))
                {
                    double nsp = ParseNum(toks[2]);
                    if (nsp > 100d)
                    {
                        issues.Add(new ValidationIssue(IssueSeverity.Warning, content[i].LineNo, $"Nspan ({toks[2]}) is unusually large for a single surface.", "This is a common symptom of a missing space merging Nspan and Sspace into one number (e.g. '24' and '1.0' becoming '241.0'). Expected: Nchord  Cspace  Nspan  Sspace, e.g. '12  1.0  24  1.0'."));
                    }
                    else if (nsp <= 0d || nsp != Math.Floor(nsp))
                    {
                        issues.Add(new ValidationIssue(IssueSeverity.Warning, content[i].LineNo, $"Nspan ({toks[2]}) should be a positive whole number.", "Nspan is the number of spanwise vortex panels for this surface, e.g. 12 or 24."));
                    }
                }
                CheckSpacingRange(content, i, 3, "Sspace", issues);
            }
            i += 1;

            int localSectionCount = 0;

            while (i < content.Count)
            {
                string key = KeyOf(content[i].Text);
                bool exitWhile = false;
                switch (key ?? "")
                {
                    case "SURFACE":
                    case "BODY":
                        {
                            exitWhile = true;
                            break; // next block starts; the outer loop picks it up
                        }
                    case "SECTION":
                        {
                            sectionCount += 1;
                            localSectionCount += 1;
                            ParseSectionBlock(content, ref i, issues, projectDir, ref controlCount);
                            break;
                        }
                    case "YDUPLICATE":
                        {
                            var kw = content[i];
                            i += 1;
                            CheckNumericLine(content, i, 1, "YDUPLICATE value (Ydupl)", issues);
                            // avl_doc.txt:436-440: "The YDUPLICATE keyword can _only_ be used if iYsym = 0
                            // ... This will almost certainly produce an arithmetic fault" otherwise.
                            if (iYsymValue.HasValue && iYsymValue.Value != 0)
                            {
                                issues.Add(new ValidationIssue(IssueSeverity.Error, kw.LineNo, $"YDUPLICATE is used on surface '{surfaceName}' but IYsym is {iYsymValue.Value} (not 0).", "YDUPLICATE can only be used when IYsym = 0 in the header - otherwise the duplicated surface coincides with AVL's own Y-symmetry image and will almost certainly cause an arithmetic fault. Either remove YDUPLICATE or set IYsym to 0."));
                            }
                            i += 1;
                            break;
                        }
                    case "SCALE":
                        {
                            i += 1;
                            CheckNumericLine(content, i, 3, "SCALE  Xscale  Yscale  Zscale", issues);
                            i += 1;
                            break;
                        }
                    case "TRANSLATE":
                        {
                            i += 1;
                            CheckNumericLine(content, i, 3, "TRANSLATE  dX  dY  dZ", issues);
                            i += 1;
                            break;
                        }
                    case "ANGLE":
                        {
                            i += 1;
                            CheckNumericLine(content, i, 1, "ANGLE (dAinc)", issues);
                            i += 1;
                            break;
                        }
                    case "COMPONENT":
                    case "INDEX":
                        {
                            i += 1;
                            CheckNumericLine(content, i, 1, $"{key} value", issues);
                            i += 1;
                            break;
                        }
                    case "NOWAKE":
                    case "NOALBE":
                    case "NOLOAD":
                        {
                            i += 1; // standalone flag, no data line
                            break;
                        }
                    case "CDCL":
                        {
                            i += 1;
                            CheckNumericLine(content, i, 6, "CDCL  CL1 CD1 CL2 CD2 CL3 CD3", issues);
                            i += 1;
                            break;
                        }

                    default:
                        {
                            issues.Add(new ValidationIssue(IssueSeverity.Warning, content[i].LineNo, $"Unrecognized line inside surface '{surfaceName}': '{content[i].Text.Trim()}'.", "Expected a SECTION, or one of YDUPLICATE/SCALE/TRANSLATE/ANGLE/NOWAKE/NOALBE/NOLOAD/COMPONENT/INDEX/CDCL."));
                            i += 1;
                            break;
                        }
                }

                if (exitWhile)
                {
                    break;
                }
            }

            // avl_doc.txt:355,601-602: "At least two SECTION keywords must be used for each surface."
            if (localSectionCount < 2)
            {
                issues.Add(new ValidationIssue(IssueSeverity.Error, surfaceLine.LineNo, $"Surface '{surfaceName}' has only {localSectionCount} SECTION(s).", "AVL requires at least two SECTIONs per surface (e.g. root and tip) - the chord/incidence are linearly interpolated between them."));
            }
        }

        private static void ParseSectionBlock(List<ContentLine> content, ref int i, List<ValidationIssue> issues, string projectDir, ref int controlCount)
        {
            var sectionLine = content[i];
            i += 1; // consume SECTION keyword

            if (i >= content.Count)
            {
                issues.Add(new ValidationIssue(IssueSeverity.Error, sectionLine.LineNo, "SECTION keyword is missing its data line.", "Add a line: Xle  Yle  Zle  Chord  Ainc  [Nspan  Sspace]"));
                return;
            }

            string[] toks = Tokens(content[i].Text);
            int numericCount = toks.Count(t => IsNum(t));
            if (numericCount < 5)
            {
                issues.Add(new ValidationIssue(IssueSeverity.Error, content[i].LineNo, $"SECTION data line has only {numericCount} numeric value(s); AVL expects at least 5 (Xle, Yle, Zle, Chord, Ainc).", "Format: Xle  Yle  Zle  Chord  Ainc  [Nspan  Sspace]"));
            }
            else if (toks.Length > 3 && IsNum(toks[3]))
            {
                double chord = ParseNum(toks[3]);
                if (chord <= 0d)
                {
                    issues.Add(new ValidationIssue(IssueSeverity.Warning, content[i].LineNo, $"Chord is {chord} (zero or negative).", "Chord should normally be a positive length; a zero/negative chord collapses or inverts this section's panel."));
                }
            }
            CheckSpacingRange(content, i, 6, "Sspace", issues);
            i += 1;

            while (i < content.Count)
            {
                string key = KeyOf(content[i].Text);
                bool exitWhile = false;
                switch (key ?? "")
                {
                    case "SURFACE":
                    case "BODY":
                    case "SECTION":
                        {
                            exitWhile = true;
                            break;
                        }
                    case "CONTROL":
                        {
                            controlCount += 1;
                            var kw = content[i];
                            i += 1;
                            if (i >= content.Count)
                            {
                                issues.Add(new ValidationIssue(IssueSeverity.Error, kw.LineNo, "CONTROL keyword is missing its data line.", "Add a line: Cname  Cgain  Xhinge  XYZhvecX  XYZhvecY  XYZhvecZ  SgnDup"));
                            }
                            else
                            {
                                string[] ctoks = Tokens(content[i].Text);
                                int cnum = ctoks.Skip(1).Count(t => IsNum(t));
                                if (ctoks.Length < 7 || cnum < 6)
                                {
                                    issues.Add(new ValidationIssue(IssueSeverity.Error, content[i].LineNo, $"CONTROL line needs a name followed by 6 numeric values (Cgain, Xhinge, XYZhvec x/y/z, SgnDup); found {ctoks.Length} token(s).", "Format: Cname  Cgain  Xhinge  XYZhvecX  XYZhvecY  XYZhvecZ  SgnDup, e.g. 'elevator  1.0  0.6  0.0 1.0 0.0  1.0'"));
                                }
                                i += 1;
                            }

                            break;
                        }
                    case "NACA":
                        {
                            var kw = content[i];
                            i += 1;
                            if (i >= content.Count || !Regex.IsMatch(content[i].Text.Trim(), @"^\d{4,5}$"))
                            {
                                issues.Add(new ValidationIssue(IssueSeverity.Warning, kw.LineNo, "NACA keyword should be followed by a 4- or 5-digit code.", "Example: NACA then '2412' on the next line."));
                            }
                            else
                            {
                                i += 1;
                            }

                            break;
                        }
                    case "AFILE":
                    case "AFIL":
                        {
                            var kw = content[i];
                            i += 1;
                            if (i >= content.Count)
                            {
                                issues.Add(new ValidationIssue(IssueSeverity.Error, kw.LineNo, "AFILE keyword is missing its filename.", "Add the airfoil coordinate filename on the next line."));
                            }
                            else
                            {
                                string fname = StripQuotes(content[i].Text);
                                if (!string.IsNullOrEmpty(projectDir) && !File.Exists(Path.Combine(projectDir, fname)))
                                {
                                    issues.Add(new ValidationIssue(IssueSeverity.Warning, content[i].LineNo, $"Airfoil file not found: '{fname}'.", $"Make sure '{fname}' exists alongside the project's .avl file, or check for a typo."));
                                }
                                i += 1;
                            }

                            break;
                        }
                    case "AIRFOIL":
                        {
                            i += 1;
                            // The coordinate table is free-form (x y pairs, optionally an "1 1" xle/xte
                            // fraction line first) - skip lines that aren't a recognized keyword.
                            while (i < content.Count && !IsKnownAvlKeyword(KeyOf(content[i].Text)))
                                i += 1;
                            break;
                        }
                    case "CLAF":
                        {
                            i += 1;
                            CheckNumericLine(content, i, 1, "CLAF value", issues);
                            i += 1;
                            break;
                        }
                    case "CDCL":
                        {
                            i += 1;
                            CheckNumericLine(content, i, 6, "CDCL  CL1 CD1 CL2 CD2 CL3 CD3", issues);
                            i += 1;
                            break;
                        }
                    case "DESIGN":
                        {
                            var kw = content[i];
                            i += 1;
                            if (i >= content.Count)
                            {
                                issues.Add(new ValidationIssue(IssueSeverity.Error, kw.LineNo, "DESIGN keyword is missing its data line.", "Add a line: DName  Wdes (design parameter name and local weight)."));
                            }
                            else
                            {
                                string[] dtoks = Tokens(content[i].Text);
                                if (dtoks.Length < 2 || !IsNum(dtoks[1]))
                                {
                                    issues.Add(new ValidationIssue(IssueSeverity.Error, content[i].LineNo, "DESIGN line needs a name followed by one numeric weight.", "Format: DName  Wdes, e.g. 'twist1  -0.5'."));
                                }
                                i += 1;
                            }

                            break;
                        }

                    default:
                        {
                            issues.Add(new ValidationIssue(IssueSeverity.Warning, content[i].LineNo, $"Unrecognized line inside a SECTION: '{content[i].Text.Trim()}'.", "Expected CONTROL, NACA, AFILE, AIRFOIL, CLAF, CDCL, DESIGN, or the start of the next SECTION/SURFACE/BODY."));
                            i += 1;
                            break;
                        }
                }

                if (exitWhile)
                {
                    break;
                }
            }
        }

        private static void ParseBodyBlock(List<ContentLine> content, ref int i, List<ValidationIssue> issues, string projectDir, int? iYsymValue)
        {
            var bodyLine = content[i];
            i += 1;
            if (i >= content.Count)
            {
                issues.Add(new ValidationIssue(IssueSeverity.Error, bodyLine.LineNo, "BODY keyword is missing its name and Nbody/BSpace lines.", "Add a name line, then a line with Nbody and BSpace."));
                return;
            }
            string bodyName = content[i].Text.Trim();
            i += 1;
            CheckNumericLine(content, i, 2, "Nbody  BSpace", issues);
            CheckSpacingRange(content, i, 1, "Bspace", issues);
            i += 1;

            while (i < content.Count)
            {
                string key = KeyOf(content[i].Text);
                bool exitWhile = false;
                switch (key ?? "")
                {
                    case "SURFACE":
                    case "BODY":
                        {
                            exitWhile = true;
                            break;
                        }
                    case "BFILE":
                        {
                            var kw = content[i];
                            i += 1;
                            if (i >= content.Count)
                            {
                                issues.Add(new ValidationIssue(IssueSeverity.Error, kw.LineNo, "BFILE keyword is missing its filename.", "Add the body coordinate filename on the next line."));
                            }
                            else
                            {
                                string fname = StripQuotes(content[i].Text);
                                if (!string.IsNullOrEmpty(projectDir) && !File.Exists(Path.Combine(projectDir, fname)))
                                {
                                    issues.Add(new ValidationIssue(IssueSeverity.Warning, content[i].LineNo, $"Body file not found: '{fname}'.", $"Make sure '{fname}' exists alongside the project's .avl file, or check for a typo."));
                                }
                                i += 1;
                            }

                            break;
                        }
                    case "YDUPLICATE":
                        {
                            var kw = content[i];
                            i += 1;
                            CheckNumericLine(content, i, 1, "YDUPLICATE value (Ydupl)", issues);
                            if (iYsymValue.HasValue && iYsymValue.Value != 0)
                            {
                                issues.Add(new ValidationIssue(IssueSeverity.Error, kw.LineNo, $"YDUPLICATE is used on body '{bodyName}' but IYsym is {iYsymValue.Value} (not 0).", "YDUPLICATE can only be used when IYsym = 0 in the header - otherwise the duplicated body coincides with AVL's own Y-symmetry image and will almost certainly cause an arithmetic fault."));
                            }
                            i += 1;
                            break;
                        }
                    case "SCALE":
                        {
                            i += 1;
                            CheckNumericLine(content, i, 3, "SCALE  Xscale  Yscale  Zscale", issues);
                            i += 1;
                            break;
                        }
                    case "TRANSLATE":
                        {
                            i += 1;
                            CheckNumericLine(content, i, 3, "TRANSLATE  dX  dY  dZ", issues);
                            i += 1;
                            break;
                        }

                    default:
                        {
                            issues.Add(new ValidationIssue(IssueSeverity.Warning, content[i].LineNo, $"Unrecognized line inside body '{bodyName}': '{content[i].Text.Trim()}'.", "Expected BFILE, YDUPLICATE, SCALE, TRANSLATE, or the start of the next SURFACE/BODY."));
                            i += 1;
                            break;
                        }
                }

                if (exitWhile)
                {
                    break;
                }
            }
        }

        #endregion

        #region .mass validation

        public static List<ValidationIssue> ValidateMass(string text)
        {
            var issues = new List<ValidationIssue>();
            string[] lines = text.Replace(Constants.vbCr, "").Split(Constants.vbLf);
            bool sawDataRow = false;

            for (int li = 0, loopTo = lines.Length - 1; li <= loopTo; li++)
            {
                int lineNo = li + 1;
                string line = StripInlineComment(lines[li]).Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                    continue;

                // avl_doc.txt:1264-1267: a line starting with "*" sets multipliers, "+" sets added
                // constants, applied to all subsequent data rows - these are not mass points themselves.
                if (line.StartsWith("*") || line.StartsWith("+"))
                {
                    string[] mtoks = Tokens(line.Substring(1));
                    if (mtoks.Length == 0 || !mtoks.All(t => IsNum(t)))
                    {
                        issues.Add(new ValidationIssue(IssueSeverity.Error, lineNo, $"'{line[0]}' multiplier/adder row has non-numeric values.", "A '*' row sets multipliers and a '+' row sets added constants for all following data rows - every value after the prefix character must be numeric."));
                    }
                    continue;
                }

                int eqIdx = line.IndexOf('=');
                if (eqIdx >= 0 && !sawDataRow)
                {
                    // Header assignment line: Lunit/Munit/Tunit/g/rho = <number> [unit text]
                    string key = line.Substring(0, eqIdx).Trim();
                    string firstTok = Tokens(line.Substring(eqIdx + 1)).FirstOrDefault();
                    if (firstTok is null || !IsNum(firstTok))
                    {
                        issues.Add(new ValidationIssue(IssueSeverity.Error, lineNo, $"'{key}' should be followed by a numeric value.", "Example: 'Lunit = 0.0254 meters' or 'g = 9.81'."));
                    }
                    continue;
                }

                // Otherwise: a mass-point data row.
                sawDataRow = true;
                string[] toks = Tokens(line);
                int numericPrefixCount = 0;
                foreach (var t in toks)
                {
                    if (IsNum(t))
                    {
                        numericPrefixCount += 1;
                    }
                    else
                    {
                        break;
                    }
                }

                // avl_doc.txt:1269-1278: mass/x/y/z are the only required fields - "the inertia values on
                // each line are optional, and any ones which are absent will be assumed to be zero."
                if (numericPrefixCount < 4)
                {
                    issues.Add(new ValidationIssue(IssueSeverity.Error, lineNo, $"Mass row has only {numericPrefixCount} numeric value(s) before the first non-numeric token; AVL requires at least 4 (mass, x, y, z).", "Format: mass  x  y  z  [Ixx  Iyy  Izz  Ixz  Ixy  Iyz]  [component name]. Inertia terms are optional and default to 0 if omitted."));
                }
                else if (IsNum(toks[0]) && ParseNum(toks[0]) <= 0d)
                {
                    issues.Add(new ValidationIssue(IssueSeverity.Warning, lineNo, $"Mass value is {toks[0]} (zero or negative).", "A mass point normally needs a positive mass; zero/negative mass can produce nonsensical inertia results."));
                }
            }

            if (!sawDataRow)
            {
                issues.Add(new ValidationIssue(IssueSeverity.Info, 0, "No mass point rows found.", "Add at least one row: mass  x  y  z"));
            }

            return issues;
        }

        #endregion

        #region .run validation

        public static List<ValidationIssue> ValidateRun(string text)
        {
            var issues = new List<ValidationIssue>();
            string[] lines = text.Replace(Constants.vbCr, "").Split(Constants.vbLf);
            bool sawRunCase = false;
            // avl_doc.txt:1522: "A constraint can be used no more than once." - tracked per run case,
            // reset every time a new "Run case N:" header is seen.
            var seenConstraintTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int li = 0, loopTo = lines.Length - 1; li <= loopTo; li++)
            {
                int lineNo = li + 1;
                string trimmed = lines[li].Trim();
                if (trimmed.Length == 0)
                    continue;
                if (Regex.IsMatch(trimmed, "^-+$"))
                    continue; // divider lines of dashes
                if (trimmed.StartsWith("#") || trimmed.StartsWith("!"))
                    continue; // full-line comment

                if (Regex.IsMatch(trimmed, @"^Run\s*[Cc]ase\s+\d+", RegexOptions.IgnoreCase))
                {
                    sawRunCase = true;
                    seenConstraintTargets.Clear();
                    continue;
                }

                if (trimmed.Contains("->"))
                {
                    string[] arrowParts = trimmed.Split(new string[] { "->" }, StringSplitOptions.None);
                    if (arrowParts.Length != 2)
                    {
                        issues.Add(new ValidationIssue(IssueSeverity.Error, lineNo, "Malformed constraint line (expected exactly one '->').", "Format: '<variable>  ->  <target> = <value>', e.g. 'alpha  ->  alpha = 5.0'."));
                        continue;
                    }
                    string rhs = arrowParts[1];
                    int eq = rhs.IndexOf('=');
                    if (eq < 0)
                    {
                        issues.Add(new ValidationIssue(IssueSeverity.Error, lineNo, "Constraint line is missing '=' after '->'.", "Format: '<variable>  ->  <target> = <value>'."));
                        continue;
                    }
                    string targetName = rhs.Substring(0, eq).Trim();
                    string firstTok = Tokens(rhs.Substring(eq + 1)).FirstOrDefault();
                    if (firstTok is null || !IsNum(firstTok))
                    {
                        issues.Add(new ValidationIssue(IssueSeverity.Error, lineNo, $"Constraint value is not numeric: '{rhs.Substring(eq + 1).Trim()}'.", "The value after '=' must be a number, e.g. 'alpha  ->  alpha = 5.0'."));
                    }
                    else if (targetName.Length > 0 && !seenConstraintTargets.Add(targetName))
                    {
                        issues.Add(new ValidationIssue(IssueSeverity.Error, lineNo, $"Constraint target '{targetName}' is used more than once in this run case.", "AVL does not allow the same constraint (the name after '->') to be used for more than one variable in the same run case - each quantity (e.g. CL, alpha, elevator) can only be pinned once."));
                    }
                }
                else if (trimmed.Contains('='))
                {
                    int eq = trimmed.IndexOf('=');
                    string firstTok = Tokens(trimmed.Substring(eq + 1)).FirstOrDefault();
                    if (firstTok is null || !IsNum(firstTok))
                    {
                        issues.Add(new ValidationIssue(IssueSeverity.Error, lineNo, $"'{trimmed.Substring(0, eq).Trim()}' is not followed by a numeric value.", "Parameter lines need a numeric value after '=', e.g. 'CL        =   0.40000'."));
                    }
                }
                // Anything else (case title lines, section labels) is free text - not checked.
            }

            if (!sawRunCase)
            {
                issues.Add(new ValidationIssue(IssueSeverity.Info, 0, "No 'Run case N:' header found.", "A .run file normally starts each case with a line like ' Run case  1:   my case name'."));
            }

            return issues;
        }

        #endregion

    }
}