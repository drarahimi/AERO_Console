// Console-engine abstraction mirroring Avl.Core's IAvlEngine: a line-in / text-out
// interface that both the in-process native port (NativeXfoilEngine, backed by
// XfoilSession) and the legacy external xfoil.exe wrapper can implement, so the
// WinForms host can switch between them without changing its command/output plumbing.

namespace Xfoil.Core.Engine;

public interface IXfoilEngine
{
    /// <summary>Human-readable engine name for status/UI (e.g. "XFOIL (native)").</summary>
    string Name { get; }

    /// <summary>Raised whenever the engine produces console text (banner, menus,
    /// command output). The host appends this verbatim to its console view.</summary>
    event Action<string>? Output;

    /// <summary>True once the engine is running and ready to accept commands.</summary>
    bool IsRunning { get; }

    /// <summary>Starts the engine and emits the startup banner + first prompt via Output.</summary>
    void Start();

    /// <summary>Feeds one command line (as if typed at the prompt).</summary>
    void Send(string command);

    /// <summary>Stops the engine and releases any resources.</summary>
    void Stop();
}
