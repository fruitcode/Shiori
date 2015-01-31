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
using Shiori.Playlist;

namespace Shiori
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

        PlaylistManager playlistManager;
        ZPlay player;
        Timer updateTimeLineTimer;

        List<Border> bookmarksDashes = new List<Border>();

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
            playlistManager = new PlaylistManager();
            BindPlaylist();

            myTimeLine.PositionChanged += myTimeLine_PositionChanged;


            KeyBinding kbUp = new KeyBinding(new SimpleCommand(MoveFilesUp, null), Key.Up, ModifierKeys.Control);
            KeyBinding kbDown = new KeyBinding(new SimpleCommand(MoveFilesDown, null), Key.Down, ModifierKeys.Control);
            KeyBinding kbDelete = new KeyBinding(new SimpleCommand(DeleteFiles, null), Key.Delete, ModifierKeys.None);
            PlaylistListBox.InputBindings.Add(kbUp);
            PlaylistListBox.InputBindings.Add(kbDown);
            PlaylistListBox.InputBindings.Add(kbDelete);


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
                    playlistManager.CurrentElement.AddBookmark((int)t.sec);
                    AddBookmark((int)t.sec);
                    break;
                case Key.H:
                    t = new TStreamTime();
                    player.GetPosition(ref t);
                    t = playlistManager.CurrentElement.GetPreviousBookmark(t);
                    player.Seek(TTimeFormat.tfSecond, ref t, TSeekMethod.smFromBeginning);
                    break;
                case Key.L:
                    t = new TStreamTime();
                    player.GetPosition(ref t);
                    t = playlistManager.CurrentElement.GetNextBookmark(t);
                    player.Seek(TTimeFormat.tfSecond, ref t, TSeekMethod.smFromBeginning);
                    break;
                default:
                    break;
            }
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            globalHotkeys.Dispose();

            playlistManager.Save();
        }

        void myTimeLine_PositionChanged(object sender, PositionChangedEventArgs e)
        {
            TStreamTime newPos = new TStreamTime() { sec = (uint)(playlistManager.CurrentElement.Duration * e.NewValue) };
            player.Seek(TTimeFormat.tfSecond, ref newPos, TSeekMethod.smFromBeginning);
        }

        private void UpdateTimeLineValue(object state)
        {
            TStreamTime t = new TStreamTime();
            player.GetPosition(ref t);

            _dispatcher.BeginInvoke(
                DispatcherPriority.Normal, new Action(() =>
                {
                    myTimeLine.Value = t.sec / (double)playlistManager.CurrentElement.Duration;
                }
            ));
        }

        private void AddBookmark(int time)
        {
            double p = time / (double)playlistManager.CurrentElement.Duration;

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

        private void PlaylistListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var s = sender as ListBox;
            if (s.SelectedItem == null)
                return;

            var element = s.SelectedItem as PlaylistElement;

            if (player != null)
            {
                player.StopPlayback();
                player.Close();
            }
            else
            {
                player = new ZPlay();
            }

            playlistManager.NowPlayingIndex = playlistManager.PlaylistElementsArray.IndexOf(element);
            player.OpenFile(element.FilePath, TStreamFormat.sfAutodetect);

            InfoLabelArtistAlbum.Content = element.ArtistAlbum;
            InfoLabelTitle.Content = element.Title;

            // clear old bookmarks and place bookmarks for new file
            foreach (var b in bookmarksDashes) { TimeLineGrid.Children.Remove(b); }
            bookmarksDashes.Clear();
            foreach (int t in playlistManager.CurrentElement.Bookmarks) { AddBookmark(t); }

            if (!player.StartPlayback())
                Console.WriteLine("Unable to start playback: " + player.GetError());

            updateTimeLineTimer = new Timer(UpdateTimeLineValue, null, 0, 500);
        }

        private void PlaylistListBox_Drop(object sender, DragEventArgs e)
        {
            //int t = int.Parse(((ListBox)sender).Tag.ToString());
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] _files = (string[])e.Data.GetData(DataFormats.FileDrop);
                List<String> files = _files.OrderBy(d => d).ToList();
                foreach (var f in files)
                {
                    if (f.IndexOf(".shiori") == f.Length - 7 && f.Length > 7)
                    {
                        playlistManager.Save();
                        playlistManager = new PlaylistManager(f);
                        BindPlaylist();
                        break;
                    }
                    else
                        playlistManager.AddFile(f);
                }
            }
        }

        private void BindPlaylist()
        {
            PlaylistListBox.ItemsSource = playlistManager.PlaylistElementsArray;

            System.Windows.Data.CollectionView myView = (System.Windows.Data.CollectionView)System.Windows.Data.CollectionViewSource.GetDefaultView(PlaylistListBox.ItemsSource);
            myView.GroupDescriptions.Clear();
            myView.GroupDescriptions.Add(new System.Windows.Data.PropertyGroupDescription("ArtistAlbum"));
        }

        private void MoveFilesUp(Object _o)
        {
            int min = 0, max = 0;
            SelectionRange(ref min, ref max);
            playlistManager.MoveFilesUp(min, max);
        }

        private void MoveFilesDown(Object _o)
        {
            int min = 0, max = 0;
            SelectionRange(ref min, ref max);
            playlistManager.MoveFilesDown(min, max);
        }

        private void SelectionRange(ref int min, ref int max)
        {
            min = PlaylistListBox.Items.Count;
            max = -1;

            int i;
            foreach (PlaylistElement f in PlaylistListBox.SelectedItems)
            {
                i = playlistManager.PlaylistElementsArray.IndexOf(f);
                if (i < min)
                    min = i;
                if (i > max)
                    max = i;
            }
        }

        private void DeleteFiles(Object _o)
        {
            List<PlaylistElement> selected = new List<PlaylistElement>();

            foreach (PlaylistElement f in PlaylistListBox.SelectedItems)
                selected.Add(f);
            foreach (PlaylistElement f in selected)
                playlistManager.DeleteElement(f);
        }
    }
}
