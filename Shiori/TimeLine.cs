using System;
using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
//using System.Windows.Data;
//using System.Windows.Documents;
using System.Windows.Input;
//using System.Windows.Media;
//using System.Windows.Media.Imaging;
//using System.Windows.Navigation;
//using System.Windows.Shapes;

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

        public static readonly DependencyProperty BarValueProperty = DependencyProperty.Register("BarValue", typeof(double), typeof(TimeLine), new PropertyMetadata(0.2));
        public double BarValue {
            get { return (double)GetValue(BarValueProperty); }
            set { SetValue(BarValueProperty, value); }
        }
        
        static TimeLine()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimeLine), new FrameworkPropertyMetadata(typeof(TimeLine)));
        }

        public static readonly DependencyProperty ItemsSourceProperty =
                DependencyProperty.Register("BookmarksSource", typeof(object), typeof(TimeLine), new PropertyMetadata(0));

        public object BookmarksSource
        {
            get { return (object)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
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
