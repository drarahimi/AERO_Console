using System.IO;

namespace Avl.Core.Tests;

/// <summary>Resolves the repository's fixture directories (the real avl3.52 runs and
/// the golden reference transcripts) relative to the repo root, located by walking
/// up from the test assembly until AERO_Console.sln is found. Mirrors the path
/// resolution in the TypeScript tests (packages/avl-core/test/**).</summary>
public static class TestPaths
{
    private static readonly Lazy<string> _repoRoot = new(FindRepoRoot);

    public static string RepoRoot => _repoRoot.Value;

    /// <summary>avl3.52/AVL3.52rel09032025/runs -- the real .avl / .mass / .run fixtures.</summary>
    public static string RunsDir => Path.Combine(RepoRoot, "avl3.52", "AVL3.52rel09032025", "runs");

    /// <summary>packages/avl-core/test/golden -- byte-for-byte reference output transcripts.</summary>
    public static string GoldenDir => Path.Combine(RepoRoot, "packages", "avl-core", "test", "golden");

    /// <summary>Avl.Core.Tests/golden -- reference transcripts captured directly from the
    /// bundled avl3.51-32.exe for features with no TypeScript predecessor (e.g. VM).</summary>
    public static string LocalGoldenDir => Path.Combine(RepoRoot, "Avl.Core.Tests", "golden");

    public static string ReadRun(string fileName) => File.ReadAllText(Path.Combine(RunsDir, fileName));

    public static string ReadGolden(string fileName) => File.ReadAllText(Path.Combine(GoldenDir, fileName));

    public static string ReadLocalGolden(string fileName) => File.ReadAllText(Path.Combine(LocalGoldenDir, fileName));

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
