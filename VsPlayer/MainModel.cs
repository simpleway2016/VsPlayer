using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsPlayer
{
    public enum PlayState
    {
        Playing = 1,
        Paused = 2,
        Stopped = 3
    }
    public class MainModel : INotifyPropertyChanged
    {
        PlayState _State = PlayState.Stopped;
        public PlayState State
        {
            get
            {
                return _State;
            }
            set
            {
                if (_State != value)
                {
                    _State = value;
                    switch (value)
                    {
                        case PlayState.Paused:
                            this.PlayButtonImage = "images/play.png";
                            break;
                        case PlayState.Playing:
                            this.PlayButtonImage = "images/pause.png";
                            break;
                        case PlayState.Stopped:
                            this.PlayButtonImage = "images/play.png";
                            break;
                    }
                    OnPropertyChange("State");
                }
            }
        }

        public string PlayButtonImage
        {
            get;
            set;
        }

        int _CurrentPosition;
        public int CurrentPosition
        {
            get
            {
                return _CurrentPosition;
            }
            set
            {
                if(_CurrentPosition != value)
                {
                    _CurrentPosition = value;
                    updatePlayingPercent();

                    var hour = value / 3600;
                    value = value % 3600;
                    var minute = value / 60;
                    var second = value % 60;
                    this.CurrentPositionText = hour.ToString().PadLeft(2, '0') + ":" + minute.ToString().PadLeft(2, '0') + ":" + second.ToString().PadLeft(2, '0');
                    this.OnPropertyChange("CurrentPosition");
                }
            }

        }
        string _CurrentPositionText = "00:00:00";
        public string CurrentPositionText
        {
            get
            {
                return _CurrentPositionText;
            }
            set
            {
                if (_CurrentPositionText != value)
                {
                    _CurrentPositionText = value;
                    this.OnPropertyChange("CurrentPositionText");
                }
            }

        }
        int _TotalSeconds;
        public int TotalSeconds
        {
            get
            {
                return _TotalSeconds;
            }
            set
            {
                if (_TotalSeconds != value)
                {
                    _TotalSeconds = value;
                    if (_CurrentPosition > value)
                        this.CurrentPosition = 0;

                    updatePlayingPercent();

                    var hour = value / 3600;
                    value = value % 3600;
                    var minute = value / 60;
                    var second = value % 60;
                    this.TotalSecondsText = hour.ToString().PadLeft(2, '0') + ":" + minute.ToString().PadLeft(2, '0') + ":" + second.ToString().PadLeft(2, '0');
                    this.OnPropertyChange("TotalSeconds");
                }
            }

        }
        string _TotalSecondsText = "00:00:00";
        public string TotalSecondsText
        {
            get
            {
                return _TotalSecondsText;
            }
            set
            {
                if (_TotalSecondsText != value)
                {
                    _TotalSecondsText = value;
                    this.OnPropertyChange("TotalSecondsText");
                }
            }

        }

        string _PlayingPercentText = "0*";
        public string PlayingPercentText
        {
            get
            {
                return _PlayingPercentText;
            }
            set
            {
                if (_PlayingPercentText != value)
                {
                    _PlayingPercentText = value;
                    this.OnPropertyChange("PlayingPercentText");
                }
            }
        }
        string _PlayingPercentText2 = "100*";
        public string PlayingPercentText2
        {
            get
            {
                return _PlayingPercentText2;
            }
            set
            {
                if (_PlayingPercentText2 != value)
                {
                    _PlayingPercentText2 = value;
                    this.OnPropertyChange("PlayingPercentText2");
                }
            }
        }

        public ObservableCollection<PlayListItemModel> PlayList
        {
            get;
            set;
        }

        public MainModel()
        {
            this.PlayButtonImage = "images/play.png";
            this.PlayList = new ObservableCollection<PlayListItemModel>();

            this.TotalSeconds = 138;
            this.CurrentPosition = 32;
        }

        void updatePlayingPercent()
        {
           var left =  (this.CurrentPosition*100) / this.TotalSeconds;
            if (left > 100)
                left = 100;
            this.PlayingPercentText = left + "*";
            this.PlayingPercentText2 = (100 - left) + "*";
        }

        void OnPropertyChange(string name)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
