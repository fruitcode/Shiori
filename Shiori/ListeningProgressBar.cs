using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Shiori
{
    public class ListeningProgressBar : Control
    {
        public static readonly DependencyProperty ListeningProgressSourceProperty =
                DependencyProperty.Register("ListeningProgressSource", typeof(object), typeof(ListeningProgressBar), new PropertyMetadata(0));

        public object ListeningProgressSource
        {
            get { return (object)GetValue(ListeningProgressSourceProperty); }
            set { SetValue(ListeningProgressSourceProperty, value); }
        }

        static ListeningProgressBar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ListeningProgressBar), new FrameworkPropertyMetadata(typeof(ListeningProgressBar)));
        }
    }
}
