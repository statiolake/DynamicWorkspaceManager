using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace DynamicWorkspaceManager
{
    /// <summary>
    /// Window1.xaml の相互作用ロジック
    /// </summary>
    public partial class WorkspacePromptWindow : Window
    {
        private bool Shift;
        private IntPtr ForegroundWindow;

        public WorkspacePromptWindow(bool shift, IntPtr foregroundWindow)
        {
            Shift = shift;
            ForegroundWindow = foregroundWindow;

            Closed += (sender, e) =>
            {
                lock (_SelfLock)
                {
                    if (Self == this) Self = null;
                }
            };

            Vm = new WorkspacePromptViewModel(shift, foregroundWindow);
            Vm.RequestClose += () => Close();
            DataContext = Vm;
            InitializeComponent();
        }

        // Close when deactivated
        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            if (IsVisible) Close();
        }

        private static readonly object _SelfLock = new object();
        private static WorkspacePromptWindow Self = null;
        public static void Open(bool shift, IntPtr foregroundWindow)
        {
            lock (_SelfLock)
            {
                if (Self != null && Self.Shift != shift)
                {
                    foregroundWindow = Self.ForegroundWindow;
                    Self.Close();
                    Self = null;
                }

                if (Self == null)
                {
                    Self = new WorkspacePromptWindow(shift, foregroundWindow);
                    Self.Show();
                    Self.Activate();
                }
                else
                {
                    Self.Activate();
                }
            }
        }


        public WorkspacePromptViewModel Vm { get; }

        // FIXME: Little hacky.
        private void TextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Vm.EnterPressed();
            }
            if (e.Key == Key.Tab)
            {
                Vm.TabPressed();
            }
            if (e.Key == Key.Escape)
            {
                Vm.EscapePressed();
            }
        }
    }
}
