using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Microsoft.Win32;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ShellLink;

namespace AutodrawInstaller;

public partial class MainWindow : Window
{
    public static bool IncludedZip = true; // The Zip needs to be included for the Windows Store version
    public static string InstallPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoDraw");
    public MainWindow()
    {
        // Check if the application is already running with administrative privileges
        if (!IsAdmin())
        {
            // If not, relaunch the application with administrative privileges
            RelaunchWithAdminPrivileges();
            return;
        }
        string[] args = Environment.GetCommandLineArgs();
        InitializeComponent();
        if (args.Contains("/S"))
        {
            Close();
            Install(1);
        }
        else
        {
            if (Path.Exists(Path.Combine(InstallPath, "Autodraw")))
            {
                Title.Content = "AutoDraw Manager";
                InstallButton.Content = "Repair Install";
                if (!IncludedZip) InstallButton.IsVisible = false;
                UninstallButton.IsVisible = true;
                UpdateButton.IsVisible = true;
                UpdateButton.Click += (_, _) => Install(0, true);
                UninstallButton.Click += (_, _) => Uninstall();
            }
            InstallButton.Click += (_, _) => Install(0);
            CloseAppButton.Click += QuitAppOnClick;
            MinimizeAppButton.Click += MinimizeAppOnClick;
        }
    }
    private void MinimizeAppOnClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void QuitAppOnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
    
    static bool IsAdmin()
    {
        // Check if the current process is running with administrative privileges
        var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        var principal = new System.Security.Principal.WindowsPrincipal(identity);
        return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }

    public async void Install(int type = 0, bool update = false, string tempPath = "")
    {
        // Install app to AutoDraw Local Path
        if (!update && IncludedZip)
        {
            Directory.CreateDirectory(InstallPath);
            tempPath = Path.Combine(Path.GetTempPath(), "AutoDraw.zip");
            using (var stream = AssetLoader.Open(new Uri("avares://AutodrawInstaller/Assets/AutoDraw.zip")))
            {
                using (var fileStream = File.Create(tempPath))
                {
                    stream.CopyTo(fileStream);
                }
            }
        }
        else
        {
            var DownloadPath = "https://github.com/auto-draw/autodraw/releases/latest/download/AutoDraw.zip";
            Directory.CreateDirectory(InstallPath);
            tempPath = Path.Combine(Path.GetTempPath(), "AutoDraw.zip");
            using (var client = new HttpClient())
            {
                using (var result = await client.GetAsync(DownloadPath))
                {
                    if (result.IsSuccessStatusCode)
                    {
                        using (var stream = await result.Content.ReadAsStreamAsync())
                        using (var fileStream = File.Create(tempPath))
                        {
                            stream.CopyTo(fileStream);
                        }
                    }
                }
            }
        }
        ZipFile.ExtractToDirectory(tempPath, InstallPath, true);
        File.Delete(tempPath);
        // Register as an app
        var appPath = Path.Combine(InstallPath, "Autodraw");
        RegistryKey uninstallKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true);

        RegistryKey programKey = uninstallKey.CreateSubKey("MyProgram");

        programKey.SetValue("DisplayName", "AutoDraw");
        programKey.SetValue("DisplayIcon", Path.Combine(appPath, "autodraw.exe"));
        programKey.SetValue("UninstallString", Path.Combine(appPath, "autodrawmgmt.exe"));
        programKey.SetValue("Publisher", "AlexDalas and Siydge");
        programKey.SetValue("URLInfoAbout", "https://www.auto-draw.com");

        programKey.Close();
        uninstallKey.Close();
        
        var shortcut = Shortcut.CreateShortcut(Path.Combine(InstallPath, "Autodraw", "autodraw.exe"));
        shortcut.WriteToFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "AutoDraw.lnk"));

        if (type == 0)
        {
            Process.Start(Path.Combine(InstallPath, "Autodraw", "autodraw.exe"));
        }
        Close();
        
    }
    static void RelaunchWithAdminPrivileges()
    {
        // Get the path of the current executable
        string exePath = Process.GetCurrentProcess().MainModule.FileName;

        // Create a new process start info
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Verb = "runas",  
            UseShellExecute = true,
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
    
    public async void Uninstall()
    {
        var box = MessageBoxManager.GetMessageBoxStandard("AutoDraw Uninstaller", "Would you like to remove your locally stored data?",
            ButtonEnum.YesNoCancel).ShowAsync();
        var result = await box;
        switch (result)
        {
            case ButtonResult.Cancel:
                return;
            case ButtonResult.Yes:
                new DirectoryInfo(Path.Combine(InstallPath)).Delete(true);
                break;
            case ButtonResult.No:
                new DirectoryInfo(Path.Combine(InstallPath, "Autodraw")).Delete(true);
                break;
        }
        File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "AutoDraw.lnk"));
        await MessageBoxManager.GetMessageBoxStandard("Uninstalled", "AutoDraw has been uninstalled.").ShowAsync();
        Close();
    }
}