using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
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
    public MainWindow()
    {
        InitializeComponent();
        string[] args = Environment.GetCommandLineArgs();
        if (Path.Exists(Path.Combine(Core.InstallPath, "Autodraw")))
        {
            Title.Content = "AutoDraw Manager";
            InstallButton.Content = "Repair Install";
            if (!Core.IncludedZip) InstallButton.IsVisible = false;
            UninstallButton.IsVisible = true;
            UpdateButton.IsVisible = true;
            UpdateButton.Click += (_, _) => { Core.Install(); Close(); };
            UninstallButton.Click += (_, _) => Uninstall();
        }

        InstallButton.Click += (_, _) => { Core.Install(); Close(); };
        CloseAppButton.Click += QuitAppOnClick;
        MinimizeAppButton.Click += MinimizeAppOnClick;
        if (args.Contains("/U")) Uninstall();

    }
    private void MinimizeAppOnClick(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void QuitAppOnClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    public async void Uninstall()
    {
        var box = MessageBoxManager.GetMessageBoxStandard("AutoDraw Uninstaller", "Would you like to remove your locally stored data?",
            ButtonEnum.YesNoCancel).ShowAsync();
        try
        {
            switch (await box)
            {
                case ButtonResult.Cancel:
                    return;
                case ButtonResult.Yes:
                    new DirectoryInfo(Path.Combine(Core.InstallPath)).Delete(true);
                    break;
                case ButtonResult.No:
                    new DirectoryInfo(Path.Combine(Core.InstallPath, "Autodraw")).Delete(true);
                    break;
            }
        }
        catch(IOException ex)
        {
            // File is in use
        }

        File.Delete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "AutoDraw.lnk"));
        await MessageBoxManager.GetMessageBoxStandard("Uninstalled", "AutoDraw has been uninstalled.").ShowAsync();
        Environment.Exit(0);
    }
}