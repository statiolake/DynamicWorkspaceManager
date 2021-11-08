using System.Windows.Forms;

namespace DynamicWorkspaceManager
{
    public class TrayIcon
    {
        private readonly NotifyIcon NotifyIcon;

        public TrayIcon()
        {
            var iconName = System.Reflection.Assembly.GetEntryAssembly().Location;
            NotifyIcon = new NotifyIcon
            {
                Text = "DynamicWorkspaceManager",
                Icon = System.Drawing.Icon.ExtractAssociatedIcon(iconName),
            };

            // Create context menu
            var menu = new ContextMenuStrip();

            // Menu item [Quit]
            var menuExit = new ToolStripMenuItem
            {
                Text = "Quit"
            };
            menuExit.Click += (sender, e) => ShutdownApplication();
            menu.Items.Add(menuExit);

            // Register the context menu
            NotifyIcon.ContextMenuStrip = menu;

            // Show notification icon
            NotifyIcon.Visible = true;
        }

        private void ShutdownApplication()
        {
            NotifyIcon.Dispose();
            App.Current.Shutdown();
        }
    }
}