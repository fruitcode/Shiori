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
        private Boolean isSeeking = false;
        public static readonly DependencyProperty BarWidthProperty = DependencyProperty.Register("BarWidth", typeof(int), typeof(TimeLine), new PropertyMetadata(10));
        public int BarWidth {
            get { return (int)GetValue(BarWidthProperty); }
            set { SetValue(BarWidthProperty, value); }
        }
        
        static TimeLine()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimeLine), new FrameworkPropertyMetadata(typeof(TimeLine)));
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            //base.OnMouseMove(e);
            if (isSeeking)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                    FillProgress(e.GetPosition(this).X);
                else
                    isSeeking = false;
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            //base.OnMouseLeftButtonDown(e);
            isSeeking = true;
            FillProgress(e.GetPosition(this).X);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            //base.OnMouseLeftButtonUp(e);
            isSeeking = false;
        }

        private void FillProgress(double p)
        {
            BarWidth = (Int16)p;
        }
    }
}
