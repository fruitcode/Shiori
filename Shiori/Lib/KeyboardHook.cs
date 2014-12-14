using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Input;

namespace Shiori.Lib
{
    public sealed class KeyboardHook : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        /// <summary>
        /// Represents the window that is used internally to get the messages.
        /// </summary>
        private class HKWindow : Window
        {
            private static int WM_HOTKEY = 0x0312;
            public IntPtr hWnd;

            public HKWindow()
            {
                WindowInteropHelper helper = new WindowInteropHelper(this);
                hWnd = helper.EnsureHandle();

                HwndSource source = HwndSource.FromHwnd(hWnd);
                source.AddHook(new HwndSourceHook(WndProc));
            }

            private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                // check if we got a hot key pressed.
                if (msg == WM_HOTKEY)
                {
                    Key key = KeyInterop.KeyFromVirtualKey( (((int)lParam >> 16) & 0xFFFF) );
                    KeyModifier modifier = (KeyModifier)((int)lParam & 0xFFFF);

                    // invoke the event to notify the parent.
                    if (KeyPressed != null)
                        KeyPressed(this, new KeyPressedEventArgs(modifier, key));

                    handled = true;
                }
                
                return IntPtr.Zero;
            }

            public event EventHandler<KeyPressedEventArgs> KeyPressed;
        }

        private HKWindow _window = new HKWindow();
        private int _currentId;

        public event EventHandler<KeyPressedEventArgs> KeyPressed;

        public KeyboardHook()
        {
            // register the event of the inner native window.
            _window.KeyPressed += delegate(object sender, KeyPressedEventArgs args)
            {
                if (KeyPressed != null)
                    KeyPressed(this, args);
            };
        }

        /// <summary>
        /// Registers a hot key in the system.
        /// </summary>
        /// <param name="modifier">The modifiers that are associated with the hot key.</param>
        /// <param name="key">The key itself that is associated with the hot key.</param>
        public void RegisterHotKey(KeyModifier modifier, Key key)
        {
            _currentId = _currentId + 1;

            // register the hot key.
            if (!RegisterHotKey(_window.hWnd, _currentId, (uint)modifier, (uint)KeyInterop.VirtualKeyFromKey(key)))
            {
                throw new InvalidOperationException("Couldn’t register the hot key.");
            }
        }

        #region IDisposable Members
        public void Dispose()
        {
            // unregister all the registered hot keys.
            for (int i = _currentId; i > 0; i--)
            {
                UnregisterHotKey(_window.hWnd, i);
            }
            _window.Close();
        }
        #endregion
    }

    /// <summary>
    /// Event Args for the event that is fired after the hot key has been pressed.
    /// </summary>
    public class KeyPressedEventArgs : EventArgs
    {
        private KeyModifier _modifier;
        private Key _key;

        internal KeyPressedEventArgs(KeyModifier modifier, Key key)
        {
            _modifier = modifier;
            _key = key;
        }

        public KeyModifier Modifier
        {
            get { return _modifier; }
        }

        public Key Key
        {
            get { return _key; }
        }
    }

    [Flags]
    public enum KeyModifier : uint
    {
        Alt = 1,
        Control = 2,
        Shift = 4,
        Win = 8
    }
}
