using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Win32;
using Velopack;
using Velopack.Sources;
using MessageBox = System.Windows.MessageBox;

namespace Lazo
{
    public partial class SettingsWindow : Window
    {
        private const string APP_NAME = "Lazo";
        private const string REGISTRY_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
            Loaded += OnLoaded;
            DisplayVersion();
        }

        private void LoadSettings()
        {
            StartupCheckBox.IsChecked = IsStartupEnabled();
            AnimationCheckBox.IsChecked = Settings1.Default.ShowStartupAnimation;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            ChangelogText.Text = "Loading changelog...";
            ChangelogText.Text = await FetchChangelogAsync();
        }

        public void DisplayVersion()
        {
            var token = "";
            var source = new Velopack.Sources.GithubSource("https://github.com/NexusOnePlus/Lazo", token, true);
            var updateManager = new UpdateManager(source);
            string version;
            if (updateManager.IsInstalled)
            {
                version = updateManager.CurrentVersion?.ToString() ?? "Unknown";
            }
            else
            {
                version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Debug";
            }
            VersionText.Text = $"v{version}";
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            var token = "";
            var source = new Velopack.Sources.GithubSource("https://github.com/NexusOnePlus/Lazo", token, true);
            var updateManager = new UpdateManager(source);
            try
            {
                UpdateButton.IsEnabled = false;
                UpdateButtonText.Text = "Checking...";
                UpdateSpinner.Visibility = Visibility.Visible;

                if (!updateManager.IsInstalled)
                {
                    MessageBox.Show("Updates can only be checked in an installed version of the application.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                try
                {
                    var newVersion = await updateManager.CheckForUpdatesAsync();
                    if (newVersion == null)
                    {
                        MessageBox.Show("Your application is up to date.", "No Updates", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    UpdateButtonText.Text = "Downloading...";
                    try
                    {
                        await updateManager.DownloadUpdatesAsync(newVersion);
                    }
                    catch (Exception exDownload)
                    {
                        MessageBox.Show($"Error during download:\n{exDownload}", "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    try
                    {
                        updateManager.ApplyUpdatesAndRestart(newVersion);
                    }
                    catch (Exception exApply)
                    {
                        MessageBox.Show($"Error applying update:\n{exApply}", "Apply Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                catch (Exception exCheck)
                {
                    MessageBox.Show($"Error checking for updates:\n{exCheck}", "Check Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"General error in update process:\n{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                UpdateButton.IsEnabled = true;
                UpdateButtonText.Text = "Check for Updates";
                UpdateSpinner.Visibility = Visibility.Collapsed;
            }
        }

        private async Task<string> FetchChangelogAsync()
        {
            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.UserAgent.ParseAdd("Lazo-client");

                var json = await http.GetStringAsync("https://api.github.com/repos/NexusOnePlus/Lazo/releases");
                var releases = JsonSerializer.Deserialize<GitHubRelease[]>(json);

                if (releases != null && releases.Length > 0)
                {
                    return releases[0].body ?? "No changelog available.";
                }

                return "No releases found.";
            }
            catch (Exception ex)
            {
                return $"Error fetching changelog: {ex.Message}";
            }
        }

        public class GitHubRelease
        {
            public string? body { get; set; }
        }

        private void StartupCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            bool isChecked = StartupCheckBox.IsChecked == true;
            SetStartupEnabled(isChecked);
        }

        private void AnimationCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            Settings1.Default.ShowStartupAnimation = AnimationCheckBox.IsChecked == true;
            Settings1.Default.Save();
        }

        private void TutorialButton_Click(object sender, RoutedEventArgs e)
        {
            Settings1.Default.IsFirstLaunch = true;
            Settings1.Default.Save();

            var welcome = new WelcomeWindow();
            welcome.Show();

            this.Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        #region Startup Registry Methods

        private bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, false))
                {
                    if (key != null)
                    {
                        object? value = key.GetValue(APP_NAME);
                        return value != null;
                    }
                }
            }
            catch
            {
                //TODO: Log error
            }
            return false;
        }

        private void SetStartupEnabled(bool enable)
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                            exePath = exePath.Replace(".dll", ".exe");
                            key.SetValue(APP_NAME, $"\"{exePath}\"");
                        }
                        else
                        {
                            key.DeleteValue(APP_NAME, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo cambiar la configuración de inicio: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            Settings1.Default.Save();
            base.OnClosed(e);
        }
    }
}