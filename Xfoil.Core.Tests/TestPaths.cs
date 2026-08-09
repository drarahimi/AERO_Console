using System.IO;

namespace Xfoil.Core.Tests;

/// <summary>Resolves fixture directories relative to the repo root (found by walking up
/// from the test assembly until AERO_Console.sln appears). Mirrors Avl.Core.Tests.</summary>
public static class TestPaths
{
    private static readonly Lazy<string> _repoRoot = new(FindRepoRoot);
    public static string RepoRoot => _repoRoot.Value;

    /// <summary>Xfoil.Core.Tests/golden -- reference airfoils/transcripts captured from
    /// the bundled XFOIL6.99/xfoil.exe.</summary>
    public static string GoldenDir => Path.Combine(RepoRoot, "Xfoil.Core.Tests", "golden");

    public static string ReadGolden(string fileName) => File.ReadAllText(Path.Combine(GoldenDir, fileName));

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AERO_Console.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate AERO_Console.sln from " + AppContext.BaseDirectory);
    }
}
