using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace AERO_Console.My
{
    // The following events are available for MyApplication:
    // Startup: Raised when the application starts, before the startup form is created.
    // Shutdown: Raised after all application forms are closed.  This event is not raised if the application terminates abnormally.
    // UnhandledException: Raised if the application encounters an unhandled exception.
    // StartupNextInstance: Raised when launching a single-instance application and the application is already active. 
    // NetworkAvailabilityChanged: Raised when the network connection is connected or disconnected.
    internal partial class MyApplication
    {
        private void MyApplication_StartupNextInstance(object sender, Microsoft.VisualBasic.ApplicationServices.StartupNextInstanceEventArgs e)
        {
            var main = MyProject.Forms.frmMain;
            if (main is not null)
            {
                if (main.WindowState == FormWindowState.Minimized)
                {
                    main.WindowState = FormWindowState.Normal;
                }
                main.Activate();
            }
        }

        private void AppStart()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssemblies;
        }

        private void MyApplication_Shutdown(object sender, EventArgs e)
        {
            // MsgBox(My.Settings.appUpdated & "|" & My.Settings.appUpdateNeeded)
            if (MySettingsProperty.Settings.appUpdateNeeded)
            {
                MySettingsProperty.Settings.appUpdateNeeded = false;
                MySettingsProperty.Settings.Save();
                Process.Start(MyProject.Forms.frmMain.updatedpath);
                // MsgBox("frmMain.updatedpath")
            }
            if (MySettingsProperty.Settings.appUpdated)
            {
                MySettingsProperty.Settings.appUpdated = false;
                MySettingsProperty.Settings.Save();
                Process.Start(MyProject.Forms.frmMain.originalpath);
                // MsgBox("frmMain.originalpath")
            }
        }

        private System.Reflection.Assembly ResolveAssemblies(object sender, ResolveEventArgs e)
        {
            var desiredAssembly = new System.Reflection.AssemblyName(e.Name);
            Debug.WriteLine("New name: " + desiredAssembly.Name);
            if (desiredAssembly.Name == "FastColoredTextBox")
            {
                return System.Reflection.Assembly.Load(Resources.Resources.FastColoredTextBox); // replace with your assembly's resource name
            }
            else
            {
                return null;
            }
        }
    }
}