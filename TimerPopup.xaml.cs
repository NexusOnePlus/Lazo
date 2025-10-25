using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace Lazo
{
    public partial class TimerPopup : Window
    {
        private static TimerPopup? _currentInstance;
        private readonly DispatcherTimer _textUpdateTimer;
        private readonly DispatcherTimer _hoverTimerShow;
        private readonly DispatcherTimer _hoverTimerHide;
        private readonly double _totalSeconds;
        private readonly double _initialPhase;
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private double _pathTotalLength;
        private bool _isHoveredUp = false;
        private bool _hoverAnimating = false;
        private readonly double _originalTop;

        #region Win32 Interop
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hwnd, int index, int style);
        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hwnd, int index);
        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var currentStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, currentStyle | WS_EX_TOOLWINDOW);
        }
        #endregion

        public static void Show(double minutes)
        {
            _currentInstance?.Close();
            _currentInstance = new TimerPopup(minutes);
            _currentInstance.Show();
            
            _currentInstance.Closed += (s, e) => {
                if (_currentInstance == s)
                    _currentInstance = null;
            };
        }

        public TimerPopup(double minutes)
        {
            InitializeComponent();
            _totalSeconds = Math.Max(0.5 * 60, minutes * 60.0);
            _initialPhase = Math.Min(Math.Max(0.1, _totalSeconds * 0.01), 0.8);

            _textUpdateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _textUpdateTimer.Tick += TextUpdateTimer_Tick;

            _hoverTimerShow = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
            _hoverTimerShow.Tick += OnHoverShow_Tick;

            _hoverTimerHide = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(2500) };
            _hoverTimerHide.Tick += OnHoverHide_Tick;

            Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
            Top = 10;
            _originalTop = Top;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var goldGradient = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
            goldGradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#FFD27A"), 0.0));
            goldGradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#FFDDA0"), 0.5));
            goldGradient.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString("#FFFCE6"), 1.0));
            ProgressPath.Stroke = goldGradient;

            RunIntroAnimation();
            UpdateTimeText(_totalSeconds);
        }

        private void Window_ContentRendered(object? sender, EventArgs e) => CreateProgressPath();

        #region Animations
        private void RunIntroAnimation()
        {
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260)) { EasingFunction = new QuadraticEase() });
            if (CenterFill.RenderTransform is ScaleTransform st)
            {
                var scaleAnim = new DoubleAnimation(0, 1.0, TimeSpan.FromSeconds(_initialPhase)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
                st.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
            }
            var startPathTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_initialPhase) };
            startPathTimer.Tick += (s, ev) =>
            {
                startPathTimer.Stop();
                CenterFill.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(160)));
                double remainingForPath = Math.Max(0.0, _totalSeconds - _initialPhase);
                StartProgressAnimation(remainingForPath);
            };
            startPathTimer.Start();
        }

        private void StartProgressAnimation(double durationSeconds)
        {
            if (durationSeconds <= 0.01) { OnTimerCompleted(); return; }
            var pathAnimation = new DoubleAnimation { From = _pathTotalLength, To = 0, Duration = TimeSpan.FromSeconds(durationSeconds * 3) };
            pathAnimation.Completed += (s, e) => OnTimerCompleted();
            ProgressPath.BeginAnimation(Shape.StrokeDashOffsetProperty, pathAnimation);
            _stopwatch.Restart();
            _textUpdateTimer.Start();
        }

        private void OnTimerCompleted()
        {
            _textUpdateTimer.Stop();
            var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300));
            fadeOut.Completed += (s, a) => Close();
            BeginAnimation(OpacityProperty, fadeOut);
        }
        #endregion

        #region Path Creation
        private void CreateProgressPath()
        {
            var parentUIElement = ProgressPath.Parent as UIElement ?? this;
            var topLeft = MainBorder.TranslatePoint(new Point(0, 0), parentUIElement);
            double w = MainBorder.ActualWidth, h = MainBorder.ActualHeight;
            double r = MainBorder.CornerRadius.TopLeft;
            var topCenter = new Point(topLeft.X + w / 2, topLeft.Y);
            var topRightLineStart = new Point(topLeft.X + w - r, topLeft.Y);
            var topRightArcEnd = new Point(topLeft.X + w, topLeft.Y + r);
            var rightLineEnd = new Point(topLeft.X + w, topLeft.Y + h - r);
            var bottomRightArcEnd = new Point(topLeft.X + w - r, topLeft.Y + h);
            var bottomLineEnd = new Point(topLeft.X + r, topLeft.Y + h);
            var bottomLeftArcEnd = new Point(topLeft.X, topLeft.Y + h - r);
            var leftLineEnd = new Point(topLeft.X, topLeft.Y + r);
            var topLeftArcEnd = new Point(topLeft.X + r, topLeft.Y);
            var figure = new PathFigure { StartPoint = topCenter, IsClosed = true };
            figure.Segments.Add(new LineSegment(topRightLineStart, true));
            figure.Segments.Add(new ArcSegment(topRightArcEnd, new Size(r, r), 0, false, SweepDirection.Clockwise, true));
            figure.Segments.Add(new LineSegment(rightLineEnd, true));
            figure.Segments.Add(new ArcSegment(bottomRightArcEnd, new Size(r, r), 0, false, SweepDirection.Clockwise, true));
            figure.Segments.Add(new LineSegment(bottomLineEnd, true));
            figure.Segments.Add(new ArcSegment(bottomLeftArcEnd, new Size(r, r), 0, false, SweepDirection.Clockwise, true));
            figure.Segments.Add(new LineSegment(leftLineEnd, true));
            figure.Segments.Add(new ArcSegment(topLeftArcEnd, new Size(r, r), 0, false, SweepDirection.Clockwise, true));
            var geometry = new PathGeometry { Figures = { figure } };
            ProgressPath.Data = geometry;
            ProgressBgPath.Data = geometry;
            _pathTotalLength = CalculatePathLength(geometry.GetFlattenedPathGeometry());

            ProgressPath.StrokeDashArray = new DoubleCollection { _pathTotalLength, _pathTotalLength };
            ProgressPath.StrokeDashOffset = _pathTotalLength;

            ProgressBgPath.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            ProgressBgPath.StrokeDashArray = new DoubleCollection { _pathTotalLength, _pathTotalLength };
            ProgressBgPath.StrokeDashOffset = 0;

            Debug.WriteLine($"[PathCreated] Total path length: {_pathTotalLength:F2}");
        }

        private double CalculatePathLength(PathGeometry flatGeometry)
        {
            double totalLength = 0.0;
            foreach (var figure in flatGeometry.Figures)
            {
                Point currentPoint = figure.StartPoint;
                foreach (var segment in figure.Segments)
                {
                    if (segment is PolyLineSegment p) { foreach (var point in p.Points) { totalLength += (point - currentPoint).Length; currentPoint = point; } }
                    else if (segment is LineSegment l) { totalLength += (l.Point - currentPoint).Length; currentPoint = l.Point; }
                }
            }
            return totalLength;
        }
        #endregion

        #region Text & Hover Logic
        private void TextUpdateTimer_Tick(object? sender, EventArgs e)
        {
            double elapsed = _stopwatch.Elapsed.TotalSeconds;
            double totalElapsed = _initialPhase + elapsed;
            double remainingSeconds = Math.Max(0, _totalSeconds - totalElapsed);
            UpdateTimeText(remainingSeconds);
        }

        private void UpdateTimeText(double remainingSeconds)
        {
            int m = (int)(remainingSeconds / 60);
            int s = (int)(remainingSeconds % 60);
            MinutesRun.Text = m.ToString();
            SecondsRun.Text = s.ToString("00");
        }

        private void Window_MouseEnter(object s, MouseEventArgs e)
        {
            _hoverTimerHide.Stop();
            _hoverTimerShow.Start();
        }

        private void Window_MouseLeave(object s, MouseEventArgs e)
        {
            _hoverTimerShow.Stop();
            _hoverTimerHide.Start();
        }

        private void OnHoverShow_Tick(object? sender, EventArgs e)
        {
            _hoverTimerShow.Stop();
            if (IsMouseOver) AnimateSlide(true);
        }



        private void OnHoverHide_Tick(object? sender, EventArgs e)
        {
            _hoverTimerHide.Stop();
            if (!IsMouseOver) AnimateSlide(false);
        }

        private void AnimateSlide(bool isSlidingUp)
        {
            if (_hoverAnimating || isSlidingUp == _isHoveredUp) return;
            _hoverAnimating = true;
            _isHoveredUp = isSlidingUp;
            double targetTop = isSlidingUp ? -this.Height : _originalTop;
            var anim = new DoubleAnimation(targetTop, TimeSpan.FromMilliseconds(260)) { EasingFunction = new QuadraticEase() };
            anim.Completed += (_, __) => _hoverAnimating = false;
            BeginAnimation(TopProperty, anim);
        }
        #endregion

        protected override void OnClosed(EventArgs e)
        {
            _textUpdateTimer?.Stop();
            _hoverTimerShow?.Stop();
            _hoverTimerHide?.Stop();
            
            _stopwatch?.Stop();
            
            if (_textUpdateTimer != null)
                _textUpdateTimer.Tick -= TextUpdateTimer_Tick;
            if (_hoverTimerShow != null)
                _hoverTimerShow.Tick -= OnHoverShow_Tick;
            if (_hoverTimerHide != null)
                _hoverTimerHide.Tick -= OnHoverHide_Tick;
            
            if (_currentInstance == this)
                _currentInstance = null;
                
            base.OnClosed(e);
        }
    }
}