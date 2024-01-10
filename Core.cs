using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using Avalonia.Platform;
using Microsoft.Win32;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ShellLink;

namespace AutodrawInstaller;

public class Core
{
    public static bool IncludedZip = true; // The Zip needs to be included for the Windows Store version
    public static string InstallPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoDraw");
    
    public static async void Install(int type = 0, bool update = false, string tempPath = "")
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
        
        var shortcut = Shortcut.CreateShortcut(Path.Combine(InstallPath, "Autodraw", "autodraw.exe"));
        shortcut.WriteToFile(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
            "AutoDraw.lnk"));
        
        if (Environment.GetCommandLineArgs()[0].EndsWith(".exe"))
        {
            var sourcePath = Environment.GetCommandLineArgs()[0];
            var destinationPath = Path.Combine(InstallPath, "Autodraw", "autodrawmgmt.exe");
            File.Copy(sourcePath, destinationPath); 
        }
        
        // Register as an app
        var appPath = Path.Combine(InstallPath, "Autodraw");
        RegistryKey uninstallKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true);

        RegistryKey programKey = uninstallKey.CreateSubKey("MyProgram");

        programKey.SetValue("DisplayName", "AutoDraw");
        programKey.SetValue("DisplayIcon", Path.Combine(appPath, "autodraw.exe"));
        if (Environment.GetCommandLineArgs()[0].Contains(".exe"))
        {
            programKey.SetValue("ModifyPath", Path.Combine(appPath, "autodrawmgmt.exe"));
            programKey.SetValue("UninstallString", Path.Combine(appPath, "autodrawmgmt.exe") + " /U");
        }
        programKey.SetValue("Publisher", "AlexDalas and Siydge");
        programKey.SetValue("URLInfoAbout", "https://www.auto-draw.com");

        programKey.Close();
        uninstallKey.Close();
        
        if (type == 0) Process.Start(Path.Combine(InstallPath, "Autodraw", "autodraw.exe"));
    }
}