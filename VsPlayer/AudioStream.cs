using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsPlayer
{
    public class AudioStream : INotifyPropertyChanged
    {
        void OnPropertyChange(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        bool _IsChecked;
        public bool IsChecked
        {
            get
            {
                return _IsChecked;
            }
            set
            {
                if(_IsChecked != value)
                {
                    _IsChecked = value;
                    OnPropertyChange("IsChecked");
                }
            }
        }
        public string Name
        {
            get;
            set;
        }
        public int Index
        {
            get;
            set;
        }

        

        public AudioStream()
        {
           
        }
    }
}
