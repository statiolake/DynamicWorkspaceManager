using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace DynamicWorkspaceManager
{
    public class HotKeyManager
    {
        public HotKeyManager(Window window)
        {
            var host = new WindowInteropHelper(window);
            this.hWnd = host.Handle;
            ComponentDispatcher.ThreadPreprocessMessage += PreprocessMessage;
        }

        private void PreprocessMessage(ref MSG msg, ref bool handled)
        {
            if (msg.message != WM_HOTKEY) return;
            var id = msg.wParam.ToInt32();
            this.actions[id]();
        }

        public HotKeyDisposable Register(
            ModifierKeys modifierKeys, Key key, Action action)
        {
            var mod = (int)modifierKeys;
            var vkey = KeyInterop.VirtualKeyFromKey(key);

            // Register hotkey
            var res = RegisterHotKey(
                this.hWnd, this.currentId, mod, vkey);

            // If fails, throw an exception.
            if (res == 0)
            {
                var err = Marshal.GetLastWin32Error();
                var replMods = modifierKeys
                    .ToString()
                    .Replace(", ", " + ")
                    .Replace("Control", "Ctrl");

                // Hacky: To avoid FormatMessage()
                var msg = new Win32Exception(err).Message;
                throw new Win32Exception(
                    err, string.Format("{0}: {1} + {2}", msg, replMods, key));
            }

            var id = this.currentId;
            this.currentId++;

            this.actions.Add(id, action);
            return new HotKeyDisposable(this, id);
        }

        public void Unregister(int id)
        {
            var res = UnregisterHotKey(this.hWnd, id);
            if (res == 0)
            {
                throw new InvalidOperationException("Unregistration failed");
            }

            this.actions.Remove(id);
        }

        [DllImport("user32.dll", SetLastError = true)]
        extern static int RegisterHotKey(
            IntPtr hWnd, int id, int modkeys, int key);

        [DllImport("user32.dll", SetLastError = true)]
        extern static int UnregisterHotKey(IntPtr hWnd, int id);

        // private const int ERROR_HOTKEY_ALREADY_REGISTERED = 0x0581;
        private const int WM_HOTKEY = 0x0312;
        // private const int MAX_HOTKEY_ID = 0xc000;

        private readonly IntPtr hWnd;
        private readonly Dictionary<int, Action> actions
            = new Dictionary<int, Action>();
        private int currentId = 0;
    }

    public class HotKeyDisposable : IDisposable
    {
        private readonly int id;
        private readonly HotKeyManager manager;
        public HotKeyDisposable(HotKeyManager manager, int id)
        {
            this.manager = manager;
            this.id = id;
        }

        #region IDisposable

        private bool valueDisposed;
        protected virtual void Dispose(bool disposing)
        {
            if (!valueDisposed)
            {
                this.manager.Unregister(this.id);
                valueDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
