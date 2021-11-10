using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WindowsDesktop;

namespace DynamicWorkspaceManager
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly HotKeyManager hotkey;
        private readonly State state;

        public MainWindow()
        {
            this.hotkey = new HotKeyManager(this);
            this.state = new State(Dispatcher);

            InitializeComponent();
            InitializeHotKeys();

            // Hide window, this main window is just for window handle.
            Hide();
        }

        private void InitializeHotKeys()
        {
            _ = this.hotkey.Register(
                ModifierKeys.Windows | ModifierKeys.Control, Key.X,
                this.state.SwitchToLastWorkspace);
            _ = this.hotkey.Register(
                ModifierKeys.Windows | ModifierKeys.Shift, Key.X,
                this.state.ShiftSwitchToLastWorkspace);
            _ = this.hotkey.Register(
                ModifierKeys.Windows | ModifierKeys.Control, Key.Oem3,
                this.state.SwitchToWorkspacePrompt);
            _ = this.hotkey.Register(
                ModifierKeys.Windows | ModifierKeys.Shift, Key.Oem3,
                this.state.ShiftSwitchToWorkspacePrompt);
            _ = this.hotkey.Register(
                ModifierKeys.Windows | ModifierKeys.Control, Key.H,
                this.state.SwitchToLeftWorkspace);
            _ = this.hotkey.Register(
                ModifierKeys.Windows | ModifierKeys.Shift, Key.H,
                this.state.ShiftSwitchToLeftWorkspace);
            _ = this.hotkey.Register(
                ModifierKeys.Windows | ModifierKeys.Control, Key.L,
                this.state.SwitchToRightWorkspace);
            _ = this.hotkey.Register(
                ModifierKeys.Windows | ModifierKeys.Shift, Key.L,
                this.state.ShiftSwitchToRightWorkspace);
        }
    }
}
