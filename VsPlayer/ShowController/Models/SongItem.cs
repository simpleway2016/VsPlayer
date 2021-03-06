﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsPlayer.ShowController.Models
{
    public class SongItem : Way.Lib.DataModel
    {

        private string _Id;
        public string Id
        {
            get => _Id;
            set
            {
                if (_Id != value)
                {
                    _Id = value;
                    this.OnPropertyChanged("Id", null, null);
                }
            }
        }

        string _Name;
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                if (_Name != value)
                {
                    _Name = value;
                    this.OnPropertyChanged("Name", null, value);
                }
            }
        }

        bool _isOpenFile;
        /// <summary>
        /// open file with system application
        /// </summary>
        public bool IsOpenFile
        {
            get
            {
                return _isOpenFile;
            }
            set
            {
                if (_isOpenFile != value)
                {
                    _isOpenFile = value;
                    this.OnPropertyChanged("IsOpenFile", null, value);
                }
            }
        }


        bool _IsShowedDetail;
        [Newtonsoft.Json.JsonIgnore]
        public bool IsShowedDetail
        {
            get
            {
                return _IsShowedDetail;
            }
            set
            {
                if (_IsShowedDetail != value)
                {
                    _IsShowedDetail = value;
                    this.OnPropertyChanged("IsShowedDetail", null, value);
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
                if (_FilePath != value)
                {
                    _FilePath = value;
                    this.OnPropertyChanged("FilePath", null, value);
                }
            }
        }

        NextStep _PlayCompletedAction = NextStep.无操作;
        public NextStep PlayCompletedAction
        {
            get
            {
                return _PlayCompletedAction;
            }
            set
            {
                if (_PlayCompletedAction != value)
                {
                    _PlayCompletedAction = value;
                    this.OnPropertyChanged("PlayCompletedAction", null, value);
                }
            }
        }

        string _PlayCompletedBgPic;
        public string PlayCompletedBgPic
        {
            get
            {
                return _PlayCompletedBgPic;
            }
            set
            {
                if (_PlayCompletedBgPic != value)
                {
                    _PlayCompletedBgPic = value;
                    this.OnPropertyChanged("PlayCompletedBgPic", null, value);
                }
            }
        }
        string _PlayingBgPic;
        /// <summary>
        /// 进行中背景图
        /// </summary>
        public string PlayingBgPic
        {
            get
            {
                return _PlayingBgPic;
            }
            set
            {
                if (_PlayingBgPic != value)
                {
                    _PlayingBgPic = value;
                    this.OnPropertyChanged("PlayingBgPic", null, value);
                }
            }
        }

        string _StopedBgPic;
        /// <summary>
        /// 手动停止背景图
        /// </summary>
        public string StopedBgPic
        {
            get
            {
                return _StopedBgPic;
            }
            set
            {
                if (_StopedBgPic != value)
                {
                    _StopedBgPic = value;
                    this.OnPropertyChanged("StopedBgPic", null, value);
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
                    ShowListWindow.instance.DataModel.ProgrammeList.FirstOrDefault(m => m.Items.Contains(this)).IsPlaying = value;
                    this.OnPropertyChanged("IsPlaying", null, value);
                }
            }
        }


        private int? _Volume;
        public int? Volume
        {
            get => _Volume;
            set
            {
                if (_Volume != value)
                {
                    _Volume = value;
                    this.OnPropertyChanged("Volume", null, null);
                }
            }
        }

        bool _IsActivedItem;
        [Newtonsoft.Json.JsonIgnore]
        public bool IsActivedItem
        {
            get
            {
                return _IsActivedItem;
            }
            set
            {
                if (_IsActivedItem != value)
                {
                    if (value)
                    {
                        var olditem = ShowListWindow.instance.GetActiveItem();
                        if (olditem != null && olditem != this)
                            olditem.IsActivedItem = false;
                       }
                    _IsActivedItem = value;
                    
                    this.OnPropertyChanged("IsActivedItem", null, value);
                }
            }
        }


        int _AudioStreamIndex;
        public int AudioStreamIndex
        {
            get
            {
                return _AudioStreamIndex;
            }
            set
            {
                if (_AudioStreamIndex != value)
                {
                    _AudioStreamIndex = value;
                    this.OnPropertyChanged("AudioStreamIndex", null, value);
                }
            }
        }

        ObservableCollection<AudioStream> _CurrentAudioStreams = new ObservableCollection<AudioStream>();
        [Newtonsoft.Json.JsonIgnore]
        public ObservableCollection<AudioStream> CurrentAudioStreams
        {
            get
            {
                return _CurrentAudioStreams;
            }
            set
            {
                if (_CurrentAudioStreams != value)
                {
                    _CurrentAudioStreams = value;
                    this.OnPropertyChanged("CurrentAudioStreams", null, value);
                }
            }
        }
        public SongItem()
        {
            this.Id = Guid.NewGuid().ToString("N");
            MediaPlayer.instance.StatusChanged += Instance_StatusChanged;
        }
        System.Diagnostics.Process _OpenFileProcess;
       public virtual void Play(MediaPlayer player)
        {
            this.IsActivedItem = true;
            if (_isOpenFile)
            {
                player.Stop();
                if (_OpenFileProcess == null)
                {
                    _OpenFileProcess = System.Diagnostics.Process.Start(this.FilePath);
                    _OpenFileProcess.EnableRaisingEvents = true;
                    _OpenFileProcess.Exited += _OpenFileProcess_Exited;
                    this.IsPlaying = true;
                }
            }
            else
            {
               if(this.Volume != null)
                {
                    ShowListWindow.instance.IsSettingVolumeByCode = true;
                    ShowListWindow.instance.DataModel.PlayerInfo.Volume = this.Volume.Value;
                    ShowListWindow.instance.IsSettingVolumeByCode = false;
                }
                player.Open(this.FilePath);
            }
        }

        private void _OpenFileProcess_Exited(object sender, EventArgs e)
        {
            _OpenFileProcess = null;
            this.IsPlaying = false;
            
            this.OnPlayCompleted();
        }

        public virtual void Stop(MediaPlayer player)
        {
            if(string.IsNullOrEmpty(this.FilePath))
            {
                player.Status = PlayerStatus.Stopped;
                this.OnPlayCompleted();
                return;
            }
            if (_isOpenFile)
            {
                if (_OpenFileProcess != null)
                {
                    ShowListWindow.instance.DataModel.ProgrammeList.FirstOrDefault(m => m.Items.Contains(this)).IsPlaying = false;
                    _OpenFileProcess.Kill();
                    _OpenFileProcess = null;
                    player.Status = PlayerStatus.Stopped;
                }
            }
            else
            {
                player.Stop();
            }
           
        }
        public virtual void Pause(MediaPlayer player)
        {
            if (_isOpenFile)
            {

            }
            else
            {
                player.Pause();
            }
            
        }

        private void Instance_StatusChanged(object sender, EventArgs e)
        {
            this.IsActivedItem = (MediaPlayer.instance.SongItem == this);

            if (this.IsActivedItem)
            {
                this.IsPlaying = (MediaPlayer.instance.Status == PlayerStatus.Running);
                if(MediaPlayer.instance.CurrentAudioStreams != this.CurrentAudioStreams)
                {
                    this.CurrentAudioStreams = MediaPlayer.instance.CurrentAudioStreams;
                }
                this.OnPropertyChanged("CurrentAudioStreams", null, null);
                this.AudioStreamIndex = MediaPlayer.instance.CurrentAudioStreamIndex;
            }
            else
            {
                this.IsPlaying = false;
            }
        }

        public SongItem(string filepath):this()
        {
            this.Name = Path.GetFileNameWithoutExtension(filepath);
            this.FilePath = filepath;
        }

        void selectBg(string pic)
        {
            if (!string.IsNullOrEmpty(pic))
            {
                var item = ShowListWindow.instance.DataModel.BgPicList.FirstOrDefault(m => m.FilePath == pic);
                if (item != null)
                {
                    item.IsSelected = true;
                }
            }
        }
        public virtual void OnBeginPlay()
        {
            if (FilePath == null)
            {
                //纯背景的
                if(string.IsNullOrEmpty(PlayingBgPic))
                {
                    //显示默认背景
                    selectBg(ShowListWindow.instance.DataModel.BgPicList.FirstOrDefault(m => m.IsDefault)?.FilePath);
                }
                else
                {
                    selectBg(this.PlayingBgPic);
                }
            }
            else
            {
                selectBg(this.PlayingBgPic);
            }
        }

        public virtual void OnStop()
        {
            selectBg(this.StopedBgPic);
        }
        public virtual void OnPlayCompleted()
        {
            var programme = ShowListWindow.instance.DataModel.ProgrammeList.FirstOrDefault(m => m.Items.Contains(this));
            if (programme == null)
                return;

            if (programme.Items[programme.Items.Count - 1] == this)
            {
                //如果是最后一个，而且this.PlayCompletedBgPic又是空，那么，背景回到默认
                if (string.IsNullOrEmpty(this.PlayCompletedBgPic))
                {
                    selectBg(ShowListWindow.instance.DataModel.BgPicList.FirstOrDefault(m=>m.IsDefault)?.FilePath);
                }
            }

            if(this.PlayCompletedAction == NextStep.循环播放)
            {
                ShowListWindow.instance.SongItemClickPlay(this);
            }
            else if (this.PlayCompletedAction == NextStep.播放下一曲)
            {
                 var nextIndex = programme.Items.IndexOf(this) + 1;
                if(nextIndex < programme.Items.Count)
                    ShowListWindow.instance.SongItemClickPlay(programme.Items[nextIndex]);

            }
            else if (this.PlayCompletedAction == NextStep.显示背景图)
            {
                selectBg(this.PlayCompletedBgPic);

            }
            else
            {
                if (programme.IsLoopPlay)
                {
                    if (this == programme.Items.Last())
                    {
                        //播放节目第一首
                        ShowListWindow.instance.SongItemClickPlay(programme.Items[0]);
                    }
                    else
                    {
                        //播放下一首
                        ShowListWindow.instance.SongItemClickPlay(programme.Items[programme.Items.IndexOf(this) + 1]);
                    }
                }
            }
        }
    }
}
