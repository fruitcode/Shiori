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
using libZPlay;

namespace Shiori
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
        ZPlay player;
        uint currentDuration;
        Timer timer;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            player = new ZPlay();
            player.OpenFile("F:\\tr\\fry\\fry1\\fry_1_02.mp3", TStreamFormat.sfAutodetect);

            TStreamInfo i = new TStreamInfo();
            player.GetStreamInfo(ref i);
            currentDuration = i.Length.sec;
            
            player.StartPlayback();

            timer = new Timer(MyTimerCallback, null, 0, 500);

            myTimeLine.PositionChanged += myTimeLine_PositionChanged;
        }

        void myTimeLine_PositionChanged(object sender, PositionChangedEventArgs e)
        {
            Console.WriteLine(e.NewValue);
        }

        private void MyTimerCallback(object state)
        {
            TStreamTime t = new TStreamTime();
            player.GetPosition(ref t);

            _dispatcher.BeginInvoke(
                DispatcherPriority.Normal, new Action(() =>
                {
                    myTimeLine.Value = t.sec / (double)currentDuration ;
                }
            ));
        }
    }
}
