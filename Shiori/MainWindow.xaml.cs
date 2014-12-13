using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading; // timer
using System.Windows.Threading; // dispather

namespace Shiori
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        private Timer timer;
        private int t = 0;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            timer = new Timer(MyTimerCallback, null, 0, 1000);
            
        }

        private void MyTimerCallback(object state)
        {
            t++;
            Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal, new Action(() =>
                {
                    myTimeLine.Value = t / 60.0;
                }
            ));
        }
    }
}
