using System;
using System.Windows.Input;

namespace Shiori.Lib
{
    class SimpleCommand : ICommand
    {
        public delegate void cbDelegate(Object _obj);
        cbDelegate callBack { get; set; }
        Object obj { get; set; }

        public SimpleCommand(cbDelegate _delegate, Object _obj)
        {
            callBack = _delegate;
            obj = _obj;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            callBack.Invoke(obj);
        }
    }
}
