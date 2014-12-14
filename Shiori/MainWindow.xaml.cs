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
        //uint currentDuration;
        String currentFileName;
        Timer timer;

        AudioMetadata metadata;
        List<Border> bookmarksDashes;

        KeyboardHook globalHotkeys;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
            this.SizeChanged += MainWindow_SizeChanged;
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            currentFileName = "F:\\fry.mp3";
            bookmarksDashes = new List<Border>();

            player = new ZPlay();
            player.OpenFile(currentFileName, TStreamFormat.sfAutodetect);

            TStreamInfo i = new TStreamInfo();
            player.GetStreamInfo(ref i);
            metadata = new AudioMetadata(i.Length.sec);

            AddBookmark(0);

            ReadID3Info();

            if (!player.StartPlayback())
                Console.WriteLine("Unable to start playback: " + player.GetError());

            timer = new Timer(MyTimerCallback, null, 0, 500);

            myTimeLine.PositionChanged += myTimeLine_PositionChanged;

            globalHotkeys = new KeyboardHook();
            globalHotkeys.KeyPressed += GlobalHotKeyPressed;

            try
            {
                globalHotkeys.RegisterHotKey(KeyModifier.Alt | KeyModifier.Control, Key.J);
                globalHotkeys.RegisterHotKey(KeyModifier.Alt | KeyModifier.Control, Key.K);
                globalHotkeys.RegisterHotKey(KeyModifier.Alt | KeyModifier.Control, Key.H);
                globalHotkeys.RegisterHotKey(KeyModifier.Alt | KeyModifier.Control, Key.L);
                globalHotkeys.RegisterHotKey(KeyModifier.Alt | KeyModifier.Control, Key.M);
            }
            catch
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
                case Key.M:
                    t = new TStreamTime();
                    player.GetPosition(ref t);
                    AddBookmark((int)t.sec);
                    break;
                case Key.H:
                    t = new TStreamTime();
                    player.GetPosition(ref t);
                    t = metadata.GetPreviousBookmark(t);
                    player.Seek(TTimeFormat.tfSecond, ref t, TSeekMethod.smFromBeginning);
                    break;
                case Key.L:
                    t = new TStreamTime();
                    player.GetPosition(ref t);
                    t = metadata.GetNextBookmark(t);
                    player.Seek(TTimeFormat.tfSecond, ref t, TSeekMethod.smFromBeginning);
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
            TStreamTime newPos = new TStreamTime() { sec = (uint)(metadata.Duration * e.NewValue) };
            player.Seek(TTimeFormat.tfSecond, ref newPos, TSeekMethod.smFromBeginning);
        }

        private void MyTimerCallback(object state)
        {
            TStreamTime t = new TStreamTime();
            player.GetPosition(ref t);

            _dispatcher.BeginInvoke(
                DispatcherPriority.Normal, new Action(() =>
                {
                    myTimeLine.Value = t.sec / (double)metadata.Duration;
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

        private void AddBookmark(int t)
        {
            metadata.Bookmarks.Add(t);
            double p = t / (double)metadata.Duration;

            Border border = new Border()
            {
                Width = 2,
                Height = 6,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Margin = new Thickness(myTimeLine.ActualWidth * p - 1, 2, 0, 2),
                BorderThickness = new Thickness(1, 0, 1, 0),
                BorderBrush = new SolidColorBrush(Colors.Black),
                SnapsToDevicePixels = true,
                Tag = p
            };
            Grid.SetRow(border, 0);

            bookmarksDashes.Add(border);
            TimeLineGrid.Children.Add(border);
        }

        void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (bookmarksDashes == null)
                return;

            double p;
            Thickness t;
            foreach (var b in bookmarksDashes)
            {
                p = (double)b.Tag;
                t = b.Margin;
                t.Left = myTimeLine.ActualWidth * p - 1;
                b.Margin = t;
            }
        }
    }

    public class AudioMetadata
    {
        public List<int> Bookmarks = new List<int>();
        public uint Duration = 0;

        public AudioMetadata()
        {

        }

        public AudioMetadata(uint duration)
        {
            Duration = duration;
        }

        public TStreamTime GetPreviousBookmark(TStreamTime t)
        {
            uint max = 0;
            uint current = t.sec - 1; // minus one second, to leave a time to skip to previous bookmark when double-clicking 'back' button

            foreach (var i in Bookmarks)
            {
                if (i > max && i < current)
                    max = (uint)i;
            }
            return new TStreamTime() { sec = max };
        }

        public TStreamTime GetNextBookmark(TStreamTime t)
        {
            uint min = Duration;
            uint current = t.sec;

            foreach (var i in Bookmarks)
            {
                if (i < min && i > current)
                    min = (uint)i;
            }
            return new TStreamTime() { sec = min };
        }
    }
}
