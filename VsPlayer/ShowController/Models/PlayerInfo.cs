using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsPlayer.ShowController.Models
{
    public class PlayerInfo:Way.Lib.DataModel
    {

        double _TotalSeconds;
        [Newtonsoft.Json.JsonIgnore]
        public double TotalSeconds
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
                    this.TotalSecondString = GetSecondString(value);
                    this.OnPropertyChanged("TotalSeconds", null, value);
                    this.OnPropertyChanged("SecondPercents", null, value);
                }
            }
        }

        double _CurrentSeconds;
        [Newtonsoft.Json.JsonIgnore]
        public double CurrentSeconds
        {
            get
            {
                return _CurrentSeconds;
            }
            set
            {
                if (_CurrentSeconds != value)
                {
                    _CurrentSeconds = value;
                    if (!this.IsMovingSecond)
                    {
                        this.CurrentSecondString = GetSecondString(value);
                    }
                    this.OnPropertyChanged("CurrentSeconds", null, value);
                    this.OnPropertyChanged("SecondPercents", null, value);
                }
            }
        }

        /// <summary>
        /// 第一个元素是播放进度百分比
        /// 第二个元素是剩余进度百分比
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public string[] SecondPercents
        {
            get
            {
                try
                {
                    
                    var percent = CurrentSeconds / TotalSeconds;
                    if(double.IsNaN(percent))
                    {
                        throw new Exception("nan");
                    }
                    return new string[] {
                        percent + "*",
                        (1 - percent) + "*"
                    };
                }
                catch
                {
                    return new string[] {
                        "0",
                        "1*"
                    };
                }
            }
        }

        public int Volume
        {
            get
            {
                return MediaPlayer.instance.GetVolume();
            }
            set
            {
                if (MediaPlayer.instance.GetVolume() != value)
                {
                    MediaPlayer.instance.SetVolume(value);
                    this.OnPropertyChanged("Volume", null, value);
                    this.OnPropertyChanged("VolumePercents", null, value);
                }
            }
        }

        [Newtonsoft.Json.JsonIgnore]
        public string[] VolumePercents
        {
            get
            {
                try
                {
                    int index = 0;
                    var volume = MediaPlayer.instance.GetVolume();
                    for(int i = 0; i < Controls.VolumeControl.volumes.Count; i ++)
                    {
                        if(volume <= Controls.VolumeControl.volumes[i])
                        {
                            index = i;
                            break;
                        }
                    }
                  

                    var percent = index / Convert.ToDouble(Controls.VolumeControl.volumes.Count - 1);

                    if (double.IsNaN(percent))
                    {
                        throw new Exception("nan");
                    }
                    return new string[] {
                        percent + "*",
                        (1 - percent) + "*"
                    };
                }
                catch
                {
                    return new string[] {
                        "0",
                        "1*"
                    };
                }
            }
        }

        bool _IsPlaying;
        [Newtonsoft.Json.JsonIgnore]
        public bool IsPlaying
        {
            get
            {
                return _IsPlaying;
            }
            set
            {
                if (_IsPlaying != value)
                {
                    _IsPlaying = value;
                    this.OnPropertyChanged("IsPlaying", null, value);
                }
            }
        }

        string _CurrentSecondString;
        [Newtonsoft.Json.JsonIgnore]
        public string CurrentSecondString
        {
            get
            {
                return _CurrentSecondString??"00:00:00";
            }
            set
            {
                if (_CurrentSecondString != value)
                {
                    _CurrentSecondString = value;
                    this.OnPropertyChanged("CurrentSecondString", null, value);
                }
            }
        }
        [Newtonsoft.Json.JsonIgnore]
        public string CurrentSecondColor
        {
            get
            {
                return this.IsMovingSecond ? "#5091e4" : "#000";
            }
        }
        string _TotalSecondString;
        [Newtonsoft.Json.JsonIgnore]
        public string TotalSecondString
        {
            get
            {
                return _TotalSecondString ?? "00:00:00";
            }
            set
            {
                if (_TotalSecondString != value)
                {
                    _TotalSecondString = value;
                    this.OnPropertyChanged("TotalSecondString", null, value);
                }
            }
        }

        bool _IsMovingSecond;
        /// <summary>
        /// 是否正在移动播放进度
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool IsMovingSecond
        {
            get
            {
                return _IsMovingSecond;
            }
            set
            {
                if (_IsMovingSecond != value)
                {
                    _IsMovingSecond = value;
                    this.OnPropertyChanged("IsMovingSecond", null, value);
                    this.OnPropertyChanged("CurrentSecondColor", null, value);
                }
            }
        }
        public PlayerInfo()
        {
            MediaPlayer.instance.StatusChanged += Instance_StatusChanged;
        }

        public static string GetSecondString(double s)
        {
            int value = Convert.ToInt32(s);
               var hour = value / 3600;
            value = value % 3600;
            var minute = value / 60;
            var second = value % 60;
            return hour.ToString().PadLeft(2, '0') + ":" + minute.ToString().PadLeft(2, '0') + ":" + second.ToString().PadLeft(2, '0');
        }

        private void Instance_StatusChanged(object sender, EventArgs e)
        {
            this.IsPlaying = (MediaPlayer.instance.Status == PlayerStatus.Running);
        }

    }
}
