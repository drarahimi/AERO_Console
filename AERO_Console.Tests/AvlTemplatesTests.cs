using AERO_Console;
using Xunit;

namespace AERO_Console.Tests
{
    /// <summary>
    /// Sanity checks that the hardcoded editor starter templates stay well-formed: they should be
    /// non-empty and, crucially, should pass FileValidator without errors - a template that the
    /// app's own validator flags would be an embarrassing thing to hand a learning user.
    /// </summary>
    public class AvlTemplatesTests
    {
        [Fact]
        public void AllTemplates_AreNonEmpty()
        {
            Assert.False(string.IsNullOrWhiteSpace(AvlTemplates.AvlTemplateFull));
            Assert.False(string.IsNullOrWhiteSpace(AvlTemplates.AvlTemplateMinimal));
            Assert.False(string.IsNullOrWhiteSpace(AvlTemplates.SurfaceTemplateFull));
            Assert.False(string.IsNullOrWhiteSpace(AvlTemplates.SurfaceTemplateMinimal));
            Assert.False(string.IsNullOrWhiteSpace(AvlTemplates.SectionTemplateFull));
            Assert.False(string.IsNullOrWhiteSpace(AvlTemplates.SectionTemplateMinimal));
            Assert.False(string.IsNullOrWhiteSpace(AvlTemplates.ControlTemplateFull));
            Assert.False(string.IsNullOrWhiteSpace(AvlTemplates.ControlTemplateMinimal));
            Assert.False(string.IsNullOrWhiteSpace(AvlTemplates.MassTemplateFull));
            Assert.False(string.IsNullOrWhiteSpace(AvlTemplates.MassTemplateMinimal));
            Assert.False(string.IsNullOrWhiteSpace(AvlTemplates.RunTemplateFull));
            Assert.False(string.IsNullOrWhiteSpace(AvlTemplates.RunTemplateMinimal));
        }

        [Theory]
        [InlineData("Lunit")]
        [InlineData("Munit")]
        [InlineData("g =")]
        [InlineData("rho")]
        public void MassTemplate_ContainsExpectedHeaderKeys(string key)
        {
            Assert.Contains(key, AvlTemplates.MassTemplateFull);
            Assert.Contains(key, AvlTemplates.MassTemplateMinimal);
        }

        [Fact]
        public void RunTemplate_ContainsRunCaseHeader()
        {
            Assert.Contains("Run case", AvlTemplates.RunTemplateFull);
            Assert.Contains("Run case", AvlTemplates.RunTemplateMinimal);
        }

        [Fact]
        public void MassTemplates_PassMassValidatorWithoutErrors()
        {
            Assert.DoesNotContain(
                FileValidator.ValidateMass(AvlTemplates.MassTemplateFull),
                i => i.Severity == IssueSeverity.Error);
            Assert.DoesNotContain(
                FileValidator.ValidateMass(AvlTemplates.MassTemplateMinimal),
                i => i.Severity == IssueSeverity.Error);
        }

        [Fact]
        public void RunTemplates_PassRunValidatorWithoutErrors()
        {
            Assert.DoesNotContain(
                FileValidator.ValidateRun(AvlTemplates.RunTemplateFull),
                i => i.Severity == IssueSeverity.Error);
            Assert.DoesNotContain(
                FileValidator.ValidateRun(AvlTemplates.RunTemplateMinimal),
                i => i.Severity == IssueSeverity.Error);
        }
    }
}
