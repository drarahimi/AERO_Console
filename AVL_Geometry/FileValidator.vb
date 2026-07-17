Option Explicit On
Option Strict On

Imports System.Globalization
Imports System.IO
Imports System.Text.RegularExpressions

Public Enum IssueSeverity
    [Error]
    Warning
    Info
End Enum

''' <summary>
''' One finding from FileValidator: what's wrong, where, and how to fix it.
''' LineNumber is 1-based; 0 means the issue applies to the file as a whole (no single line).
''' </summary>
Public Class ValidationIssue
    Public Property Severity As IssueSeverity
    Public Property LineNumber As Integer
    Public Property Message As String
    Public Property FixHint As String

    Public Sub New(severity As IssueSeverity, lineNumber As Integer, message As String, fixHint As String)
        Me.Severity = severity
        Me.LineNumber = lineNumber
        Me.Message = message
        Me.FixHint = fixHint
    End Sub
End Class

''' <summary>
''' Line-based structural checker for AVL's .avl/.mass/.run text formats, cross-checked directly
''' against AVL's own bundled documentation (appdata/avl_doc.txt) rather than assumption - e.g. the
''' "at least 2 SECTIONs per surface", "YDUPLICATE requires IYsym=0", "spacing parameters must be in
''' -3..3", and mass file "*"/"+" multiplier-row syntax rules below are all taken directly from it.
''' This is deliberately a second, independent read of the grammar from the app's existing parsers
''' (findPoints/ScanGeometryBlocks in frmGeometry.vb) - those are optimized to degrade silently on
''' malformed input (skip a bad token, keep going) so the 3D view never crashes on a half-edited file.
''' A validator needs the opposite instinct: notice the malformed token and explain it.
''' Not a full AVL grammar/parser - covers the structural mistakes users actually make.
''' </summary>
Public Module FileValidator

    Private Structure ContentLine
        Public Text As String
        Public LineNo As Integer
    End Structure

    Private ReadOnly KnownAvlKeywords As New HashSet(Of String) From {
        "SURFACE", "BODY", "SECTION", "CONTROL", "NACA", "AFILE", "AFIL", "AIRFOIL", "CLAF", "CDCL", "DESIGN",
        "YDUPLICATE", "SCALE", "TRANSLATE", "ANGLE", "NOWAKE", "NOALBE", "NOLOAD", "COMPONENT", "INDEX", "BFILE"
    }

    Public Function ValidateFile(text As String, fileKind As String, projectDir As String) As List(Of ValidationIssue)
        Select Case fileKind
            Case "Geometry" : Return ValidateAvl(text, projectDir)
            Case "Mass" : Return ValidateMass(text)
            Case "Run" : Return ValidateRun(text)
            Case Else : Return New List(Of ValidationIssue)
        End Select
    End Function

#Region "Shared helpers"

    Private Function Tokens(s As String) As String()
        Return s.Split(New Char() {" "c, ControlChars.Tab}, StringSplitOptions.RemoveEmptyEntries)
    End Function

    Private Function IsNum(s As String) As Boolean
        Dim d As Double
        Return Double.TryParse(s, NumberStyles.Float Or NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, d)
    End Function

    Private Function ParseNum(s As String) As Double
        Return Double.Parse(s, NumberStyles.Float Or NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture)
    End Function

    Private Function StripInlineComment(line As String) As String
        Dim idx = line.IndexOf("!"c)
        If idx >= 0 Then Return line.Substring(0, idx)
        Return line
    End Function

    Private Function StripQuotes(s As String) As String
        Dim t = s.Trim()
        If t.Length >= 2 AndAlso t.StartsWith("""") AndAlso t.EndsWith("""") Then Return t.Substring(1, t.Length - 2)
        Return t
    End Function

    ''' <summary>Blank lines and full-line "#"/"!" comments removed; inline "! ..." trailing comments stripped.</summary>
    Private Function ExtractContentLines(text As String) As List(Of ContentLine)
        Dim result As New List(Of ContentLine)
        Dim rawLines = text.Replace(vbCr, "").Split(vbLf)
        For li = 0 To rawLines.Length - 1
            Dim s = StripInlineComment(rawLines(li)).Trim()
            If s.Length = 0 Then Continue For
            If s.StartsWith("#") OrElse s.StartsWith("!") Then Continue For
            result.Add(New ContentLine With {.Text = s, .LineNo = li + 1})
        Next
        Return result
    End Function

    ''' <summary>
    ''' AVL matches keywords by only their first 4 characters (e.g. "SURF" for SURFACE, "COMP" for
    ''' COMPONENT) - this maps any such abbreviation back to its canonical full keyword so the rest
    ''' of the parser's plain string comparisons ("SURFACE", "SECTION", ...) keep working unchanged.
    ''' </summary>
    Private Function KeyOf(s As String) As String
        Dim toks = Tokens(s)
        If toks.Length = 0 Then Return ""
        Dim token = toks(0).ToUpperInvariant()
        If token.Length < 4 Then Return token
        Dim prefix4 = token.Substring(0, 4)
        For Each kw In KnownAvlKeywords
            If kw.Substring(0, 4) = prefix4 Then Return kw
        Next
        Return token
    End Function

#End Region

#Region ".avl validation"

    Private Function IsKnownAvlKeyword(key As String) As Boolean
        Return KnownAvlKeywords.Contains(key)
    End Function

    Private Sub CheckNumericLine(content As List(Of ContentLine), idx As Integer, expectedCount As Integer, label As String, issues As List(Of ValidationIssue))
        If idx >= content.Count Then
            Dim lastLine = If(content.Count > 0, content(content.Count - 1).LineNo, 0)
            issues.Add(New ValidationIssue(IssueSeverity.Error, lastLine,
                $"File ends before the required '{label}' line.",
                $"Add a line with {expectedCount} numeric value(s): {label}."))
            Return
        End If
        Dim toks = Tokens(content(idx).Text)
        Dim numericCount = toks.Take(expectedCount).Count(Function(t) IsNum(t))
        If toks.Length < expectedCount OrElse numericCount < expectedCount Then
            issues.Add(New ValidationIssue(IssueSeverity.Error, content(idx).LineNo,
                $"Expected {expectedCount} numeric value(s) for '{label}', found: '{content(idx).Text.Trim()}'.",
                $"This line should contain {expectedCount} number(s) - {label} - separated by spaces."))
        End If
    End Sub

    Private Sub CheckPositiveLine(content As List(Of ContentLine), idx As Integer, expectedCount As Integer, label As String, issues As List(Of ValidationIssue))
        CheckNumericLine(content, idx, expectedCount, label, issues)
        If idx < content.Count Then
            For Each t In Tokens(content(idx).Text).Take(expectedCount)
                If IsNum(t) AndAlso ParseNum(t) <= 0 Then
                    issues.Add(New ValidationIssue(IssueSeverity.Warning, content(idx).LineNo,
                        $"'{label}' contains a zero or negative value ({t}).",
                        "Reference area/chord/span (Sref, Cref, Bref) are normally positive."))
                    Exit For
                End If
            Next
        End If
    End Sub

    ''' <summary>Bare-integer token check by literal text (e.g. "1"), not by numeric value - "1.00" is
    ''' numerically whole but is still written as a decimal, which is what actually matters here.</summary>
    Private Function IsIntegerToken(s As String) As Boolean
        Return Regex.IsMatch(s, "^[+-]?\d+$")
    End Function

    ''' <summary>
    ''' IYsym and IZsym are read as whole numbers restricted to -1/0/1, unlike Zsym on the same line
    ''' which is a real-valued Z location - AVL's doc: "iYsym = 1 ... = -1 ... = 0" (avl_doc.txt:260-266).
    ''' Returns the parsed IYsym value if it's a clean integer, for the YDUPLICATE cross-check below.
    ''' </summary>
    Private Function CheckIYsymIZsymAreIntegers(content As List(Of ContentLine), idx As Integer, issues As List(Of ValidationIssue)) As Integer?
        If idx >= content.Count Then Return Nothing
        Dim toks = Tokens(content(idx).Text)
        Dim iYsymValue As Integer? = Nothing

        If toks.Length >= 1 AndAlso IsNum(toks(0)) Then
            If Not IsIntegerToken(toks(0)) Then
                issues.Add(New ValidationIssue(IssueSeverity.Error, content(idx).LineNo,
                    $"IYsym ('{toks(0)}') is written as a decimal; AVL expects it as a whole number.",
                    "Write IYsym with no decimal point: 0 (no symmetry), 1 (mirror about Y=0), or -1 (mirror, flow-reversed). E.g. '0' instead of '0.00'."))
            Else
                Dim v = CInt(ParseNum(toks(0)))
                iYsymValue = v
                If v < -1 OrElse v > 1 Then
                    issues.Add(New ValidationIssue(IssueSeverity.Warning, content(idx).LineNo,
                        $"IYsym ({v}) is outside the documented range.",
                        "IYsym must be -1, 0, or 1: 0 = no symmetry, 1 = mirror about Y=0, -1 = mirror with flow reversed."))
                End If
            End If
        End If

        If toks.Length >= 2 AndAlso IsNum(toks(1)) Then
            If Not IsIntegerToken(toks(1)) Then
                issues.Add(New ValidationIssue(IssueSeverity.Error, content(idx).LineNo,
                    $"IZsym ('{toks(1)}') is written as a decimal; AVL expects it as a whole number.",
                    "Write IZsym with no decimal point: 0 (no symmetry), 1 (mirror about Z=Zsym), or -1 (mirror, flow-reversed). E.g. '0' instead of '1.00'."))
            Else
                Dim v = CInt(ParseNum(toks(1)))
                If v < -1 OrElse v > 1 Then
                    issues.Add(New ValidationIssue(IssueSeverity.Warning, content(idx).LineNo,
                        $"IZsym ({v}) is outside the documented range.",
                        "IZsym must be -1, 0, or 1: 0 = no symmetry, 1 = mirror about Z=Zsym, -1 = mirror with flow reversed."))
                End If
            End If
        End If

        Return iYsymValue
    End Function

    ''' <summary>Spacing parameters (Cspace, Sspace, Bspace) must fall in -3.0..+3.0 (avl_doc.txt:1004).</summary>
    Private Sub CheckSpacingRange(content As List(Of ContentLine), idx As Integer, tokenIndex As Integer, label As String, issues As List(Of ValidationIssue))
        If idx >= content.Count Then Return
        Dim toks = Tokens(content(idx).Text)
        If toks.Length <= tokenIndex OrElse Not IsNum(toks(tokenIndex)) Then Return
        Dim v = ParseNum(toks(tokenIndex))
        If v < -3.0 OrElse v > 3.0 Then
            issues.Add(New ValidationIssue(IssueSeverity.Error, content(idx).LineNo,
                $"{label} ({toks(tokenIndex)}) is outside the valid range.",
                $"{label} is a vortex-spacing parameter and must fall between -3.0 and +3.0 (3=equal, 1=cosine, -2=-sine, etc.)."))
        End If
    End Sub

    Public Function ValidateAvl(text As String, projectDir As String) As List(Of ValidationIssue)
        Dim issues As New List(Of ValidationIssue)
        Dim content = ExtractContentLines(text)

        If content.Count = 0 Then
            issues.Add(New ValidationIssue(IssueSeverity.Info, 0, "File is empty.",
                "Add a title line, then Mach / IYsym IZsym Zsym / Sref Cref Bref / Xref Yref Zref header lines, then at least one SURFACE block."))
            Return issues
        End If

        Dim i As Integer = 1 ' content(0) is the free-text title line - nothing to validate there

        CheckNumericLine(content, i, 1, "Mach number", issues) : i += 1
        CheckNumericLine(content, i, 3, "IYsym  IZsym  Zsym", issues)
        Dim iYsymValue = CheckIYsymIZsymAreIntegers(content, i, issues) : i += 1
        CheckPositiveLine(content, i, 3, "Sref  Cref  Bref", issues) : i += 1
        CheckNumericLine(content, i, 3, "Xref  Yref  Zref", issues) : i += 1

        ' Optional CDp line: present iff the next content line isn't a SURFACE/BODY keyword.
        If i < content.Count AndAlso KeyOf(content(i).Text) <> "SURFACE" AndAlso KeyOf(content(i).Text) <> "BODY" Then
            CheckNumericLine(content, i, 1, "CDp (optional profile drag)", issues)
            i += 1
        End If

        Dim surfaceCount = 0, sectionCount = 0, controlCount = 0

        While i < content.Count
            Dim key = KeyOf(content(i).Text)
            Select Case key
                Case "SURFACE"
                    surfaceCount += 1
                    ParseSurfaceBlock(content, i, issues, projectDir, sectionCount, controlCount, iYsymValue)
                Case "BODY"
                    ParseBodyBlock(content, i, issues, projectDir, iYsymValue)
                Case Else
                    issues.Add(New ValidationIssue(IssueSeverity.Warning, content(i).LineNo,
                        $"Unexpected line outside any SURFACE/BODY block: '{content(i).Text.Trim()}'.",
                        "Every block after the header should start with SURFACE or BODY. If this was meant to be a keyword, check its spelling."))
                    i += 1
            End Select
        End While

        If surfaceCount = 0 Then
            issues.Add(New ValidationIssue(IssueSeverity.Error, 0, "No SURFACE (or BODY) blocks found.",
                "AVL geometry files need at least one SURFACE block to define lifting/aero surfaces."))
        Else
            issues.Add(New ValidationIssue(IssueSeverity.Info, 0,
                $"Found {surfaceCount} surface(s), {sectionCount} section(s), {controlCount} control(s).", ""))
        End If

        Return issues
    End Function

    Private Sub ParseSurfaceBlock(content As List(Of ContentLine), ByRef i As Integer, issues As List(Of ValidationIssue),
                                   projectDir As String, ByRef sectionCount As Integer, ByRef controlCount As Integer,
                                   iYsymValue As Integer?)
        Dim surfaceLine = content(i)
        i += 1 ' consume SURFACE keyword line

        If i >= content.Count Then
            issues.Add(New ValidationIssue(IssueSeverity.Error, surfaceLine.LineNo,
                "SURFACE keyword is missing its name and Nchord/Cspace lines.",
                "Add a name line, then a line with Nchord and Cspace (e.g. '8  1.0')."))
            Return
        End If
        Dim surfaceName = content(i).Text.Trim()
        i += 1 ' consume name line

        CheckNumericLine(content, i, 2, "Nchord  Cspace", issues)
        If i < content.Count Then
            Dim toks = Tokens(content(i).Text)
            If toks.Length >= 1 AndAlso IsNum(toks(0)) Then
                Dim nch = ParseNum(toks(0))
                If nch <= 0 OrElse nch <> Math.Floor(nch) Then
                    issues.Add(New ValidationIssue(IssueSeverity.Warning, content(i).LineNo,
                        $"Nchord ({toks(0)}) should be a positive whole number.",
                        "Nchord is the number of chordwise vortex panels, e.g. 8 or 12."))
                End If
            End If
            CheckSpacingRange(content, i, 1, "Cspace", issues)
            ' Optional 3rd/4th fields: Nspan  Sspace. A suspiciously large Nspan is the
            ' classic symptom of a missing space merging "Nspan Sspace" into one token
            ' (e.g. "24" and "1.0" becoming "241.0") - CheckNumericLine can't catch this since
            ' it only requires the first 2 (mandatory) fields to be numeric.
            If toks.Length >= 3 AndAlso IsNum(toks(2)) Then
                Dim nsp = ParseNum(toks(2))
                If nsp > 100 Then
                    issues.Add(New ValidationIssue(IssueSeverity.Warning, content(i).LineNo,
                        $"Nspan ({toks(2)}) is unusually large for a single surface.",
                        "This is a common symptom of a missing space merging Nspan and Sspace into one number (e.g. '24' and '1.0' becoming '241.0'). Expected: Nchord  Cspace  Nspan  Sspace, e.g. '12  1.0  24  1.0'."))
                ElseIf nsp <= 0 OrElse nsp <> Math.Floor(nsp) Then
                    issues.Add(New ValidationIssue(IssueSeverity.Warning, content(i).LineNo,
                        $"Nspan ({toks(2)}) should be a positive whole number.",
                        "Nspan is the number of spanwise vortex panels for this surface, e.g. 12 or 24."))
                End If
            End If
            CheckSpacingRange(content, i, 3, "Sspace", issues)
        End If
        i += 1

        Dim localSectionCount = 0

        While i < content.Count
            Dim key = KeyOf(content(i).Text)
            Select Case key
                Case "SURFACE", "BODY"
                    Exit While ' next block starts; the outer loop picks it up
                Case "SECTION"
                    sectionCount += 1
                    localSectionCount += 1
                    ParseSectionBlock(content, i, issues, projectDir, controlCount)
                Case "YDUPLICATE"
                    Dim kw = content(i) : i += 1
                    CheckNumericLine(content, i, 1, "YDUPLICATE value (Ydupl)", issues)
                    ' avl_doc.txt:436-440: "The YDUPLICATE keyword can _only_ be used if iYsym = 0
                    ' ... This will almost certainly produce an arithmetic fault" otherwise.
                    If iYsymValue.HasValue AndAlso iYsymValue.Value <> 0 Then
                        issues.Add(New ValidationIssue(IssueSeverity.Error, kw.LineNo,
                            $"YDUPLICATE is used on surface '{surfaceName}' but IYsym is {iYsymValue.Value} (not 0).",
                            "YDUPLICATE can only be used when IYsym = 0 in the header - otherwise the duplicated surface coincides with AVL's own Y-symmetry image and will almost certainly cause an arithmetic fault. Either remove YDUPLICATE or set IYsym to 0."))
                    End If
                    i += 1
                Case "SCALE"
                    i += 1 : CheckNumericLine(content, i, 3, "SCALE  Xscale  Yscale  Zscale", issues) : i += 1
                Case "TRANSLATE"
                    i += 1 : CheckNumericLine(content, i, 3, "TRANSLATE  dX  dY  dZ", issues) : i += 1
                Case "ANGLE"
                    i += 1 : CheckNumericLine(content, i, 1, "ANGLE (dAinc)", issues) : i += 1
                Case "COMPONENT", "INDEX"
                    i += 1 : CheckNumericLine(content, i, 1, $"{key} value", issues) : i += 1
                Case "NOWAKE", "NOALBE", "NOLOAD"
                    i += 1 ' standalone flag, no data line
                Case "CDCL"
                    i += 1 : CheckNumericLine(content, i, 6, "CDCL  CL1 CD1 CL2 CD2 CL3 CD3", issues) : i += 1
                Case Else
                    issues.Add(New ValidationIssue(IssueSeverity.Warning, content(i).LineNo,
                        $"Unrecognized line inside surface '{surfaceName}': '{content(i).Text.Trim()}'.",
                        "Expected a SECTION, or one of YDUPLICATE/SCALE/TRANSLATE/ANGLE/NOWAKE/NOALBE/NOLOAD/COMPONENT/INDEX/CDCL."))
                    i += 1
            End Select
        End While

        ' avl_doc.txt:355,601-602: "At least two SECTION keywords must be used for each surface."
        If localSectionCount < 2 Then
            issues.Add(New ValidationIssue(IssueSeverity.Error, surfaceLine.LineNo,
                $"Surface '{surfaceName}' has only {localSectionCount} SECTION(s).",
                "AVL requires at least two SECTIONs per surface (e.g. root and tip) - the chord/incidence are linearly interpolated between them."))
        End If
    End Sub

    Private Sub ParseSectionBlock(content As List(Of ContentLine), ByRef i As Integer, issues As List(Of ValidationIssue),
                                   projectDir As String, ByRef controlCount As Integer)
        Dim sectionLine = content(i)
        i += 1 ' consume SECTION keyword

        If i >= content.Count Then
            issues.Add(New ValidationIssue(IssueSeverity.Error, sectionLine.LineNo,
                "SECTION keyword is missing its data line.",
                "Add a line: Xle  Yle  Zle  Chord  Ainc  [Nspan  Sspace]"))
            Return
        End If

        Dim toks = Tokens(content(i).Text)
        Dim numericCount = toks.Count(Function(t) IsNum(t))
        If numericCount < 5 Then
            issues.Add(New ValidationIssue(IssueSeverity.Error, content(i).LineNo,
                $"SECTION data line has only {numericCount} numeric value(s); AVL expects at least 5 (Xle, Yle, Zle, Chord, Ainc).",
                "Format: Xle  Yle  Zle  Chord  Ainc  [Nspan  Sspace]"))
        ElseIf toks.Length > 3 AndAlso IsNum(toks(3)) Then
            Dim chord = ParseNum(toks(3))
            If chord <= 0 Then
                issues.Add(New ValidationIssue(IssueSeverity.Warning, content(i).LineNo,
                    $"Chord is {chord} (zero or negative).",
                    "Chord should normally be a positive length; a zero/negative chord collapses or inverts this section's panel."))
            End If
        End If
        CheckSpacingRange(content, i, 6, "Sspace", issues)
        i += 1

        While i < content.Count
            Dim key = KeyOf(content(i).Text)
            Select Case key
                Case "SURFACE", "BODY", "SECTION"
                    Exit While
                Case "CONTROL"
                    controlCount += 1
                    Dim kw = content(i) : i += 1
                    If i >= content.Count Then
                        issues.Add(New ValidationIssue(IssueSeverity.Error, kw.LineNo,
                            "CONTROL keyword is missing its data line.",
                            "Add a line: Cname  Cgain  Xhinge  XYZhvecX  XYZhvecY  XYZhvecZ  SgnDup"))
                    Else
                        Dim ctoks = Tokens(content(i).Text)
                        Dim cnum = ctoks.Skip(1).Count(Function(t) IsNum(t))
                        If ctoks.Length < 7 OrElse cnum < 6 Then
                            issues.Add(New ValidationIssue(IssueSeverity.Error, content(i).LineNo,
                                $"CONTROL line needs a name followed by 6 numeric values (Cgain, Xhinge, XYZhvec x/y/z, SgnDup); found {ctoks.Length} token(s).",
                                "Format: Cname  Cgain  Xhinge  XYZhvecX  XYZhvecY  XYZhvecZ  SgnDup, e.g. 'elevator  1.0  0.6  0.0 1.0 0.0  1.0'"))
                        End If
                        i += 1
                    End If
                Case "NACA"
                    Dim kw = content(i) : i += 1
                    If i >= content.Count OrElse Not Regex.IsMatch(content(i).Text.Trim(), "^\d{4,5}$") Then
                        issues.Add(New ValidationIssue(IssueSeverity.Warning, kw.LineNo,
                            "NACA keyword should be followed by a 4- or 5-digit code.",
                            "Example: NACA then '2412' on the next line."))
                    Else
                        i += 1
                    End If
                Case "AFILE", "AFIL"
                    Dim kw = content(i) : i += 1
                    If i >= content.Count Then
                        issues.Add(New ValidationIssue(IssueSeverity.Error, kw.LineNo,
                            "AFILE keyword is missing its filename.",
                            "Add the airfoil coordinate filename on the next line."))
                    Else
                        Dim fname = StripQuotes(content(i).Text)
                        If Not String.IsNullOrEmpty(projectDir) AndAlso Not File.Exists(Path.Combine(projectDir, fname)) Then
                            issues.Add(New ValidationIssue(IssueSeverity.Warning, content(i).LineNo,
                                $"Airfoil file not found: '{fname}'.",
                                $"Make sure '{fname}' exists alongside the project's .avl file, or check for a typo."))
                        End If
                        i += 1
                    End If
                Case "AIRFOIL"
                    i += 1
                    ' The coordinate table is free-form (x y pairs, optionally an "1 1" xle/xte
                    ' fraction line first) - skip lines that aren't a recognized keyword.
                    While i < content.Count AndAlso Not IsKnownAvlKeyword(KeyOf(content(i).Text))
                        i += 1
                    End While
                Case "CLAF"
                    i += 1 : CheckNumericLine(content, i, 1, "CLAF value", issues) : i += 1
                Case "CDCL"
                    i += 1 : CheckNumericLine(content, i, 6, "CDCL  CL1 CD1 CL2 CD2 CL3 CD3", issues) : i += 1
                Case "DESIGN"
                    Dim kw = content(i) : i += 1
                    If i >= content.Count Then
                        issues.Add(New ValidationIssue(IssueSeverity.Error, kw.LineNo,
                            "DESIGN keyword is missing its data line.",
                            "Add a line: DName  Wdes (design parameter name and local weight)."))
                    Else
                        Dim dtoks = Tokens(content(i).Text)
                        If dtoks.Length < 2 OrElse Not IsNum(dtoks(1)) Then
                            issues.Add(New ValidationIssue(IssueSeverity.Error, content(i).LineNo,
                                "DESIGN line needs a name followed by one numeric weight.",
                                "Format: DName  Wdes, e.g. 'twist1  -0.5'."))
                        End If
                        i += 1
                    End If
                Case Else
                    issues.Add(New ValidationIssue(IssueSeverity.Warning, content(i).LineNo,
                        $"Unrecognized line inside a SECTION: '{content(i).Text.Trim()}'.",
                        "Expected CONTROL, NACA, AFILE, AIRFOIL, CLAF, CDCL, DESIGN, or the start of the next SECTION/SURFACE/BODY."))
                    i += 1
            End Select
        End While
    End Sub

    Private Sub ParseBodyBlock(content As List(Of ContentLine), ByRef i As Integer, issues As List(Of ValidationIssue),
                                projectDir As String, iYsymValue As Integer?)
        Dim bodyLine = content(i) : i += 1
        If i >= content.Count Then
            issues.Add(New ValidationIssue(IssueSeverity.Error, bodyLine.LineNo,
                "BODY keyword is missing its name and Nbody/BSpace lines.",
                "Add a name line, then a line with Nbody and BSpace."))
            Return
        End If
        Dim bodyName = content(i).Text.Trim() : i += 1
        CheckNumericLine(content, i, 2, "Nbody  BSpace", issues)
        CheckSpacingRange(content, i, 1, "Bspace", issues)
        i += 1

        While i < content.Count
            Dim key = KeyOf(content(i).Text)
            Select Case key
                Case "SURFACE", "BODY"
                    Exit While
                Case "BFILE"
                    Dim kw = content(i) : i += 1
                    If i >= content.Count Then
                        issues.Add(New ValidationIssue(IssueSeverity.Error, kw.LineNo,
                            "BFILE keyword is missing its filename.",
                            "Add the body coordinate filename on the next line."))
                    Else
                        Dim fname = StripQuotes(content(i).Text)
                        If Not String.IsNullOrEmpty(projectDir) AndAlso Not File.Exists(Path.Combine(projectDir, fname)) Then
                            issues.Add(New ValidationIssue(IssueSeverity.Warning, content(i).LineNo,
                                $"Body file not found: '{fname}'.",
                                $"Make sure '{fname}' exists alongside the project's .avl file, or check for a typo."))
                        End If
                        i += 1
                    End If
                Case "YDUPLICATE"
                    Dim kw = content(i) : i += 1
                    CheckNumericLine(content, i, 1, "YDUPLICATE value (Ydupl)", issues)
                    If iYsymValue.HasValue AndAlso iYsymValue.Value <> 0 Then
                        issues.Add(New ValidationIssue(IssueSeverity.Error, kw.LineNo,
                            $"YDUPLICATE is used on body '{bodyName}' but IYsym is {iYsymValue.Value} (not 0).",
                            "YDUPLICATE can only be used when IYsym = 0 in the header - otherwise the duplicated body coincides with AVL's own Y-symmetry image and will almost certainly cause an arithmetic fault."))
                    End If
                    i += 1
                Case "SCALE"
                    i += 1 : CheckNumericLine(content, i, 3, "SCALE  Xscale  Yscale  Zscale", issues) : i += 1
                Case "TRANSLATE"
                    i += 1 : CheckNumericLine(content, i, 3, "TRANSLATE  dX  dY  dZ", issues) : i += 1
                Case Else
                    issues.Add(New ValidationIssue(IssueSeverity.Warning, content(i).LineNo,
                        $"Unrecognized line inside body '{bodyName}': '{content(i).Text.Trim()}'.",
                        "Expected BFILE, YDUPLICATE, SCALE, TRANSLATE, or the start of the next SURFACE/BODY."))
                    i += 1
            End Select
        End While
    End Sub

#End Region

#Region ".mass validation"

    Public Function ValidateMass(text As String) As List(Of ValidationIssue)
        Dim issues As New List(Of ValidationIssue)
        Dim lines = text.Replace(vbCr, "").Split(vbLf)
        Dim sawDataRow = False

        For li = 0 To lines.Length - 1
            Dim lineNo = li + 1
            Dim line = StripInlineComment(lines(li)).Trim()
            If line.Length = 0 OrElse line.StartsWith("#") Then Continue For

            ' avl_doc.txt:1264-1267: a line starting with "*" sets multipliers, "+" sets added
            ' constants, applied to all subsequent data rows - these are not mass points themselves.
            If line.StartsWith("*") OrElse line.StartsWith("+") Then
                Dim mtoks = Tokens(line.Substring(1))
                If mtoks.Length = 0 OrElse Not mtoks.All(Function(t) IsNum(t)) Then
                    issues.Add(New ValidationIssue(IssueSeverity.Error, lineNo,
                        $"'{line(0)}' multiplier/adder row has non-numeric values.",
                        "A '*' row sets multipliers and a '+' row sets added constants for all following data rows - every value after the prefix character must be numeric."))
                End If
                Continue For
            End If

            Dim eqIdx = line.IndexOf("="c)
            If eqIdx >= 0 AndAlso Not sawDataRow Then
                ' Header assignment line: Lunit/Munit/Tunit/g/rho = <number> [unit text]
                Dim key = line.Substring(0, eqIdx).Trim()
                Dim firstTok = Tokens(line.Substring(eqIdx + 1)).FirstOrDefault()
                If firstTok Is Nothing OrElse Not IsNum(firstTok) Then
                    issues.Add(New ValidationIssue(IssueSeverity.Error, lineNo,
                        $"'{key}' should be followed by a numeric value.",
                        "Example: 'Lunit = 0.0254 meters' or 'g = 9.81'."))
                End If
                Continue For
            End If

            ' Otherwise: a mass-point data row.
            sawDataRow = True
            Dim toks = Tokens(line)
            Dim numericPrefixCount = 0
            For Each t In toks
                If IsNum(t) Then
                    numericPrefixCount += 1
                Else
                    Exit For
                End If
            Next

            ' avl_doc.txt:1269-1278: mass/x/y/z are the only required fields - "the inertia values on
            ' each line are optional, and any ones which are absent will be assumed to be zero."
            If numericPrefixCount < 4 Then
                issues.Add(New ValidationIssue(IssueSeverity.Error, lineNo,
                    $"Mass row has only {numericPrefixCount} numeric value(s) before the first non-numeric token; AVL requires at least 4 (mass, x, y, z).",
                    "Format: mass  x  y  z  [Ixx  Iyy  Izz  Ixz  Ixy  Iyz]  [component name]. Inertia terms are optional and default to 0 if omitted."))
            ElseIf IsNum(toks(0)) AndAlso ParseNum(toks(0)) <= 0 Then
                issues.Add(New ValidationIssue(IssueSeverity.Warning, lineNo,
                    $"Mass value is {toks(0)} (zero or negative).",
                    "A mass point normally needs a positive mass; zero/negative mass can produce nonsensical inertia results."))
            End If
        Next

        If Not sawDataRow Then
            issues.Add(New ValidationIssue(IssueSeverity.Info, 0, "No mass point rows found.",
                "Add at least one row: mass  x  y  z"))
        End If

        Return issues
    End Function

#End Region

#Region ".run validation"

    Public Function ValidateRun(text As String) As List(Of ValidationIssue)
        Dim issues As New List(Of ValidationIssue)
        Dim lines = text.Replace(vbCr, "").Split(vbLf)
        Dim sawRunCase = False
        ' avl_doc.txt:1522: "A constraint can be used no more than once." - tracked per run case,
        ' reset every time a new "Run case N:" header is seen.
        Dim seenConstraintTargets As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        For li = 0 To lines.Length - 1
            Dim lineNo = li + 1
            Dim trimmed = lines(li).Trim()
            If trimmed.Length = 0 Then Continue For
            If Regex.IsMatch(trimmed, "^-+$") Then Continue For ' divider lines of dashes
            If trimmed.StartsWith("#") OrElse trimmed.StartsWith("!") Then Continue For ' full-line comment

            If Regex.IsMatch(trimmed, "^Run\s*[Cc]ase\s+\d+", RegexOptions.IgnoreCase) Then
                sawRunCase = True
                seenConstraintTargets.Clear()
                Continue For
            End If

            If trimmed.Contains("->") Then
                Dim arrowParts = trimmed.Split(New String() {"->"}, StringSplitOptions.None)
                If arrowParts.Length <> 2 Then
                    issues.Add(New ValidationIssue(IssueSeverity.Error, lineNo,
                        "Malformed constraint line (expected exactly one '->').",
                        "Format: '<variable>  ->  <target> = <value>', e.g. 'alpha  ->  alpha = 5.0'."))
                    Continue For
                End If
                Dim rhs = arrowParts(1)
                Dim eq = rhs.IndexOf("="c)
                If eq < 0 Then
                    issues.Add(New ValidationIssue(IssueSeverity.Error, lineNo,
                        "Constraint line is missing '=' after '->'.",
                        "Format: '<variable>  ->  <target> = <value>'."))
                    Continue For
                End If
                Dim targetName = rhs.Substring(0, eq).Trim()
                Dim firstTok = Tokens(rhs.Substring(eq + 1)).FirstOrDefault()
                If firstTok Is Nothing OrElse Not IsNum(firstTok) Then
                    issues.Add(New ValidationIssue(IssueSeverity.Error, lineNo,
                        $"Constraint value is not numeric: '{rhs.Substring(eq + 1).Trim()}'.",
                        "The value after '=' must be a number, e.g. 'alpha  ->  alpha = 5.0'."))
                ElseIf targetName.Length > 0 AndAlso Not seenConstraintTargets.Add(targetName) Then
                    issues.Add(New ValidationIssue(IssueSeverity.Error, lineNo,
                        $"Constraint target '{targetName}' is used more than once in this run case.",
                        "AVL does not allow the same constraint (the name after '->') to be used for more than one variable in the same run case - each quantity (e.g. CL, alpha, elevator) can only be pinned once."))
                End If
            ElseIf trimmed.Contains("="c) Then
                Dim eq = trimmed.IndexOf("="c)
                Dim firstTok = Tokens(trimmed.Substring(eq + 1)).FirstOrDefault()
                If firstTok Is Nothing OrElse Not IsNum(firstTok) Then
                    issues.Add(New ValidationIssue(IssueSeverity.Error, lineNo,
                        $"'{trimmed.Substring(0, eq).Trim()}' is not followed by a numeric value.",
                        "Parameter lines need a numeric value after '=', e.g. 'CL        =   0.40000'."))
                End If
            End If
            ' Anything else (case title lines, section labels) is free text - not checked.
        Next

        If Not sawRunCase Then
            issues.Add(New ValidationIssue(IssueSeverity.Info, 0, "No 'Run case N:' header found.",
                "A .run file normally starts each case with a line like ' Run case  1:   my case name'."))
        End If

        Return issues
    End Function

#End Region

End Module
