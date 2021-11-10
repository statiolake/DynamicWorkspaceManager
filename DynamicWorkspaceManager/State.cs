using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using WindowsDesktop;

namespace DynamicWorkspaceManager
{
    public class State
    {
        private const string DEFAULT_NAME_UNNAMED = "work";
        private const string DEFAULT_NAME_HOME = "home";

        private VirtualDesktop lastWorkspace = null;

        public State(Dispatcher dispatcher)
        {
            InitializeWorkspaces();

            VirtualDesktop.CurrentChanged += async (sender, args) =>
            {
                // Remove old workspace if it is empty.
                var old = args.OldDesktop;
                if (old.IsEmpty())
                {
                    try
                    {
                        old.Remove();
                        this.lastWorkspace = null;
                    }
                    catch (ArgumentException)
                    {
                        // Sometimes fails; ignore it
                    }
                }
                else
                {
                    this.lastWorkspace = old;
                }

                // Ask users to new name if current desktop is unnamed
                var taskNaming = dispatcher.Invoke(async () =>
                {
                    var curr = args.NewDesktop;
                    if (string.IsNullOrEmpty(curr.Name))
                    {
                        try
                        {
                            var name = await WorkspacePromptWindow.GetUserInput(true);

                            var existingNames = new HashSet<string>(
                                VirtualDesktop
                                    .GetDesktops()
                                    .Select(desktop => desktop.Name));

                            string cand = name;
                            int index = 0;
                            while (existingNames.Contains(cand))
                            {
                                index++;
                                cand = string.Format("{0}{1}", name, index);
                            }

                            curr.Name = cand;
                        }
                        catch (OperationCanceledException)
                        {
                            // Cancelled; ignore.
                        }
                    }
                });

                // Notify current desktop
                var taskNotifying = dispatcher.Invoke(async () =>
                {
                    var name = VirtualDesktop.Current.Name;
                    if (string.IsNullOrEmpty(name))
                    {
                        name = "(Unnamed)";
                    }

                    using (CurrentDesktopNotifierPopup.Open(name))
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                });

                await Task.WhenAll(taskNaming, taskNotifying);
            };

            VirtualDesktop.Destroyed += (sender, args) =>
            {
                if (this.lastWorkspace == args.Destroyed)
                {
                    this.lastWorkspace = null;
                }
            };
        }

        /// <summary>
        /// Initialize workspaces:
        ///
        /// - Give unique names for all existing unique workspace
        /// - Give names for all existing unnamed workspace
        /// </summary>
        public static void InitializeWorkspaces()
        {
            // Order is important: name before uniqify.
            EnsureNoUnnamed();
            EnsureUniqueName();
        }

        public void Switch(string name)
            => GetOrCreate(name).SwitchFocus();

        public void ShiftSwitch(string name)
            => GetOrCreate(name).ShiftSwitch(GetForegroundWindow());

        public async void SwitchToWorkspacePrompt()
        {
            try
            {
                var name = await WorkspacePromptWindow.GetUserInput();
                // Wait for window close
                await Task.Delay(TimeSpan.FromMilliseconds(200));
                var desktop = GetOrCreate(name);
                desktop.SwitchFocus();
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation.
            }
        }

        public async void ShiftSwitchToWorkspacePrompt()
        {
            try
            {
                var name = await WorkspacePromptWindow.GetUserInput();
                // Wait for window close
                await Task.Delay(TimeSpan.FromMilliseconds(200));
                var desktop = GetOrCreate(name);
                desktop.ShiftSwitch(GetForegroundWindow());
            }
            catch (OperationCanceledException)
            {
                // Ignore cancellation.
            }
        }

        public void SwitchToLastWorkspace()
            => this.lastWorkspace?.SwitchFocus();

        public void ShiftSwitchToLastWorkspace()
            => this.lastWorkspace?.ShiftSwitch(GetForegroundWindow());

        public void SwitchToLeftWorkspace()
            => VirtualDesktop.Current.GetLeft()?.SwitchFocus();

        public void ShiftSwitchToLeftWorkspace()
            => VirtualDesktop.Current.GetLeft()?
                .ShiftSwitch(GetForegroundWindow());

        public void SwitchToRightWorkspace()
            => VirtualDesktop.Current.GetRight()?.SwitchFocus();

