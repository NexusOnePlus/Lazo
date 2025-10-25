using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using MessageBox = System.Windows.MessageBox;

namespace Lazo
{
    public partial class TutorialPage2 : Page
    {
        public TutorialPage2()
        {
            InitializeComponent();
            Unloaded += TutorialPage2_Unloaded;
        }

        private void TutorialVideo_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "1.mp4");

                if (File.Exists(videoPath))
                {
                    TutorialVideo.Source = new Uri(videoPath, UriKind.Absolute);
                    TutorialVideo.Play();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Video not found: {videoPath}");
                    MessageBox.Show($"Could't find video:\n{videoPath}",
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cannot load video: {ex.Message}");
                MessageBox.Show($"Error unable to load video: {ex.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TutorialVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            TutorialVideo.Position = TimeSpan.Zero;
            TutorialVideo.Play();
        }

        private void TutorialVideo_MediaFailed(object? sender, ExceptionRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Error MediaElement: {e.ErrorException?.Message}");
            MessageBox.Show($"Cannot reproduce video:\n{e.ErrorException?.Message}",
                          "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            StopVideo();
            NavigationService.Navigate(new TutorialPage3());
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            StopVideo();
            NavigationService.GoBack();
        }

        private void TutorialPage2_Unloaded(object sender, RoutedEventArgs e)
        {
            StopVideo();
        }

        private void StopVideo()
        {
            if (TutorialVideo != null)
            {
                TutorialVideo.MediaEnded -= TutorialVideo_MediaEnded;
                TutorialVideo.MediaFailed -= TutorialVideo_MediaFailed;
                
                TutorialVideo.Stop();
                TutorialVideo.Close();
                TutorialVideo.Source = null;
            }
        }
    }
}