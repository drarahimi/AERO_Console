using System.Collections.Generic;
using System.Linq;
using AERO_Console;
using Xunit;

namespace AERO_Console.Tests
{
    /// <summary>
    /// Tests for <see cref="FileValidator"/>, the line-based structural checker for AVL's
    /// .avl/.mass/.run text formats. Focus is on the rules the validator documents explicitly:
    /// header numeric lines, IYsym/IZsym integer form, YDUPLICATE-vs-IYsym, spacing ranges,
    /// "at least 2 SECTIONs per surface", mass row required fields, and run-case constraint rules.
    /// </summary>
    public class FileValidatorTests
    {
        // --- helpers -------------------------------------------------------

        private static bool HasError(IEnumerable<ValidationIssue> issues) =>
            issues.Any(i => i.Severity == IssueSeverity.Error);

        private static bool MessageContains(IEnumerable<ValidationIssue> issues, string fragment) =>
            issues.Any(i => i.Message != null && i.Message.Contains(fragment));

        private static int CountBySeverity(IEnumerable<ValidationIssue> issues, IssueSeverity sev) =>
            issues.Count(i => i.Severity == sev);

        /// <summary>A minimal but fully valid .avl file: header + one surface with two sections.</summary>
        private const string ValidAvl =
            "My Plane\n" +
            "0.0\n" +
            "0   0   0.0\n" +
            "9.0   0.9   10.0\n" +
            "0.0   0.0   0.0\n" +
            "SURFACE\n" +
            "Wing\n" +
            "8   1.0\n" +
            "SECTION\n" +
            "0.0  0.0  0.0  1.0  0.0\n" +
            "NACA\n" +
            "2412\n" +
            "SECTION\n" +
            "0.0  5.0  0.0  0.6  0.0\n" +
            "NACA\n" +
            "2412\n";

        // --- dispatcher ----------------------------------------------------

        [Fact]
        public void ValidateFile_UnknownKind_ReturnsNoIssues()
        {
            var issues = FileValidator.ValidateFile("anything", "Bogus", null);
            Assert.Empty(issues);
        }

        [Fact]
        public void ValidateFile_NullKind_ReturnsNoIssues()
        {
            var issues = FileValidator.ValidateFile("anything", null, null);
            Assert.Empty(issues);
        }

        [Theory]
        [InlineData("Geometry")]
        [InlineData("Mass")]
        [InlineData("Run")]
        public void ValidateFile_KnownKinds_Dispatch(string kind)
        {
            // Each kind should at least run without throwing and return a list.
            var issues = FileValidator.ValidateFile("", kind, null);
            Assert.NotNull(issues);
        }

        // --- .avl: happy path ---------------------------------------------

        [Fact]
        public void ValidateAvl_ValidFile_HasNoErrors()
        {
            var issues = FileValidator.ValidateAvl(ValidAvl, null);
            Assert.False(HasError(issues), "A valid .avl file should produce no Error-severity issues.");
        }

        [Fact]
        public void ValidateAvl_ValidFile_ReportsCounts()
        {
            var issues = FileValidator.ValidateAvl(ValidAvl, null);
            Assert.Contains(issues, i =>
                i.Severity == IssueSeverity.Info &&
                i.Message.Contains("1 surface") &&
                i.Message.Contains("2 section"));
        }

        [Fact]
        public void ValidateAvl_EmptyFile_ReturnsInfoAndStops()
        {
            var issues = FileValidator.ValidateAvl("", null);
            Assert.Single(issues);
            Assert.Equal(IssueSeverity.Info, issues[0].Severity);
            Assert.Contains("empty", issues[0].Message);
        }

        // --- .avl: header lines -------------------------------------------

        [Fact]
        public void ValidateAvl_NonNumericMach_ReportsError()
        {
            string text = ValidAvl.Replace("0.0\n", "notanumber\n");
            var issues = FileValidator.ValidateAvl(text, null);
            Assert.True(MessageContains(issues, "Mach number"));
        }

        [Fact]
        public void ValidateAvl_IYsymAsDecimal_ReportsError()
        {
            string text = ValidAvl.Replace("0   0   0.0\n", "0.0   0   0.0\n");
            var issues = FileValidator.ValidateAvl(text, null);
            Assert.True(MessageContains(issues, "IYsym"));
            Assert.True(HasError(issues));
        }

