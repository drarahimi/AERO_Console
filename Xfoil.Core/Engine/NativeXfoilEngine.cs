// In-process XFOIL engine: drives the XfoilSession (no external xfoil.exe), reading
// airfoil .dat files from a working directory on disk. Output is delivered
// synchronously through the Output event, mirroring the text a user would see from
// the real xfoil.exe console. Mirrors Avl.Core's NativeAvlEngine.

using Xfoil.Core.Cli;

namespace Xfoil.Core.Engine;

public sealed class NativeXfoilEngine : IXfoilEngine
{
    private sealed class DiskFileSystem : IVirtualFileSystem
    {
        private readonly string _workingDir;
        public DiskFileSystem(string workingDir) => _workingDir = workingDir;

        public string? ReadFile(string name)
        {
            foreach (var candidate in Candidates(name))
                if (System.IO.File.Exists(candidate)) return System.IO.File.ReadAllText(candidate);
            return null;
        }

        public void WriteFile(string name, string contents)
        {
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
    private XfoilSession? _session;

    public string Name => "XFOIL (native)";
    public event Action<string>? Output;

    /// <summary>Raised when the session recognizes an OPER plot command (CPX/VPLO/...); the host
    /// draws its own modern plot. Re-raised from the current session so hosts subscribe once.</summary>
    public event Action<XfoilPlotKind>? PlotRequested;

    public bool IsRunning => _session != null && !_session.Quit;

    /// <summary>Creates an engine that resolves relative file names against
    /// <paramref name="workingDirectory"/> (typically the app's data folder where
    /// .dat airfoil files live).</summary>
    public NativeXfoilEngine(string workingDirectory) : this(new DiskFileSystem(workingDirectory)) { }

    /// <summary>Creates an engine over a custom file system (e.g. for testing).</summary>
    public NativeXfoilEngine(IVirtualFileSystem fileSystem) => _fs = fileSystem;

    /// <summary>The live session, exposed so the host can read the loaded buffer
    /// airfoil for a native geometry plot. Null until Start().</summary>
    public XfoilSession? Session => _session;

    public void Start()
    {
        _session = new XfoilSession(_fs);
        _session.PlotRequested += k => PlotRequested?.Invoke(k);
        // Evaluate Start()/Feed() into a local FIRST -- Output?.Invoke(_session.Feed(x)) would skip
        // running Feed entirely when Output has no subscribers (null-conditional short-circuit).
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
