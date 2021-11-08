using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using WindowsDesktop;

namespace DynamicWorkspaceManager
{
    public class State
    {
        private const string DEFAULT_NAME_UNNAMED = "work";
        private const string DEFAULT_NAME_HOME = "home";

        private VirtualDesktop lastWorkspace = null;

        public State()
        {
            InitializeWorkspaces();

            VirtualDesktop.CurrentChanged += (sender, args) =>
            {
                this.lastWorkspace = args.OldDesktop;
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
            => GetOrCreate(name).RemoveSwitch();

        public void ShiftSwitch(string name)
            => GetOrCreate(name).ShiftSwitch(GetForegroundWindow());

        public async void SwitchToWorkspacePrompt()
        {
            var name = await WorkspacePromptWindow.GetUserInput();
            var desktop = GetOrCreate(name);
            desktop.Switch();
        }

        public async void ShiftSwitchToWorkspacePrompt()
        {
            var name = await WorkspacePromptWindow.GetUserInput();
            var desktop = GetOrCreate(name);
            desktop.ShiftSwitch(GetForegroundWindow());
        }

        public void SwitchToLastWorkspace()
            => this.lastWorkspace?.RemoveSwitch();

        public void ShiftSwitchToLastWorkspace()
            => this.lastWorkspace?.ShiftSwitch(GetForegroundWindow());

        public void SwitchToLeftWorkspace()
            => VirtualDesktop.Current.GetLeft()?
                .RemoveSwitch();

        public void ShiftSwitchToLeftWorkspace()
            => VirtualDesktop.Current.GetLeft()?
                .ShiftSwitch(GetForegroundWindow());

        public void SwitchToRightWorkspace()
            => VirtualDesktop.Current.GetRight()?
                .RemoveSwitch();

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
        public static void RemoveSwitch(this VirtualDesktop desktop)
        {
            // Remove current if empty
            if (VirtualDesktop.Current.IsEmpty())
            {
                try
                {
                    VirtualDesktop.Current.Remove(desktop);
                }
                catch (ArgumentException)
                {
                    // sometimes already removed; ignore it
                }
            }
            else
            {
                desktop.Switch();
            }
        }

        public static void ShiftSwitch(this VirtualDesktop desktop, IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) return;
            try
            {
                VirtualDesktopHelper.MoveToDesktop(hWnd, desktop);
                desktop.RemoveSwitch();
            }
            catch (COMException)
            {
                MessageBox.Show("Window to move not found");
            }
        }

        public static bool IsEmpty(this VirtualDesktop desktop)
        {
            bool found = false;
            bool callback(IntPtr hwnd, IntPtr _lparam)
            {
                if (VirtualDesktop.FromHwnd(hwnd) == desktop)
                {
                    found = true;
                    // We don't have to continue enumeration, hence return false.
                    return false;
                }

                // Continue enumeration, as this window does not belongs not
                // the current desktop.
                return true;
            }
            _ = EnumWindows(callback, IntPtr.Zero);
            return !found;
        }

        private delegate bool EnumWindowsDelegate(IntPtr hWnd, IntPtr lparam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(
            EnumWindowsDelegate lpEnumFunc,
            IntPtr lparam);
    }
}
