using Avalonia;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AutodrawInstaller;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Check if the application is already running with administrative privileges
        if (!IsAdmin())
        {
            // If not, relaunch the application with administrative privileges
            RelaunchWithAdminPrivileges();
            return;
        }
        if (Environment.GetCommandLineArgs().Contains("/S"))
        {
            BuildAvaloniaApp().SetupWithoutStarting();
            Core.Install(1);
        }
        else BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    
    
    static bool IsAdmin()
    {
        // Check if the current process is running with administrative privileges
        var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }
    
    static void RelaunchWithAdminPrivileges()
    {
        // Get the path of the current executable
        string exePath = Process.GetCurrentProcess().MainModule.FileName;
        File.Copy(exePath, Path.Combine(Path.GetTempPath(), "autodrawmgmt.exe"), true);
        exePath = Path.Combine(Path.GetTempPath(), "autodrawmgmt.exe");
        // Create a new process start info
        string arg = "";
        if (Environment.GetCommandLineArgs().Contains("/S"))
        {
            arg = "/S";
        }
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Verb = "runas",  
            UseShellExecute = true,
            Arguments = arg
        };

        try
        {
            Process.Start(startInfo);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }

        Environment.Exit(0);
    }

}