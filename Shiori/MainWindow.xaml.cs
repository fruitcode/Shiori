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
using System.IO; // FileInfo
using libZPlay;
using Shiori.Lib;

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
        String currentFileName;
        Timer timer;

        KeyboardHook globalHotkeys;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            currentFileName = "F:\\gp.aac";

            player = new ZPlay();
            player.OpenFile(currentFileName, TStreamFormat.sfAutodetect);

            TStreamInfo i = new TStreamInfo();
            player.GetStreamInfo(ref i);
            currentDuration = i.Length.sec;

            ReadID3Info();

            if (!player.StartPlayback())
                Console.WriteLine("Unable to start playback: " + player.GetError());

            timer = new Timer(MyTimerCallback, null, 0, 500);

            myTimeLine.PositionChanged += myTimeLine_PositionChanged;

            globalHotkeys = new KeyboardHook();
            globalHotkeys.KeyPressed += GlobalHotKeyPressed;
            if ( !(globalHotkeys.RegisterHotKey(KeyModifier.Alt | KeyModifier.Control, Key.J) &&
                   globalHotkeys.RegisterHotKey(KeyModifier.Alt | KeyModifier.Control, Key.K) ))
            {
                MessageBox.Show("Unable to register global hotkeys", "Error");
            }
        }

        void GlobalHotKeyPressed(object sender, KeyPressedEventArgs e)
        {
            TStreamTime t;
            switch (e.Key)
            {
                case Key.J:
                    t = new TStreamTime() { sec = 5 };
                    player.Seek(TTimeFormat.tfSecond, ref t, TSeekMethod.smFromCurrentBackward);
                    break;
                case Key.K:
                    t = new TStreamTime() { sec = 10 };
                    player.Seek(TTimeFormat.tfSecond, ref t, TSeekMethod.smFromCurrentForward);
                    break;
                default:
                    break;
            }
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            globalHotkeys.Dispose();
        }

        void myTimeLine_PositionChanged(object sender, PositionChangedEventArgs e)
        {
            TStreamTime newPos = new TStreamTime() { sec = (uint)(currentDuration * e.NewValue) };
            player.Seek(TTimeFormat.tfSecond, ref newPos, TSeekMethod.smFromBeginning);
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

        private void ReadID3Info()
        {
            TID3Info i3 = new TID3Info();
            player.LoadID3(TID3Version.id3Version2, ref i3);
            

            Boolean b1 = false, b2 = false, b3 = false;
            if (i3.Artist != null && i3.Artist != "")
                b1 = true;
            if (i3.Album != null && i3.Album != "")
                b2 = true;
            if (i3.Title != null && i3.Title != "")
                b3 = true;

            if (b1 || b2 || b3)
            {
                InfoLabelArtist.Content = i3.Artist;
                InfoLabelAlbum.Content = i3.Album;
                InfoLabelTitle.Content = i3.Title;
            }
            else
            {
                InfoLabelArtist.Content = "-";
                InfoLabelAlbum.Content = (new FileInfo(currentFileName)).Name;
                InfoLabelTitle.Content = "-";
            }
        }
    }
}
