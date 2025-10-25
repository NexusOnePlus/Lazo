
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Point = System.Windows.Point;

namespace Lazo
{
    public partial class LazoWindow : Window
    {
        private static LazoWindow? _instance;

        // --- UI Elements ---
        private readonly Line _line;
        private readonly Ellipse _endCircle;
        private readonly Border _previewContainer;
        private readonly TextBlock _timePreview;

        // --- State and Configuration ---
        private const double PIXELS_PER_MINUTE = 80.0;
        private double _dpiScaleX = 1.0;
        private double _dpiScaleY = 1.0;
        private readonly Point _startPoint;
        private Point _lastMousePosition;
        private double _retractProgress = 0;

        public static void ShowLazo()
        {
            _instance?.Close();
            _instance = new LazoWindow();
            _instance.Show();
            
            // Ensure the static reference is cleared when the window closes
            _instance.Closed += (s, e) => {
                if (_instance == s)
                    _instance = null;
            };
        }

        public LazoWindow()
        {
            InitializeComponent();
            InitializeWindow();
            CalculateDpiScale();

            _startPoint = GetMousePosition();
            _lastMousePosition = _startPoint;

            _line = CreateLazoLine();
            _endCircle = CreateLazoCircle();
            _timePreview = CreateTimePreviewText();
            _previewContainer = CreatePreviewContainer(_timePreview);

            CanvasRoot.Children.Add(_line);
            CanvasRoot.Children.Add(_endCircle);
            CanvasRoot.Children.Add(_previewContainer);

            RegisterEventHandlers();
            UpdateVisuals(_startPoint);
        }

        #region Initialization Methods

        private void InitializeWindow()
        {
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;
            Left = 0; Top = 0;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            WindowStyle = WindowStyle.None;
            Topmost = true;
        }

        private void CalculateDpiScale()
        {
            try
            {
                double screenWidthDIU = SystemParameters.PrimaryScreenWidth;
                var primaryScreen = System.Windows.Forms.Screen.PrimaryScreen;
                int screenWidthPixels = primaryScreen?.Bounds.Width ?? 1920;
                _dpiScaleX = screenWidthPixels / screenWidthDIU;

                double screenHeightDIU = SystemParameters.PrimaryScreenHeight;
                int screenHeightPixels = primaryScreen?.Bounds.Height ?? 1080;
                _dpiScaleY = screenHeightPixels / screenHeightDIU;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ERROR] Could not calculate DPI scale: {ex.Message}");
                _dpiScaleX = 1.0; _dpiScaleY = 1.0;
            }
            Debug.WriteLine($"[INIT] DPI Scale=({_dpiScaleX:F2}, {_dpiScaleY:F2})");
        }

        private void RegisterEventHandlers()
        {
            CompositionTarget.Rendering += OnTrackingFrame;
            MouseLeftButtonUp += OnMouseUp;
        }

        #endregion

        #region UI Creation Methods

        private Line CreateLazoLine()
        {
            var goldGradient = new LinearGradientBrush((Color)ColorConverter.ConvertFromString("#FFD27A"), (Color)ColorConverter.ConvertFromString("#FFFCE6"), new Point(0, 0.5), new Point(1, 0.5));
            var goldGlow = new DropShadowEffect { Color = (Color)ColorConverter.ConvertFromString("#FFD27A"), BlurRadius = 14, ShadowDepth = 0, Opacity = 0.8 };

            return new Line
            {
                Stroke = goldGradient,
                StrokeThickness = 4,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                X1 = _startPoint.X,
                Y1 = _startPoint.Y,
                X2 = _startPoint.X,
                Y2 = _startPoint.Y,
                Effect = goldGlow
            };
        }

        private Ellipse CreateLazoCircle()
        {
            var goldGradient = new LinearGradientBrush((Color)ColorConverter.ConvertFromString("#FFD27A"), (Color)ColorConverter.ConvertFromString("#FFFCE6"), new Point(0, 0.5), new Point(1, 0.5));
            return new Ellipse
            {
                Width = 16,
                Height = 16,
                Fill = goldGradient,
                Stroke = Brushes.White,
                StrokeThickness = 1.0,
                Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 10, Opacity = 0.3 }
            };
        }

        private TextBlock CreateTimePreviewText() => new() { Foreground = Brushes.White, FontSize = 14, FontWeight = FontWeights.SemiBold, Text = "0.5 min" };

        private Border CreatePreviewContainer(TextBlock child)
        {
            return new Border
            {
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(Color.FromArgb(200, 20, 20, 20)),
                Padding = new Thickness(10, 5, 10, 5),
                Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 8, Opacity = 0.5 },
                Child = child
            };
        }

        #endregion

        #region Event Handlers & Core Logic

        private Point GetMousePosition()
        {
            var pos = System.Windows.Forms.Control.MousePosition;
            return new Point(pos.X / _dpiScaleX, pos.Y / _dpiScaleY);
        }

        private void OnTrackingFrame(object? sender, EventArgs e)
        {
            _lastMousePosition = GetMousePosition();
            UpdateVisuals(_lastMousePosition);
        }

        private void OnRetractFrame(object? sender, EventArgs e)
        {
            _retractProgress = Math.Min(1.0, _retractProgress + 0.08);
            double x = _lastMousePosition.X + (_startPoint.X - _lastMousePosition.X) * _retractProgress;
            double y = _lastMousePosition.Y + (_startPoint.Y - _lastMousePosition.Y) * _retractProgress;
            UpdateVisuals(new Point(x, y));
            _previewContainer.Opacity = 1.0 - _retractProgress;

            if (_retractProgress >= 1.0)
            {
                CompositionTarget.Rendering -= OnRetractFrame;
                Close();
            }
        }

        private void UpdateVisuals(Point currentPoint)
        {
            _line.X2 = currentPoint.X;
            _line.Y2 = currentPoint.Y;
            Canvas.SetLeft(_endCircle, currentPoint.X - _endCircle.Width / 2);
            Canvas.SetTop(_endCircle, currentPoint.Y - _endCircle.Height / 2);
            Canvas.SetLeft(_previewContainer, currentPoint.X + 20);
            Canvas.SetTop(_previewContainer, currentPoint.Y + 20);

            double distance = (_startPoint - currentPoint).Length;
            double rawMinutes = distance / PIXELS_PER_MINUTE;
            double minutes = Math.Max(0.5, Math.Round(rawMinutes * 2) / 2.0);
            _timePreview.Text = $"{minutes:F1} min";
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            CompositionTarget.Rendering -= OnTrackingFrame;
            CompositionTarget.Rendering += OnRetractFrame;

            double distance = (_startPoint - _lastMousePosition).Length;
            double rawMinutes = distance / PIXELS_PER_MINUTE;
            double minutes = Math.Max(0.5, Math.Round(rawMinutes * 2) / 2.0);

            Debug.WriteLine($"[CLICK] Distance={distance:0.0} → {minutes:F1} min");
            TimerPopup.Show(minutes);
        }

        #endregion

        protected override void OnClosed(EventArgs e)
        {
            // Unregister event handlers to prevent memory leaks
            CompositionTarget.Rendering -= OnTrackingFrame;
            CompositionTarget.Rendering -= OnRetractFrame;
            MouseLeftButtonUp -= OnMouseUp;
            
            // Clear static reference when this instance is closed
            if (_instance == this)
                _instance = null;
                
            base.OnClosed(e);
        }
    }
}