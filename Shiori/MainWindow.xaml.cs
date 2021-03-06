﻿using System;
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
                    t = new TStreamTime() { ms = 5000 };
                    PlayerSeekTo(t, TSeekMethod.smFromCurrentBackward);
                    break;
                case Key.K: // forward 10 seconds
                    t = new TStreamTime() { ms = 10000 };
                    PlayerSeekTo(t, TSeekMethod.smFromCurrentForward);
                    break;
                case Key.M: // add bookmark
                    t = new TStreamTime();
                    player.GetPosition(ref t);
                    playlistManager.CurrentElement.AddBookmark(t.ms);
                    break;
                case Key.H: // go to previous bookmark
                    t = new TStreamTime();
                    player.GetPosition(ref t);
                    t = playlistManager.CurrentElement.GetPreviousBookmark(t);
                    PlayerSeekTo(t, TSeekMethod.smFromBeginning);
                    break;
                case Key.L: // go to next bookmark
                    t = new TStreamTime();
                    player.GetPosition(ref t);
                    t = playlistManager.CurrentElement.GetNextBookmark(t);
                    PlayerSeekTo(t, TSeekMethod.smFromBeginning);
                    break;
                case Key.U:
                    if (GetPlayerState() == PlayerState.Play)
                    {
                        player.PausePlayback();
                        StopUpdateTimer();
                    }
                    else
                    {
                        player.ResumePlayback();
                        StartUpdateTimer();
                    }
                    break;
                default:
                    break;
            }
        }

        void myTimeLine_PositionChanged(object sender, PositionChangedEventArgs e)
        {
            if (player != null)
            {
                TStreamTime newPos = new TStreamTime() { ms = (uint)(currentPlaylistElement.Duration * e.NewValue) };
                PlayerSeekTo(newPos, TSeekMethod.smFromBeginning);
            }
        }

        private void PlayerSeekTo(TStreamTime t, TSeekMethod method)
        {
            Console.WriteLine(">>> {0}", t.ms);
            PlayerState state = GetPlayerState();
            FinishListeningRange();
            player.Seek(TTimeFormat.tfMillisecond, ref t, method);
            myTimeLine.Value = (double)t.ms / currentPlaylistElement.Duration;
            // for some reason, zPlay will resume playing after seeking
            // and it will leave its 'state' as 'paused'
            if (state != PlayerState.Play)
                player.PausePlayback();
            StartListeningRange();
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (ClosePlaylist(true, false) == false)
            {
                e.Cancel = true; // cancel exiting
                return;
            }
            globalHotkeys.Dispose();
        }

        private Boolean ClosePlaylist(Boolean showAlert, Boolean showFileDialog)
        {
            StopUpdateTimer();

            if (player != null)
            {
                FinishListeningRange();
                currentPlaylistElement = null;
                player.StopPlayback();
            }

            return playlistManager.Save(showAlert, showFileDialog);
        }

        private void StartUpdateTimer()
        {
            if (updateTimeLineTimer == null)
                updateTimeLineTimer = new Timer(UpdateTimeLineValue, null, 0, 200);
        }

        private void StopUpdateTimer()
        {
            if (updateTimeLineTimer != null)
            {
                updateTimeLineTimer.Dispose();
                updateTimeLineTimer = null;
            }
        }

        private void UpdateTimeLineValue(object state)
        {
            TStreamTime t = new TStreamTime();
            player.GetPosition(ref t);

            _dispatcher.BeginInvoke(
                DispatcherPriority.Normal, new Action(() =>
                {
                    if (currentPlaylistElement != null)
                    {
                        currentPlaylistElement.UpdateListeningRange(t.ms);
                        myTimeLine.Value = t.ms / (double)currentPlaylistElement.Duration;
                    }
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

            myTimeLine.BookmarksSource = currentPlaylistElement.Bookmarks;
            myListeningProgressBar.TrackSource = currentPlaylistElement;

            // TODO: start from position where playback have been stopped last time
            if (player.StartPlayback())
            {
                currentPlaylistElement.StartListeningRange(0);
                player.SetCallbackFunc(myZPlayCallbackFunction, (TCallbackMessage)TCallbackMessage.MsgStop, 0);
            } else {
                Console.WriteLine("Unable to start playback: " + player.GetError());
            }

            StartUpdateTimer();
        }

        private int ZPlayCallbackFunction(uint objptr, int user_data, TCallbackMessage msg, uint param1, uint param2)
        {
            if (currentPlaylistElement == null)
            {
                // Media is stopped because was started playback of another media
                // So, we've already updated ListeningRange for previous media
                return 0;
            }

            StopUpdateTimer();

            TStreamInfo info = new TStreamInfo();
            player.GetStreamInfo(ref info);
            TStreamTime time = info.Length;

            var tmp = currentPlaylistElement;
            currentPlaylistElement = null;
            _dispatcher.BeginInvoke(
                DispatcherPriority.Normal, new Action(() =>
                {
                    tmp.EndListeningRange(time.ms);
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

            currentPlaylistElement.EndListeningRange(t.ms);
        }

        private void StartListeningRange()
        {
            if (currentPlaylistElement == null)
                return;

            TStreamTime t = new TStreamTime();
            player.GetPosition(ref t);
            currentPlaylistElement.StartListeningRange(t.ms);
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
                        if (ClosePlaylist(true, false) == false)
                            break;
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
            PlaylistNameLabel.Content = playlistManager.Title;
            PlaylistListBox.ItemsSource = playlistManager.PlaylistElementsArray;

            System.Windows.Data.CollectionView myView = (System.Windows.Data.CollectionView)System.Windows.Data.CollectionViewSource.GetDefaultView(PlaylistListBox.ItemsSource);
            myView.GroupDescriptions.Clear();
            myView.GroupDescriptions.Add(new System.Windows.Data.PropertyGroupDescription("ArtistAlbum"));
        }

        private enum PlayerState
        {
            Play,
            Pause,
            Stop
        }

        private PlayerState GetPlayerState()
        {
            TStreamStatus s = new TStreamStatus();
            player.GetStatus(ref s);
            if (s.fPlay)
            {
                return PlayerState.Play;
            }
            else if (s.fPause)
            {
                return PlayerState.Pause;
            }
            else
            {
                return PlayerState.Stop;
            }
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
            int min = 0, max = 0;
            SelectionRange(ref min, ref max);
            for (int i = 0; i <= max - min; i++)
                playlistManager.DeleteElement(min);

            if (playlistManager.PlaylistElementsArray.Count == 0)
                return;

            if (playlistManager.PlaylistElementsArray.Count <= min)
                min--;

            PlaylistListBox.SelectedItem = playlistManager.PlaylistElementsArray[min];
        }

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (player != null)
                player.SetMasterVolume((int)e.NewValue, (int)e.NewValue);
        }

        private void PlaylistNameLabel_MouseUp(object sender, MouseButtonEventArgs e)
        {
            PlaylistNameTextBox.Text = playlistManager.Title;
            PlaylistNameLabel.Visibility = System.Windows.Visibility.Hidden;
            PlaylistNameTextBox.Visibility = System.Windows.Visibility.Visible;
            PlaylistNameTextBox.Focus();
            PlaylistNameTextBox.SelectAll();
        }

        private void PlaylistNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                PlaylistNameLabel.Visibility = System.Windows.Visibility.Visible;
                PlaylistNameTextBox.Visibility = System.Windows.Visibility.Collapsed;
            }
            else if (e.Key == Key.Enter)
            {
                playlistManager.Title = PlaylistNameTextBox.Text;
                PlaylistNameLabel.Content = PlaylistNameTextBox.Text;
                PlaylistNameLabel.Visibility = System.Windows.Visibility.Visible;
                PlaylistNameTextBox.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void SaveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // TODO: should not stop playback, but also should finish current ListeningRange and should not form a gap in progress
            // (which will be caused by subsequential calls of FinishListeningRange and StartListeningRange)
            ClosePlaylist(false, false);
        }

        private void SaveAsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ClosePlaylist(false, true);
        }

        private void NewPlaylistMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ClosePlaylist(true, false);

            playlistManager = new PlaylistManager();
            BindPlaylist();
        }

        private void ManageBookmarksMenuItem_Click(object sender, RoutedEventArgs e)
        {
            BookmarksWindow bookmarksWindow = new BookmarksWindow();
            bookmarksWindow.CurrentPlaylistElement = currentPlaylistElement;

            bookmarksWindow.ShowDialog();
        }
    }
}
