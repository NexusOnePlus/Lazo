using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
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
        }

        private void LoadSettings()
        {
            StartupCheckBox.IsChecked = IsStartupEnabled();
            AnimationCheckBox.IsChecked = Settings1.Default.ShowStartupAnimation;
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
                // Si hay error al leer el registro, asumir que no está habilitado
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
                            // Agregar al inicio
                            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                            // Para .NET 5+ necesitas obtener el .exe correctamente
                            exePath = exePath.Replace(".dll", ".exe");
                            key.SetValue(APP_NAME, $"\"{exePath}\"");
                        }
                        else
                        {
                            // Remover del inicio
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