        [Fact]
        public void ValidateAvl_IZsymAsDecimal_ReportsError()
        {
            string text = ValidAvl.Replace("0   0   0.0\n", "0   1.0   0.0\n");
            var issues = FileValidator.ValidateAvl(text, null);
            Assert.True(MessageContains(issues, "IZsym"));
        }

        [Fact]
        public void ValidateAvl_IYsymOutOfRange_ReportsWarning()
        {
            string text = ValidAvl.Replace("0   0   0.0\n", "5   0   0.0\n");
            var issues = FileValidator.ValidateAvl(text, null);
            Assert.Contains(issues, i => i.Severity == IssueSeverity.Warning && i.Message.Contains("IYsym"));
        }

        [Fact]
        public void ValidateAvl_NegativeSref_ReportsWarning()
        {
            string text = ValidAvl.Replace("9.0   0.9   10.0\n", "-9.0   0.9   10.0\n");
            var issues = FileValidator.ValidateAvl(text, null);
            Assert.Contains(issues, i => i.Severity == IssueSeverity.Warning && i.Message.Contains("Sref"));
        }

        // --- .avl: surface / section rules --------------------------------

        [Fact]
        public void ValidateAvl_SurfaceWithOneSection_ReportsError()
        {
            string text =
                "My Plane\n0.0\n0 0 0.0\n9.0 0.9 10.0\n0.0 0.0 0.0\n" +
                "SURFACE\nWing\n8 1.0\n" +
                "SECTION\n0.0 0.0 0.0 1.0 0.0\nNACA\n2412\n";
            var issues = FileValidator.ValidateAvl(text, null);
            Assert.Contains(issues, i =>
                i.Severity == IssueSeverity.Error && i.Message.Contains("only 1 SECTION"));
        }

        [Fact]
        public void ValidateAvl_NoSurface_ReportsError()
        {
            string text = "My Plane\n0.0\n0 0 0.0\n9.0 0.9 10.0\n0.0 0.0 0.0\n";
            var issues = FileValidator.ValidateAvl(text, null);
            Assert.Contains(issues, i =>
                i.Severity == IssueSeverity.Error && i.Message.Contains("No SURFACE"));
        }

        [Fact]
        public void ValidateAvl_SectionWithTooFewNumbers_ReportsError()
        {
            string text = ValidAvl.Replace("0.0  0.0  0.0  1.0  0.0\n", "0.0  0.0\n");
            var issues = FileValidator.ValidateAvl(text, null);
            Assert.Contains(issues, i =>
                i.Severity == IssueSeverity.Error && i.Message.Contains("SECTION data line"));
        }

        [Fact]
        public void ValidateAvl_CspaceOutOfRange_ReportsError()
        {
            string text = ValidAvl.Replace("8   1.0\n", "8   9.0\n");
            var issues = FileValidator.ValidateAvl(text, null);
            Assert.Contains(issues, i =>
                i.Severity == IssueSeverity.Error && i.Message.Contains("Cspace"));
        }

        [Fact]
        public void ValidateAvl_LargeNspan_ReportsMergedTokenWarning()
        {
            // "241.0" is the classic "24" + "1.0" merged-token symptom.
            string text = ValidAvl.Replace("8   1.0\n", "8   1.0   241.0\n");
            var issues = FileValidator.ValidateAvl(text, null);
            Assert.Contains(issues, i =>
                i.Severity == IssueSeverity.Warning && i.Message.Contains("Nspan"));
        }

        [Fact]
        public void ValidateAvl_YDuplicateWithNonZeroIYsym_ReportsError()
        {
            string text =
                "My Plane\n0.0\n1 0 0.0\n9.0 0.9 10.0\n0.0 0.0 0.0\n" +
                "SURFACE\nWing\n8 1.0\n" +
                "YDUPLICATE\n0.0\n" +
                "SECTION\n0.0 0.0 0.0 1.0 0.0\nNACA\n2412\n" +
                "SECTION\n0.0 5.0 0.0 0.6 0.0\nNACA\n2412\n";
            var issues = FileValidator.ValidateAvl(text, null);
            Assert.Contains(issues, i =>
                i.Severity == IssueSeverity.Error && i.Message.Contains("YDUPLICATE"));
        }

