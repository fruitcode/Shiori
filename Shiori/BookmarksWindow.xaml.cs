using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Shiori.Playlist;
using Shiori.Lib;

namespace Shiori
{
    public partial class BookmarksWindow : Window
    {
        public PlaylistElement CurrentPlaylistElement;

        public BookmarksWindow()
        {
            InitializeComponent();

            this.Loaded += BookmarksWindow_Loaded;
            this.Closing += BookmarksWindow_Closing;
        }

        void BookmarksWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            BookmarksListBox.ItemsSource = null;
            CurrentPlaylistElement = null;
        }

        void BookmarksWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Title = CurrentPlaylistElement.Title + " - Bookmarks";

            BookmarksListBox.ItemsSource = CurrentPlaylistElement.Bookmarks;

            KeyBinding kbDelete = new KeyBinding(new SimpleCommand(DeleteFiles, null), Key.Delete, ModifierKeys.None);
            BookmarksListBox.InputBindings.Add(kbDelete);
        }

        private void DeleteFiles(Object _o)
        {
            List<Bookmark> deleteItems = new List<Bookmark>();
            foreach (Bookmark item in BookmarksListBox.SelectedItems)
                deleteItems.Add(item);

            foreach (var item in deleteItems)
                CurrentPlaylistElement.DeleteBookmark(item);
        }
    }
}
