using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWorkspaceManager
{
    public class CurrentDesktopNotifierViewModel : INotifyPropertyChanged
    {
        private string currentDesktopName;
        public string CurrentDesktopName
        {
            get => this.currentDesktopName;
            set
            {
                if (this.currentDesktopName == value) return;
                this.currentDesktopName = value;
                RaisePropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
