using System.IO;
using Avl.Core.Cli;
using Xunit;

namespace Avl.Core.Tests;

/// <summary>Full-transcript CLI dispatch tests for AvlSession -- replays the scripted
/// command sequences used to capture the golden avl.exe transcripts and diffs the
/// FT block. Ports packages/avl-core/test/cli/avlSession.test.ts.</summary>
public class AvlSessionTests
{
    private sealed class RunsFs : IVirtualFileSystem
    {
        public string? ReadFile(string name)
        {
            var p = Path.Combine(TestPaths.RunsDir, name);
            return File.Exists(p) ? File.ReadAllText(p) : null;
        }
    }

    /// <summary>Serves the real runs/ files plus one crafted .run whose control count
    /// doesn't match the geometry (5 aero + only 1 control instead of 2).</summary>
    private sealed class MismatchRunFs : IVirtualFileSystem
    {
        private const string CraftedRun =
            " ---------------------------------------------\n" +
            " Run case  1:   mismatch\n\n" +
            " alpha        ->  alpha       =   1.00000    \n" +
            " beta         ->  beta        =   0.00000    \n" +
            " pb/2V        ->  pb/2V       =   0.00000    \n" +
            " qc/2V        ->  qc/2V       =   0.00000    \n" +
            " rb/2V        ->  rb/2V       =   0.00000    \n" +
            " elevator     ->  elevator    =   0.00000    \n";
        public string? ReadFile(string name)
        {
            if (name == "mismatch.run") return CraftedRun;
            var p = Path.Combine(TestPaths.RunsDir, name);
            return File.Exists(p) ? File.ReadAllText(p) : null;
        }
    }

    [Fact]
    public void Cli_CaseWithMismatchedControlCount_StillExecutes()
    {
        // allegro has 2 controls; the .run only constrains 1. AVL adapts -- the missing
        // control constraint is padded, and X must not throw (regression: it did).
        var session = new AvlSession(new MismatchRunFs());
        session.Start();
        string @out = "";
        foreach (var cmd in new[] { "LOAD allegro.avl", "CASE mismatch.run", "OPER", "X", "FT", "", "QUIT" })
        {
            if (session.Quit) break;
            @out += session.Feed(cmd);
        }
        Assert.Contains("Vortex Lattice Output -- Total Forces", @out);
        Assert.DoesNotContain("Trim did not converge", @out);
    }

    private static string RunScript(params string[] commands)
    {
        var session = new AvlSession(new RunsFs());
        string @out = session.Start();
        foreach (var line in commands)
        {
            if (session.Quit) break;
            @out += session.Feed(line);
        }
        return @out;
    }

    /// <summary>Extracts the LAST "----...Vortex Lattice Output...----" block.</summary>
    private static string[] ExtractLastOutTotalBlock(string text)
    {
        var lines = GoldenText.SplitLines(text);
        int start = -1;
        for (int i = 0; i < lines.Length; i++) if (lines[i].Contains("Vortex Lattice Output")) start = i - 1;
        int end = start + 1;
        while (!lines[end].TrimStart().StartsWith("---")) end++;
        return lines.Skip(start).Take(end - start + 1).Select(l => l.TrimEnd()).ToArray();
    }

    private static void AssertFt(string goldenFile, string[] actualText)
    {
        var golden = ExtractLastOutTotalBlock(TestPaths.ReadGolden(goldenFile)).Select(GoldenText.NormalizeNegativeZero).ToArray();
        Assert.Equal(golden, actualText.Select(GoldenText.NormalizeNegativeZero).ToArray());
    }

    [Theory]
    [InlineData("allegro-ft-alpha0.txt", "LOAD allegro.avl|OPER|X|FT||QUIT")]
    [InlineData("allegro-ft-cl05.txt", "LOAD allegro.avl|OPER|A C 0.5|X|FT||QUIT")]
    [InlineData("allegro-ft-elev10.txt", "LOAD allegro.avl|OPER|D1 D1 10|X|FT||QUIT")]
    [InlineData("allegro-ft-elevtrim.txt", "LOAD allegro.avl|OPER|D1 PM 0|X|FT||QUIT")]
    [InlineData("b737-ft-alpha0.txt", "LOAD b737.avl|OPER|X|FT||QUIT")]
    public void Cli_FtBlock_MatchesReference(string goldenFile, string script)
    {
        var transcript = RunScript(script.Split('|'));
        AssertFt(goldenFile, ExtractLastOutTotalBlock(transcript));
    }

    [Fact]
    public void Cli_B737_MassMsetCaseTrim_MatchesReference()
    {
        var transcript = RunScript("LOAD b737.avl", "MASS b737.mass", "MSET 0", "CASE b737.run", "OPER", "X", "FT", "", "QUIT");
        AssertFt("b737-run-ft.txt", ExtractLastOutTotalBlock(transcript));
    }

    /// <summary>A display command given a filename writes its output to that file --
    /// this is exactly how the WinForms plot features drive AVL ("fs" then a temp
    /// filename), so the native engine must persist the block.</summary>
    private sealed class CapturingFs : IVirtualFileSystem
    {
        private readonly RunsFs _reader = new();
        public readonly Dictionary<string, string> Written = new();
        public string? ReadFile(string name) => _reader.ReadFile(name);
        public void WriteFile(string name, string contents) => Written[name] = contents;
    }

    [Fact]
    public void Cli_DisplayCommandWithFilename_WritesBlockToFile()
    {
        var fs = new CapturingFs();
        var session = new AvlSession(fs);
        session.Start();
        foreach (var cmd in new[] { "LOAD allegro.avl", "OPER", "X", "FS", "out.fs", "QUIT" })
        {
            if (session.Quit) break;
            session.Feed(cmd);
        }

        Assert.True(fs.Written.ContainsKey("out.fs"));
        Assert.Contains("Surface and Strip Forces by surface", fs.Written["out.fs"]);
        Assert.Contains("Strip Forces referred to Strip Area, Chord", fs.Written["out.fs"]);
    }
}
