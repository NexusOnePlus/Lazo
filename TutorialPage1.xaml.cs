using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Lazo
{
    public partial class TutorialPage1 : Page
    {
        public TutorialPage1()
        {
            InitializeComponent();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new TutorialPage2());
        }
    }
}