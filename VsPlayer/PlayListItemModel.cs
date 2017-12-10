using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsPlayer
{
    public class PlayListItemModel : INotifyPropertyChanged
    {
        string _FontColor = "#89898d";
        public string FontColor
        {
            get
            {
                return _FontColor;
            }
            set
            {
                if (_FontColor != value)
                {
                    _FontColor = value;
                   
                    OnPropertyChange("FontColor");
                }
            }
        }

        string _FilePath;
        public string FilePath
        {
            get
            {
                return _FilePath;
            }
            set
            {
                if(_FilePath != value)
                {
                    _FilePath = value;
                    this.Name = System.IO.Path.GetFileName(value);
                    OnPropertyChange("FilePath");
                    OnPropertyChange("Text");
                }
            }
        }

        public string Name
        {
            get;
            set;
        }

        public string Text
        {
            get
            {
                return $"{(this.Continer.IndexOf(this) + 1)}.{this.Name}";
            }
        }

        string _BgColor;
        public string BgColor
        {
            get
            {
                return _BgColor;
            }
            set
            {
                if (_BgColor != value)
                {
                    _BgColor = value;
                    OnPropertyChange("BgColor");
                }
            }
        }

        ObservableCollection<PlayListItemModel> Continer;
        public PlayListItemModel(ObservableCollection<PlayListItemModel> container)
        {
            this.Continer = container;
        }

        void OnPropertyChange(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
