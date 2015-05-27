using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Shiori.Playlist;

namespace Shiori
{
    public class TimeLine : Control
    {
        private Boolean seekingMode = false;
        private Boolean allowBarUpdate = true;

        public event EventHandler<PositionChangedEventArgs> PositionChanged;

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(double), typeof(TimeLine), new PropertyMetadata(0.2));
        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set
            {
                if (allowBarUpdate)
                {
                    BarValue = value;
                }
                SetValue(ValueProperty, value);
            }
        }

        public static readonly DependencyProperty BarValueProperty = DependencyProperty.Register("BarValue", typeof(double), typeof(TimeLine), new PropertyMetadata(0.0));
        public double BarValue
        {
            get { return (double)GetValue(BarValueProperty); }
            set { SetValue(BarValueProperty, value); }
        }

        public static readonly DependencyProperty BookmarksSourceProperty =
                DependencyProperty.Register("BookmarksSource",
                typeof(IEnumerable<Bookmark>), typeof(TimeLine), new PropertyMetadata(null));

        public IEnumerable<Bookmark> BookmarksSource
        {
            get { return (IEnumerable<Bookmark>)GetValue(BookmarksSourceProperty); }
            set { SetValue(BookmarksSourceProperty, value); }
        }

        static TimeLine()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimeLine), new FrameworkPropertyMetadata(typeof(TimeLine)));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (seekingMode)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    if (allowBarUpdate) // we've returned from outside and we should stop updating bar again
                        allowBarUpdate = false;
                    BarValue = e.GetPosition(this).X / this.ActualWidth;
                }
                else
                {
                    seekingMode = false;
                }
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            seekingMode = true;
            allowBarUpdate = false;

            BarValue = e.GetPosition(this).X / this.ActualWidth;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            seekingMode = false;
            allowBarUpdate = true;

            Value = e.GetPosition(this).X / this.ActualWidth;
            PositionChangedEventArgs pce = new PositionChangedEventArgs() { NewValue = Value };
            PositionChanged(this, pce);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (seekingMode)
            {
                allowBarUpdate = true;
                BarValue = Value;
            }
        }
    }

    public class PositionChangedEventArgs : EventArgs
    {
        public double NewValue { get; set; }
    }
}
