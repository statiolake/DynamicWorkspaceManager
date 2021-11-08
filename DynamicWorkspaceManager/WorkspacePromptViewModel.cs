using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using WindowsDesktop;

namespace DynamicWorkspaceManager
{
    public class WorkspacePromptViewModel : INotifyPropertyChanged
    {
        public List<string> Desktops
        {
            get => VirtualDesktop.GetDesktops().Select(d => d.Name).ToList();
        }

        public AutoCompleteFilterPredicate<object> DesktopFilter
        {
            get => (query, obj) =>
            {
                if (!(obj is string name)) return false;
                if (name == null) return false;
                return name.Contains(query);
            };
        }

        private string selectedName;
        public string SelectedName
        {
            get => this.selectedName;
            set
            {
                if (ReferenceEquals(this.selectedName, value)) return;
                this.selectedName = value;
                RaisePropertyChanged();
            }
        }

        private string boxText;
        public string BoxText
        {
            get => this.boxText;
            set
            {
                if (this.boxText == value) return;
                this.boxText = value;
                RaisePropertyChanged();
            }
        }

        public bool Shift { get; }

        public IntPtr ForegroundWindow { get; }

        public string WindowTitle
        {
            get => Shift ? "Shift to workspace" : "Move to workspace";
        }

        public void EnterPressed()
        {
            if (string.IsNullOrEmpty(this.BoxText))
            {
                Rejected?.Invoke();
                return;
            }

            Accepted?.Invoke(this.BoxText);
        }

        public void TabPressed()
        {
            // TODO: Get completion?
        }

        public void EscapePressed()
        {
            Rejected?.Invoke();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([CallerMemberName] string caller = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(caller));
        }

        public event Action<string> Accepted;
        public event Action Rejected;
    }
}
