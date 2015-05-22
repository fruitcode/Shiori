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
        PlaylistElement currentPlaylistElement;
        //ListeningProgressRange currentListeningRange;
        ZPlay player;
        Timer updateTimeLineTimer;
        TCallbackFunc myZPlayCallbackFunction;

        KeyboardHook globalHotkeys;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;

            myZPlayCallbackFunction = new TCallbackFunc(ZPlayCallbackFunction);
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
                globalHotkeys.RegisterHotKey(KeyModifier.Alt | KeyModifier.Control, Key.U);
            }
            catch
            {
                MessageBox.Show("Unable to register global hotkeys", "Error");
            }
        }

        void GlobalHotKeyPressed(object sender, KeyPressedEventArgs e)
        {
            if (player == null)
                return;

            TStreamTime t;
            switch (e.Key)
            {
                case Key.J: // backward 5 seconds
                    FinishListeningRange();
                    t = new TStreamTime() { sec = 5 };
                    player.Seek(TTimeFormat.tfSecond, ref t, TSeekMethod.smFromCurrentBackward);
                    StartListeningRange();
                    break;
                case Key.K: // forward 10 seconds
                    FinishListeningRange();
                    t = new TStreamTime() { sec = 10 };
                    player.Seek(TTimeFormat.tfSecond, ref t, TSeekMethod.smFromCurrentForward);
                    StartListeningRange();
                    break;
                case Key.M: // add bookmark
                    t = new TStreamTime();
                    player.GetPosition(ref t);
                    playlistManager.CurrentElement.AddBookmark(t.ms);
                    break;
                case Key.H: // go to previous bookmark
                    FinishListeningRange();
                    t = new TStreamTime();
                    player.GetPosition(ref t);
                    t = playlistManager.CurrentElement.GetPreviousBookmark(t);
                    player.Seek(TTimeFormat.tfMillisecond, ref t, TSeekMethod.smFromBeginning);
                    StartListeningRange();
                    break;
                case Key.L: // go to next bookmark
                    FinishListeningRange();
                    t = new TStreamTime();
                    player.GetPosition(ref t);
                    t = playlistManager.CurrentElement.GetNextBookmark(t);
                    player.Seek(TTimeFormat.tfMillisecond, ref t, TSeekMethod.smFromBeginning);
                    StartListeningRange();
                    break;
                case Key.U:
                    TStreamStatus s = new TStreamStatus();
                    player.GetStatus(ref s);
                    if (s.fPause)
                        player.ResumePlayback();
                    else if (s.fPlay)
                        player.PausePlayback();
                    break;
                default:
                    break;
            }
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ClosePlaylist();
            globalHotkeys.Dispose();
        }

        private void ClosePlaylist()
        {
            if (player != null)
            {
                FinishListeningRange();
                currentPlaylistElement = null;
                player.StopPlayback();
            }

            playlistManager.Save();
        }

        void myTimeLine_PositionChanged(object sender, PositionChangedEventArgs e)
        {
            if (player != null)
            {
                FinishListeningRange();
                TStreamTime newPos = new TStreamTime() { ms = (uint)(playlistManager.CurrentElement.Duration * e.NewValue) };
                player.Seek(TTimeFormat.tfMillisecond, ref newPos, TSeekMethod.smFromBeginning);
                StartListeningRange();
            }
        }

        private void UpdateTimeLineValue(object state)
        {
            TStreamTime t = new TStreamTime();
            player.GetPosition(ref t);

            _dispatcher.BeginInvoke(
                DispatcherPriority.Normal, new Action(() =>
                {
                    myTimeLine.Value = t.ms / (double)playlistManager.CurrentElement.Duration;
                }
            ));
        }

        private void PlaylistListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2)
                return;

            FinishListeningRange();
            currentPlaylistElement = null;

            if (player != null)
            {
                player.StopPlayback();
                player.Close();
            }
            else
            {
                player = new ZPlay();
                int _volume = (int)volumeSlider.Value;
                player.SetMasterVolume(_volume, _volume);
            }

            if ((currentPlaylistElement = PlaylistListBox.SelectedItem as PlaylistElement) == null)
                return;

            playlistManager.NowPlayingIndex = playlistManager.PlaylistElementsArray.IndexOf(currentPlaylistElement);
            player.OpenFile(currentPlaylistElement.FilePath, TStreamFormat.sfAutodetect);

            this.Title = currentPlaylistElement.Title + " - Shiori";

            myTimeLine.BookmarksSource = playlistManager.CurrentElement.BookmarksPercents;

            // TODO: start from position where playback have been stopped last time
            if (player.StartPlayback())
            {
                currentPlaylistElement.StartProgressRange(0);
                player.SetCallbackFunc(myZPlayCallbackFunction, (TCallbackMessage)TCallbackMessage.MsgStop, 0);
            } else {
                Console.WriteLine("Unable to start playback: " + player.GetError());
            }

            updateTimeLineTimer = new Timer(UpdateTimeLineValue, null, 0, 500);
        }

        private int ZPlayCallbackFunction(uint objptr, int user_data, TCallbackMessage msg, uint param1, uint param2)
        {
            if (currentPlaylistElement == null)
            {
                // Media is stopped because was started playback of another media
                // So, we've already updated ListeningRange for previous media
                return 0;
            }

            TStreamInfo info = new TStreamInfo();
            player.GetStreamInfo(ref info);
            TStreamTime time = info.Length;

            var tmp = currentPlaylistElement;
            currentPlaylistElement = null;
            _dispatcher.BeginInvoke(
                DispatcherPriority.Normal, new Action(() =>
                {
                    tmp.EndProgressRange(time.ms);
                }
            ));

            return 0;
        }

        private void FinishListeningRange()
        {
            if (currentPlaylistElement == null)
                return;

            TStreamTime t = new TStreamTime();
            player.GetPosition(ref t);

            currentPlaylistElement.EndProgressRange(t.ms);
        }

        private void StartListeningRange()
        {
            TStreamTime t = new TStreamTime();
            player.GetPosition(ref t);
            currentPlaylistElement.StartProgressRange(t.ms);
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
                        ClosePlaylist();
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

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (player != null)
                player.SetMasterVolume((int)e.NewValue, (int)e.NewValue);
        }
    }
}
