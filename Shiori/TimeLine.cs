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
        public static readonly DependencyProperty BarWidthProperty = DependencyProperty.Register("BarWidth", typeof(int), typeof(TimeLine), new PropertyMetadata(10));
        public int BarWidth {
            get { return (int)GetValue(BarWidthProperty); }
            set { SetValue(BarWidthProperty, value); }
        }
        private int oldBarWidth;
        
        static TimeLine()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimeLine), new FrameworkPropertyMetadata(typeof(TimeLine)));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            //base.OnMouseMove(e);
            if (seekingMode)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    FillProgress(e.GetPosition(this).X);
                else
                    seekingMode = false;
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            seekingMode = true;
            allowBarUpdate = false;

            oldBarWidth = BarWidth;
            FillProgress(e.GetPosition(this).X);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            seekingMode = false;
            // TODO: execute callback OnPositionChanged(TimeSpan newPosition)
            allowBarUpdate = true;
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            //base.OnMouseLeave(e);
            if (seekingMode)
            {
                allowBarUpdate = true;
                // TODO: set actual value
                FillProgress(oldBarWidth);
            }
        }

        private void FillProgress(double p)
        {
            BarWidth = (int)p;
        }
    }
}
