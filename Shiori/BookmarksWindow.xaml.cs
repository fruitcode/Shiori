using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Shiori.Playlist;

namespace Shiori
{
    public partial class BookmarksWindow : Window
    {
        public ObservableCollection<Bookmark> BookmarksList;

        public BookmarksWindow()
        {
            InitializeComponent();

            this.Loaded += BookmarksWindow_Loaded;
            this.Closing += BookmarksWindow_Closing;
        }

        void BookmarksWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            BookmarksListBox.ItemsSource = null;
            BookmarksList = null;
        }

        void BookmarksWindow_Loaded(object sender, RoutedEventArgs e)
        {
            BookmarksListBox.ItemsSource = BookmarksList;
        }
    }
}
