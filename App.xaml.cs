using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;
using MouseEventArgs = System.Windows.Forms.MouseEventArgs;
using Velopack;
using MessageBox = System.Windows.MessageBox;

namespace Lazo;

public partial class App : Application
{
    [STAThread]
    private static void Main(string[] args)
    {
        bool createdNew;
        using (var mutex = new System.Threading.Mutex(true, "IconizerAppMutex", out createdNew))
        {
            if (!createdNew)
            {
                MessageBox.Show("App already running.", "Unique instance", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                VelopackApp.Build().Run();

                var app = new App();
                app.InitializeComponent();
                app.Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fatal error on startup runtime: {ex.Message}", "Start failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private NotifyIcon? _trayIcon;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        InitializeTrayIcon();

        if (Settings1.Default.IsFirstLaunch || Settings1.Default.ShowStartupAnimation)
        {
            var welcome = new WelcomeWindow();
            welcome.Show();

            if (Settings1.Default.IsFirstLaunch)
            {
                Settings1.Default.IsFirstLaunch = false;
                Settings1.Default.Save();
            }
        }
    }

    private void InitializeTrayIcon()
    {
        try
        {
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Configuración", null, OnSettingsClick);
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Salir", null, OnExitClick);

            _trayIcon = new NotifyIcon()
            {
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                    System.Reflection.Assembly.GetExecutingAssembly().Location
                ),
                Visible = true,
                Text = "Lazo Timer",
                ContextMenuStrip = contextMenu
            };

            _trayIcon.MouseClick += OnTrayClick;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] Could not initialize Tray Icon: {ex.Message}");
        }
    }

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        var settings = new SettingsWindow();
        settings.Show();
        
    }

    private void OnExitClick(object? sender, EventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void OnTrayClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            LazoWindow.ShowLazo();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        base.OnExit(e);
    }
}