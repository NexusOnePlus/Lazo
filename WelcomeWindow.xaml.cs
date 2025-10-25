using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace Lazo
{
    public partial class WelcomeWindow : Window
    {
        private readonly Page page1 = new TutorialPage1();
        private readonly Page page2 = new TutorialPage2();
        private readonly Page page3 = new TutorialPage3();

        public WelcomeWindow()
        {
            InitializeComponent();
            NavigationFrame.Navigate(page1);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void PaginationDot_Click(object sender, RoutedEventArgs e)
        {
            if (sender == Page1Dot) NavigationFrame.Navigate(page1);
            else if (sender == Page2Dot) NavigationFrame.Navigate(page2);
            else if (sender == Page3Dot) NavigationFrame.Navigate(page3);
        }

        private void NavigationFrame_Navigated(object sender, NavigationEventArgs e)
        {
            Page1Dot.Tag = (e.Content == page1) ? "Active" : null;
            Page2Dot.Tag = (e.Content == page2) ? "Active" : null;
            Page3Dot.Tag = (e.Content == page3) ? "Active" : null;
        }

        protected override void OnClosed(EventArgs e)
        {
            NavigationFrame.Navigated -= NavigationFrame_Navigated;
            
            base.OnClosed(e);
        }
    }
}