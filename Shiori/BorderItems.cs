using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiori
{
    public class BorderItems
    {

        int _MarginLeft;
        public int MarginLeft
        {
            get { return _MarginLeft; }
            set
            {
                _MarginLeft = value;
                OnPropertyChanged("MarginLeft");
            }
        }


        string _Tag;
        public string Tag
        {
            get { return _Tag; }
            set
            {
                _Tag = value;
                OnPropertyChanged("Tag");
            }
        }

        
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string Value)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(Value));
            }
        }
    }
}
