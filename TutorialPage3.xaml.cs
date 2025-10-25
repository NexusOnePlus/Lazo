using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Lazo
{
    public partial class TutorialPage3 : Page
    {
        public TutorialPage3()
        {
            InitializeComponent();

            Loaded += TutorialPage3_Loaded;
            Unloaded += TutorialPage3_Unloaded;
        }

        private void TutorialPage3_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                string videoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "2.mp4");

                if (File.Exists(videoPath))
                {
                    TutorialVideo.Source = new Uri(videoPath, UriKind.Absolute);
                    TutorialVideo.MediaEnded += TutorialVideo_MediaEnded;
                    TutorialVideo.Play();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Video not found: {videoPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error unable to load video: {ex.Message}");
            }
        }

        private void TutorialVideo_MediaEnded(object sender, RoutedEventArgs e)
        {
            TutorialVideo.Position = TimeSpan.Zero;
            TutorialVideo.Play();
        }

        private void TutorialVideo_MediaFailed(object? sender, ExceptionRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Error en MediaElement: {e.ErrorException?.Message}");
        }

        private void TutorialPage3_Unloaded(object sender, RoutedEventArgs e)
        {
            StopVideo();
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            StopVideo();
            Window.GetWindow(this)?.Close();
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