        [Fact]
        public void ValidateAvl_YDuplicateWithZeroIYsym_NoYDuplicateError()
        {
            string text =
                "My Plane\n0.0\n0 0 0.0\n9.0 0.9 10.0\n0.0 0.0 0.0\n" +
                "SURFACE\nWing\n8 1.0\n" +
                "YDUPLICATE\n0.0\n" +
                "SECTION\n0.0 0.0 0.0 1.0 0.0\nNACA\n2412\n" +
                "SECTION\n0.0 5.0 0.0 0.6 0.0\nNACA\n2412\n";
            var issues = FileValidator.ValidateAvl(text, null);
            Assert.DoesNotContain(issues, i =>
                i.Severity == IssueSeverity.Error && i.Message.Contains("YDUPLICATE"));
        }

        [Fact]
        public void ValidateAvl_NacaWithNonNumericCode_ReportsWarning()
        {
            // Only the first section's NACA code is corrupted (it is the one followed by SECTION).
            string text = ValidAvl.Replace("NACA\n2412\nSECTION", "NACA\nabcd\nSECTION");
            var issues = FileValidator.ValidateAvl(text, null);
            Assert.Contains(issues, i =>
                i.Severity == IssueSeverity.Warning && i.Message.Contains("NACA"));
        }

        [Fact]
        public void ValidateAvl_FourCharKeywordAbbreviation_Recognized()
        {
            // AVL matches keywords on their first 4 chars; "SURF" must be treated as SURFACE.
            string text =
                "My Plane\n0.0\n0 0 0.0\n9.0 0.9 10.0\n0.0 0.0 0.0\n" +
                "SURF\nWing\n8 1.0\n" +
                "SECTION\n0.0 0.0 0.0 1.0 0.0\nNACA\n2412\n" +
                "SECTION\n0.0 5.0 0.0 0.6 0.0\nNACA\n2412\n";
            var issues = FileValidator.ValidateAvl(text, null);
            Assert.Contains(issues, i => i.Severity == IssueSeverity.Info && i.Message.Contains("1 surface"));
        }

        // --- .avl: comments & blank lines are ignored ---------------------

        [Fact]
        public void ValidateAvl_CommentsAndBlankLines_Ignored()
        {
            string text =
                "My Plane\n" +
                "# a comment\n\n" +
                "0.0\n" +
                "0 0 0.0\n" +
                "9.0 0.9 10.0\n" +
                "0.0 0.0 0.0\n" +
                "SURFACE\nWing\n8 1.0\n" +
                "SECTION\n0.0 0.0 0.0 1.0 0.0\nNACA\n2412\n" +
                "SECTION\n0.0 5.0 0.0 0.6 0.0\nNACA\n2412\n";
            var issues = FileValidator.ValidateAvl(text, null);
            Assert.False(HasError(issues));
        }

        // --- .mass ---------------------------------------------------------

        [Fact]
        public void ValidateMass_ValidFile_HasNoErrors()
        {
            string text =
                "Lunit = 1.0 m\nMunit = 1.0 kg\nTunit = 1.0 s\n" +
                "g = 9.81\nrho = 1.225\n" +
                "1.0  0.0  0.0  0.0  0 0 0\n";
            var issues = FileValidator.ValidateMass(text);
            Assert.False(HasError(issues));
        }

        [Fact]
        public void ValidateMass_RowWithTooFewNumbers_ReportsError()
        {
            string text = "1.0  0.0  0.0  wing\n"; // only 3 numeric before non-numeric
            var issues = FileValidator.ValidateMass(text);
            Assert.Contains(issues, i =>
                i.Severity == IssueSeverity.Error && i.Message.Contains("Mass row"));
        }

        [Fact]
        public void ValidateMass_NegativeMass_ReportsWarning()
        {
            string text = "-1.0  0.0  0.0  0.0\n";
            var issues = FileValidator.ValidateMass(text);
            Assert.Contains(issues, i =>
                i.Severity == IssueSeverity.Warning && i.Message.Contains("Mass value"));
        }

