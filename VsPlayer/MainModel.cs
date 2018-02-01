using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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
                            if (MediaPlayer.instance.HasVideo)
                            {
                                MediaPlayer.instance.Visible = true;
                            }
                            this.PlayButtonImage = "images/pause.png";
                            break;
                        case PlayState.Stopped:
                            MediaPlayer.instance.Visible = false;
                            this.CurrentPosition = 0;
                            this.TotalSeconds = 0;
                            this.PlayButtonImage = "images/play.png";
                            break;
                    }
                    OnPropertyChange("State");
                    OnPropertyChange("PlayButtonImage");
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

        bool _IsSetLastTimeVolume;
        public bool IsSetLastTimeVolume
        {
            get
            {
                return _IsSetLastTimeVolume;
            }
            set
            {
                if (_IsSetLastTimeVolume != value)
                {
                    _IsSetLastTimeVolume = value;                   
                    this.OnPropertyChange("IsSetLastTimeVolume");
                }
            }

        }
        bool _IsVideoStretchMode;
        public bool IsVideoStretchMode
        {
            get
            {
                return _IsVideoStretchMode;
            }
            set
            {
                if (_IsVideoStretchMode != value)
                {
                    _IsVideoStretchMode = value;
                    this.OnPropertyChange("IsVideoStretchMode");
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

        int _Volumn = -10000;
        public int Volumn
        {
            get
            {
                return _Volumn;
            }
            set
            {
                if (value > 0)
                    value = 0;
                else if (value < -10000)
                    value = -10000;

                if (_Volumn != value)
                {
                    _Volumn = value;
                    this.OnPropertyChange("Volumn");
                }
            }

        }

        Thickness _VolumnPointLocation = new Thickness(-3, 7, 0, 0);
        public Thickness VolumnPointLocation
        {
            get
            {
                return _VolumnPointLocation;
            }
            set
            {
                if (_VolumnPointLocation.Left != value.Left)
                {
                    _VolumnPointLocation = value;
                    this.OnPropertyChange("VolumnPointLocation");
                }
            }

        }

        static List<int> volumes = new List<int>( new int[]{-10000,-6418,-6147,-6000,
        -5892,-4826,-4647,-4540
        -4477, -4162,-3876, -3614, -3500,
        -3492,-3374,-3261,-3100,-3153,-3048,-2947,-2849,-2755,-2700,
        -2663,-2575,-2520,-2489,-2406,-2325,-2280,-2246,-2170,-2095,-2050,
        -2023,-1952,-1900, -1884,-1834, -1820, -1800,-1780, -1757,-1695,-1636,-1579,
        -1521,-1500,-1464,-1436,-1420, -1408,-1353,-1299,-1246,-1195,-1144,
        -1096,-1060, -1049,-1020,-1003,-957,-912,-868, -800, -774,-784, -760, -744,
        -705,-667,-630,-610,-594,-570 ,-558,-525,-493,-462,-432,-403,
        -375,-348,-322,-297,-285, -273,-250,-228,-207,-187,-176, -168,
        -150,-102,-75,-19,-10,0,0});

        int _VolumnBgWidth = 0;
        public int VolumnBgWidth
        {
            get
            {
                return _VolumnBgWidth;
            }
            set
            {
                if (value > 77) value = 77;
                else if (value < 0) value = 0;

                if (_VolumnBgWidth != value)
                {
                    _VolumnBgWidth = value;
                    this.VolumnPointLocation = new Thickness( value - 3 , 7,0,0);
                    int index = (int)((volumes.Count - 1) * (value / 77.0));
                    if (index < 0)
                        index = 0;
                    else if (index >= volumes.Count)
                        index = volumes.Count - 1;
                    this.Volumn = volumes[index];
                    this.OnPropertyChange("VolumnBgWidth");
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

        //
        double _PlayerListWidth = 0;
        public double PlayerListWidth
        {
            get
            {
                return _PlayerListWidth;
            }
            set
            {
                if (_PlayerListWidth != value)
                {
                    _PlayerListWidth = value;
                    this.OnPropertyChange("PlayerListWidth");
                }
            }
        }

        double _BackgroundListWidth = 0;
        public double BackgroundListWidth
        {
            get
            {
                return _BackgroundListWidth;
            }
            set
            {
                if (_BackgroundListWidth != value)
                {
                    _BackgroundListWidth = value;
                    this.OnPropertyChange("BackgroundListWidth");
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

        
            bool _ShowSerialNumber;
        public bool ShowSerialNumber
        {
            get
            {
                return _ShowSerialNumber;
            }
            set
            {
                if (_ShowSerialNumber != value)
                {
                    _ShowSerialNumber = value;
                    this.OnPropertyChange("ShowSerialNumber");
                    foreach( var item in this.PlayList )
                    {
                        item.OnPropertyChange("Text");
                    }
                    foreach (var item in this.BackgroundList)
                    {
                        item.OnPropertyChange("Text");
                    }
                }
            }
        }

        bool _IsSingleLoop;
        public bool IsSingleLoop
        {
            get
            {
                return _IsSingleLoop;
            }
            set
            {
                if (_IsSingleLoop != value)
                {
                    _IsSingleLoop = value;
                    if (value)
                        this.IsListLoop = false;
                    this.OnPropertyChange("IsSingleLoop");
                }
            }
        }

        
        bool _IsAutoMuteVolumeOnStop;
        public bool IsAutoMuteVolumeOnStop
        {
            get
            {
                return _IsAutoMuteVolumeOnStop;
            }
            set
            {
                if (_IsAutoMuteVolumeOnStop != value)
                {
                    _IsAutoMuteVolumeOnStop = value;
                    this.OnPropertyChange("IsAutoMuteVolumeOnStop");
                }
            }
        }

        bool _IsListLoop;
        public bool IsListLoop
        {
            get
            {
                return _IsListLoop;
            }
            set
            {
                if (_IsListLoop != value)
                {
                    _IsListLoop = value;
                    if (value)
                        this.IsSingleLoop = false;
                    this.OnPropertyChange("IsListLoop");
                }
            }
        }

        public ObservableCollection<PlayListItemModel> PlayList
        {
            get;
            set;
        }
        public ObservableCollection<PlayListItemModel> BackgroundList
        {
            get;
            set;
        }
        public MainModel()
        {
            this.PlayButtonImage = "images/play.png";
            this.PlayList = new ObservableCollection<PlayListItemModel>();
            this.BackgroundList = new ObservableCollection<PlayListItemModel>();
        }
        public void SetVolume(int volume)
        {
            try
            {
                var index = volumes.IndexOf(volume);
                this.VolumnBgWidth = (index * 77) / (volumes.Count - 1);
                this.Volumn = volumes[index];
            }
            catch
            {

            }
        }
        public void DownVolume()
        {
            try
            {
                var index = new List<int>(volumes).IndexOf(this.Volumn) - 1;
                this.VolumnBgWidth = (index * 77) / (volumes.Count - 1);
                this.Volumn = volumes[index];
            }
            catch
            {

            }
        }
        public void UpVolume()
        {
            try
            {
                var index = new List<int>(volumes).IndexOf(this.Volumn) + 1;
                if (index < volumes.Count)
                {
                    this.VolumnBgWidth = (index * 77) / (volumes.Count - 1);
                    this.Volumn = volumes[index];
                }
            }
            catch
            {

            }
        }
        void updatePlayingPercent()
        {
            var total = this.TotalSeconds;
            int left = 0;
            if (total > 0)
            {
                left = (this.CurrentPosition * 100) / this.TotalSeconds;
                if (left > 100)
                    left = 100;
              
            }
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
