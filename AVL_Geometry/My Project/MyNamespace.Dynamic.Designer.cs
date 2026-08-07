using System;
using System.ComponentModel;
using System.Diagnostics;

namespace AERO_Console.My
{
    internal static partial class MyProject
    {
        internal partial class MyForms
        {

            [EditorBrowsable(EditorBrowsableState.Never)]
            public frmAbout m_frmAbout;

            public frmAbout frmAbout
            {
                [DebuggerHidden]
                get
                {
                    m_frmAbout = Create__Instance__(m_frmAbout);
                    return m_frmAbout;
                }
                [DebuggerHidden]
                set
                {
                    if (ReferenceEquals(value, m_frmAbout))
                        return;
                    if (value is not null)
                        throw new ArgumentException("Property can only be set to Nothing");
                    Dispose__Instance__(ref m_frmAbout);
                }
            }


            [EditorBrowsable(EditorBrowsableState.Never)]
            public frmGeometry m_frmGeometry;

            public frmGeometry frmGeometry
            {
                [DebuggerHidden]
                get
                {
                    m_frmGeometry = Create__Instance__(m_frmGeometry);
                    return m_frmGeometry;
                }
                [DebuggerHidden]
                set
                {
                    if (ReferenceEquals(value, m_frmGeometry))
                        return;
                    if (value is not null)
                        throw new ArgumentException("Property can only be set to Nothing");
                    Dispose__Instance__(ref m_frmGeometry);
                }
            }


            [EditorBrowsable(EditorBrowsableState.Never)]
            public frmHelp m_frmHelp;

            public frmHelp frmHelp
            {
                [DebuggerHidden]
                get
                {
                    m_frmHelp = Create__Instance__(m_frmHelp);
                    return m_frmHelp;
                }
                [DebuggerHidden]
                set
                {
                    if (ReferenceEquals(value, m_frmHelp))
                        return;
                    if (value is not null)
                        throw new ArgumentException("Property can only be set to Nothing");
                    Dispose__Instance__(ref m_frmHelp);
                }
            }


            [EditorBrowsable(EditorBrowsableState.Never)]
            public frmInfo m_frmInfo;

            public frmInfo frmInfo
            {
                [DebuggerHidden]
                get
                {
                    m_frmInfo = Create__Instance__(m_frmInfo);
                    return m_frmInfo;
                }
                [DebuggerHidden]
                set
                {
                    if (ReferenceEquals(value, m_frmInfo))
                        return;
                    if (value is not null)
                        throw new ArgumentException("Property can only be set to Nothing");
                    Dispose__Instance__(ref m_frmInfo);
                }
            }


            [EditorBrowsable(EditorBrowsableState.Never)]
            public frmMain m_frmMain;

            public frmMain frmMain
            {
                [DebuggerHidden]
                get
                {
                    m_frmMain = Create__Instance__(m_frmMain);
                    return m_frmMain;
                }
                [DebuggerHidden]
                set
                {
                    if (ReferenceEquals(value, m_frmMain))
                        return;
                    if (value is not null)
                        throw new ArgumentException("Property can only be set to Nothing");
                    Dispose__Instance__(ref m_frmMain);
                }
            }


            [EditorBrowsable(EditorBrowsableState.Never)]
            public frmUpdate m_frmUpdate;

            public frmUpdate frmUpdate
            {
                [DebuggerHidden]
                get
                {
                    m_frmUpdate = Create__Instance__(m_frmUpdate);
                    return m_frmUpdate;
                }
                [DebuggerHidden]
                set
                {
                    if (ReferenceEquals(value, m_frmUpdate))
                        return;
                    if (value is not null)
                        throw new ArgumentException("Property can only be set to Nothing");
                    Dispose__Instance__(ref m_frmUpdate);
                }
            }


            [EditorBrowsable(EditorBrowsableState.Never)]
            public frmXfoilAnalysis m_frmXfoilAnalysis;

            public frmXfoilAnalysis frmXfoilAnalysis
            {
                [DebuggerHidden]
                get
                {
                    m_frmXfoilAnalysis = Create__Instance__(m_frmXfoilAnalysis);
                    return m_frmXfoilAnalysis;
                }
                [DebuggerHidden]
                set
                {
                    if (ReferenceEquals(value, m_frmXfoilAnalysis))
                        return;
                    if (value is not null)
                        throw new ArgumentException("Property can only be set to Nothing");
                    Dispose__Instance__(ref m_frmXfoilAnalysis);
                }
            }

        }


    }
}