        [Fact]
        public void ValidateMass_NonNumericHeaderAssignment_ReportsError()
        {
            string text = "Lunit = abc\n1.0 0.0 0.0 0.0\n";
            var issues = FileValidator.ValidateMass(text);
            Assert.Contains(issues, i =>
                i.Severity == IssueSeverity.Error && i.Message.Contains("Lunit"));
        }

        [Fact]
        public void ValidateMass_MultiplierRowWithNonNumeric_ReportsError()
        {
            string text = "* 2.0 x\n1.0 0.0 0.0 0.0\n";
            var issues = FileValidator.ValidateMass(text);
            Assert.Contains(issues, i =>
                i.Severity == IssueSeverity.Error && i.Message.Contains("multiplier"));
        }

        [Fact]
        public void ValidateMass_NoDataRows_ReportsInfo()
        {
            string text = "Lunit = 1.0 m\ng = 9.81\n";
            var issues = FileValidator.ValidateMass(text);
            Assert.Contains(issues, i =>
                i.Severity == IssueSeverity.Info && i.Message.Contains("No mass point"));
        }

        // --- .run ----------------------------------------------------------

        [Fact]
        public void ValidateRun_ValidFile_HasNoErrors()
        {
            string text =
                " Run case  1:  cruise\n" +
                " alpha  ->  alpha = 5.0\n" +
                " beta   ->  beta  = 0.0\n" +
                " CL     =   0.4\n";
            var issues = FileValidator.ValidateRun(text);
            Assert.False(HasError(issues));
        }

        [Fact]
        public void ValidateRun_NoRunCaseHeader_ReportsInfo()
        {
            string text = " alpha  ->  alpha = 5.0\n";
            var issues = FileValidator.ValidateRun(text);
            Assert.Contains(issues, i =>
                i.Severity == IssueSeverity.Info && i.Message.Contains("Run case"));
        }

        [Fact]
        public void ValidateRun_DuplicateConstraintTarget_ReportsError()
        {
            string text =
                " Run case  1:  cruise\n" +
                " alpha  ->  CL = 0.4\n" +
                " beta   ->  CL = 0.2\n";
            var issues = FileValidator.ValidateRun(text);
            Assert.Contains(issues, i =>
                i.Severity == IssueSeverity.Error && i.Message.Contains("used more than once"));
        }

        [Fact]
        public void ValidateRun_DuplicateTargetAcrossCases_NoError()
        {
            // The seen-set resets on each "Run case N:" header, so CL pinned once per case is fine.
            string text =
                " Run case  1:  a\n alpha  ->  CL = 0.4\n" +
                " Run case  2:  b\n alpha  ->  CL = 0.2\n";
            var issues = FileValidator.ValidateRun(text);
            Assert.DoesNotContain(issues, i =>
                i.Severity == IssueSeverity.Error && i.Message.Contains("used more than once"));
        }

        [Fact]
        public void ValidateRun_ConstraintMissingEquals_ReportsError()
        {
            string text = " Run case  1:  cruise\n alpha  ->  alpha\n";
            var issues = FileValidator.ValidateRun(text);
            Assert.Contains(issues, i =>
                i.Severity == IssueSeverity.Error && i.Message.Contains("missing '='"));
        }

        [Fact]
        public void ValidateRun_NonNumericConstraintValue_ReportsError()
        {
            string text = " Run case  1:  cruise\n alpha  ->  alpha = abc\n";
            var issues = FileValidator.ValidateRun(text);
            Assert.Contains(issues, i =>
                i.Severity == IssueSeverity.Error && i.Message.Contains("not numeric"));
        }

        [Fact]
        public void ValidateRun_NonNumericParameterValue_ReportsError()
        {
            string text = " Run case  1:  cruise\n alpha -> alpha = 1.0\n CL = notanumber\n";
            var issues = FileValidator.ValidateRun(text);
            Assert.Contains(issues, i =>
                i.Severity == IssueSeverity.Error && i.Message.Contains("numeric value"));
        }

        [Fact]
        public void ValidateRun_DashDividersAndComments_Ignored()
        {
            string text =
                " ---------------------------------------------\n" +
                " Run case  1:  cruise\n" +
                " ! a comment\n" +
                " alpha  ->  alpha = 5.0\n";
            var issues = FileValidator.ValidateRun(text);
            Assert.False(HasError(issues));
        }
    }
}
