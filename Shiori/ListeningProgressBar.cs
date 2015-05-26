using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Shiori.Playlist;

namespace Shiori
{
    public class ListeningProgressBar : Control
    {
        public static readonly DependencyProperty TrackSourceProperty =
                DependencyProperty.Register("TrackSource",
                typeof(PlaylistElement), typeof(ListeningProgressBar),
                new PropertyMetadata(null, new PropertyChangedCallback(OnTrackSourceChanged)));

        public PlaylistElement TrackSource
        {
            get { return (PlaylistElement)GetValue(TrackSourceProperty); }
            set { SetValue(TrackSourceProperty, value); }
        }

        private static void OnTrackSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //Console.WriteLine("TrackSourceChanged");
        }

        static ListeningProgressBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ListeningProgressBar), new FrameworkPropertyMetadata(typeof(ListeningProgressBar)));
        }
    }
}
