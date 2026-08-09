// In-process AVL engine: drives the fully-ported AvlSession (no external
// avl.exe), reading LOAD/MASS/CASE/AFIL files from a working directory on disk.
// Output is delivered synchronously through the Output event, mirroring the
// text a user would see from the real avl.exe console.

using Avl.Core.Cli;

namespace Avl.Core.Engine;

public sealed class NativeAvlEngine : IAvlEngine
{
    private sealed class DiskFileSystem : IVirtualFileSystem
    {
        private readonly string _workingDir;
        public DiskFileSystem(string workingDir) => _workingDir = workingDir;

        public string? ReadFile(string name)
        {
            // Try the name as given, then relative to the working directory.
            foreach (var candidate in Candidates(name))
            {
                if (System.IO.File.Exists(candidate)) return System.IO.File.ReadAllText(candidate);
            }
            return null;
        }

        public void WriteFile(string name, string contents)
        {
            // Rooted paths (e.g. the plot features' temp files) write in place; bare
            // names go to the working directory. Match avl.exe's Windows CRLF output.
            string path = System.IO.Path.IsPathRooted(name) ? name : System.IO.Path.Combine(_workingDir, name);
            string crlf = contents.Replace("\r\n", "\n").Replace("\n", "\r\n");
            System.IO.File.WriteAllText(path, crlf);
        }

        private IEnumerable<string> Candidates(string name)
        {
            yield return name;
            yield return System.IO.Path.Combine(_workingDir, name);
        }
    }

    private readonly IVirtualFileSystem _fs;
    private AvlSession? _session;

    public string Name => "AVL (native)";
    public event Action<string>? Output;

    /// <summary>Raised when the session recognizes an OPER-menu plot command (G/T); the host
    /// draws its own modern plot in response. Re-raised from the current AvlSession so hosts can
    /// subscribe once on the engine and survive session restarts.</summary>
    public event Action<AvlPlotKind>? PlotRequested;

    public bool IsRunning => _session != null && !_session.Quit;

    /// <summary>Creates an engine that resolves relative file names against
    /// <paramref name="workingDirectory"/> (typically the app's data folder where
    /// .avl/.mass/.run/.dat files live).</summary>
    public NativeAvlEngine(string workingDirectory) : this(new DiskFileSystem(workingDirectory)) { }

    /// <summary>Creates an engine over a custom file system (e.g. for testing).</summary>
    public NativeAvlEngine(IVirtualFileSystem fileSystem) => _fs = fileSystem;

    /// <summary>The live session, exposed so the host can read parsed geometry /
    /// last result for a native geometry/loading plot panel. Null until Start().</summary>
    public AvlSession? Session => _session;

    public void Start()
    {
        _session = new AvlSession(_fs);
        _session.PlotRequested += k => PlotRequested?.Invoke(k);
        // NOTE: evaluate Start()/Feed() into a local FIRST. `Output?.Invoke(_session.Feed(x))`
        // would skip evaluating the argument entirely when Output has no subscribers (C#
        // null-conditional short-circuits the whole expression), silently dropping the command.
        string banner = _session.Start();
        Output?.Invoke(banner);
    }

    public void Send(string command)
    {
        if (_session == null) Start();
        if (_session!.Quit) return;
        string output = _session.Feed(command);
        Output?.Invoke(output);
    }

    public void Stop()
    {
        _session = null;
    }
}
