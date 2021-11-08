using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WindowsDesktop.Internal;

namespace DynamicWorkspaceManager
{
    /// <summary>
    /// CurrentDesktopNotifierWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class CurrentDesktopNotifierPopup : Popup
    {
        private readonly CurrentDesktopNotifierViewModel viewModel;
        public CurrentDesktopNotifierPopup()
        {
            this.viewModel = new CurrentDesktopNotifierViewModel();
            this.DataContext = this.viewModel;
            InitializeComponent();
        }

        private static readonly object selfLock = new object();
        private static CurrentDesktopNotifierPopup self = null;
        public static IDisposable Open(string currentDesktopName)
        {
            lock (selfLock)
            {
                if (self != null)
                {
                    self.viewModel.CurrentDesktopName = currentDesktopName;
                }
                else
                {
                    self = new CurrentDesktopNotifierPopup();
                }
                self.viewModel.CurrentDesktopName = currentDesktopName;
                self.IsOpen = true;
            }

            lock (numShowingLock) numShowing++;
            return Disposable.Create(() =>
            {
                lock (numShowingLock)
                {
                    lock (selfLock)
                    {
                        numShowing--;
                        if (numShowing == 0)
                        {
                            self.IsOpen = false;
                            self = null;
                        }
                    }
                }
            });
        }

        private static readonly object numShowingLock = new object();
        private static int numShowing = 0;
    }
}