        public void ShiftSwitchToRightWorkspace()
            => VirtualDesktop.Current.GetRight()?
                .ShiftSwitch(GetForegroundWindow());

        /// <summary>
        /// Ensure unique workspace name
        /// </summary>
        private static void EnsureUniqueName()
        {
            var found = new HashSet<string>();
            var duplicated = new Dictionary<string, List<VirtualDesktop>>();

            foreach (var desktop in VirtualDesktop.GetDesktops())
            {
                if (string.IsNullOrEmpty(desktop.Name)) continue;

                var name = desktop.Name;
                if (!found.Add(name))
                {
                    if (!duplicated.ContainsKey(name))
                    {
                        duplicated.Add(name, new List<VirtualDesktop>());
                    }
                    duplicated[name].Add(desktop);
                }
            }

            foreach (var pair in duplicated)
            {
                var name = pair.Key;
                var desktops = pair.Value;

                // Remove duplication by appending serial number
                int serial = 0;
                foreach (var desktop in desktops)
                {
                    // Find next unused name
                    string cand;
                    while (true)
                    {
                        serial++;
                        cand = string.Format("{0}{1}", name, serial);
                        if (!found.Contains(cand)) break;
                    }
                    desktop.Name = cand;
                }
            }
        }

        private static void EnsureNoUnnamed()
        {
            // Populate all used names
            var found = new HashSet<string>();

            foreach (var desktop in VirtualDesktop.GetDesktops())
            {
                if (string.IsNullOrEmpty(desktop.Name)) continue;
                found.Add(desktop.Name);
            }

            // If the current desktop is unnamed, name it
            // DEFAULT_NAME_HOME if it doesn't exist.
            if (string.IsNullOrEmpty(VirtualDesktop.Current.Name)
                && !found.Contains(DEFAULT_NAME_HOME))
            {
                VirtualDesktop.Current.Name = DEFAULT_NAME_HOME;
            }

            // Name it temporary; multiple workspaces having the same name is
            // ok for now. Uniqify is not my job.
            foreach (var desktop in VirtualDesktop.GetDesktops())
            {
                if (!string.IsNullOrEmpty(desktop.Name)) continue;
                desktop.Name = DEFAULT_NAME_UNNAMED;
            }
        }

        public static VirtualDesktop GetOrCreate(string name)
        {
            var desktop = VirtualDesktop
                .GetDesktops()
                .FirstOrDefault(d => d.Name == name);
            if (desktop == null)
            {
                desktop = VirtualDesktop.Create();
                desktop.Name = name;
            }
            return desktop;
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();
    }

    public static class ShiftSwitchExtension
    {
        public static void SwitchFocus(this VirtualDesktop desktop)
        {
            desktop.Switch();

            // Focus some window in the new desktop to avoid keyboard inputs
            // still captured by the previous application.
            foreach (var hwnd in desktop.AllWindows(firstOnly: true))
            {
                SetForegroundWindow(hwnd);
            }
        }

        public static void ShiftSwitch(this VirtualDesktop desktop, IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;
            try
            {
                VirtualDesktopHelper.MoveToDesktop(hWnd, desktop);
                desktop.SwitchFocus();
            }
            catch (COMException)
            {
                MessageBox.Show("Window to move not found");
            }
        }

        public static bool IsEmpty(this VirtualDesktop desktop)
            => desktop.AllWindows(firstOnly: true).Count == 0;

        public static List<IntPtr> AllWindows(
            this VirtualDesktop desktop, bool firstOnly = false)
        {
            var hwnds = new List<IntPtr>();
            bool callback(IntPtr hwnd, IntPtr _lparam)
            {
                if (VirtualDesktop.FromHwnd(hwnd) == desktop)
                {
                    hwnds.Add(hwnd);
                    // if firstOnly is true, We don't have to continue
                    // enumeration, hence return false.
                    return !firstOnly;
                }

                // Continue enumeration, as this window does not belongs not
                // the current desktop.
                return true;
            }
            _ = EnumWindows(callback, IntPtr.Zero);
            return hwnds;
        }

        private delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lparam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(
            EnumWindowsDelegate lpEnumFunc,
            IntPtr lparam);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int SendMessage(
            IntPtr hWnd,
            int Msg,
            IntPtr wParam, IntPtr lParam);

        private const int WM_KILLFOCUS = 0x0008;
    }
}
