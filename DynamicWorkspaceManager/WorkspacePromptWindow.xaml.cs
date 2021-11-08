using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DynamicWorkspaceManager
{
    /// <summary>
    /// Window1.xaml の相互作用ロジック
    /// </summary>
    public partial class WorkspacePromptWindow : Window
    {
        public WorkspacePromptWindow()
        {
            Closed += (sender, e) =>
            {
                lock (_SelfLock)
                {
                    if (Self == this) Self = null;
                }

                // If no result was set before closing, operation was not
                // "accepted", i.e. cancelled. Otherwise, I expect this
                // `TrySetCanceled()` silently fails.
                this.futureResult.TrySetException(
                    new OperationCanceledException());
            };

            Vm = new WorkspacePromptViewModel();
            Vm.Accepted += (name) =>
            {
                this.futureResult.SetResult(name);
                Close();
            };
            Vm.Rejected += () => Close();

            DataContext = Vm;

            InitializeComponent();
        }

        private readonly TaskCompletionSource<string> futureResult
            = new TaskCompletionSource<string>();

        // Close when deactivated
        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            if (IsVisible) Close();
        }

        private static readonly object _SelfLock = new object();
        private static WorkspacePromptWindow Self = null;

        public static Task<string> GetUserInput()
        {
            // If not opened, previous window still exists. The user input
            // should be handled by the previous call of GetUserInput(), so for
            // this call we simply throws OperationCanceledException.
            if (!Open())
            {
                return Task.FromException<string>(
                    new OperationCanceledException());
            }

            // If the window newly opened, return the associated Task.
            return Self.futureResult.Task;
        }

        public static bool Open()
        {
            lock (_SelfLock)
            {
                if (Self == null)
                {
                    Self = new WorkspacePromptWindow();
                    Self.Show();
                    Self.Activate();
                    return true;
                }
                else
                {
                    Self.Activate();
                    return false;
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
