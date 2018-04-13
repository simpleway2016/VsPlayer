using DirectShow;
using MediaFoundation;
using MediaFoundation.EVR;
using Sonic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VideoConnectLib.Control.Encoding;
using Way.Media;

namespace VsPlayer
{
    public enum PlayerStatus
    {
        Stopped = 0,
        Running = 1,
        Paused = 2
    }
    public class MediaPlayer : System.Windows.Forms.Control
    {
        public static MediaPlayer instance;

        PlayerStatus _Status = PlayerStatus.Stopped;
        public event EventHandler StatusChanged;
        public PlayerStatus Status
        {
            get
            {
                return _Status;
            }
            set
            {
                if (_Status != value)
                {
                    _Status = value;
                    if (this.StatusChanged != null)
                        this.StatusChanged(this, null);
                }
            }
        }
        public ShowController.Models.SongItem SongItem;
        public event EventHandler PlayCompleted;
        public event EventHandler Stopped;
        public delegate void ProgressChangedHandler(object sender, double totalSeconds, double currentSeconds);
        public event ProgressChangedHandler ProgressChanged;
        internal MediaBuilder _mediaBuilder;
        IMFVideoDisplayControl EvrDisplayControl;
        internal ObservableCollection<AudioStream> CurrentAudioStreams
        {
            get;
            private set;
        }

        int _CurrentAudioStreamIndex = 0;
        internal int CurrentAudioStreamIndex
        {
            get
            {
                return _CurrentAudioStreamIndex;
            }
            set
            {
                _CurrentAudioStreamIndex = value;
                try
                {
                    CurrentAudioStreams.Where(m => m.IsChecked && m.Index != value).FirstOrDefault().IsChecked = false;
                    CurrentAudioStreams.Where(m => m.Index == value).FirstOrDefault().IsChecked = true;
                    //_mediaBuilder.StopForPinReconnect();
                    //var pin = _audioFilter.InputPin.ConnectedTo;
                    //_audioFilter.InputPin.Disconnect();
                    //_audioFilter.OutputPin.Disconnect();
                    if(SongItem != null)
                    {
                        SongItem.AudioStreamIndex = value;
                    }
                    if (_streamSelect != null)
                    {
                        _streamSelect.Enable(value, DirectShowLib.AMStreamSelectEnableFlags.Enable);
                    }

                    //_audioFilter.InputPin.Connect(pin).Throw();
                    //_audioFilter.OutputPin.Render().Throw();

                    //_mediaBuilder.Run();
                }
                catch
                {

                }
            }
        }

        bool _HasVideo = false;
        public bool HasVideo
        {
            get
            {
                return _HasVideo;
            }
            private set
            {
                if (_HasVideo != value)
                {
                    _HasVideo = value;
                }
            }
        }

        bool _IsVideoStretchMode = false;
        internal bool IsVideoStretchMode
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
                    try
                    {
                        EvrDisplayControl.SetAspectRatioMode(value ? MFVideoAspectRatioMode.None : MFVideoAspectRatioMode.PreservePixel);
                    }
                    catch
                    {

                    }
                }
            }
        }
        public MediaPlayer()
        {
            instance = this;
            _mediaBuilder = new MediaBuilder();
            this.CurrentAudioStreams = new ObservableCollection<AudioStream>();

            _mediaBuilder.DoOpenFileWithSplitter += _mediaBuilder_DoOpenFileWithSplitter;
            _mediaBuilder.DoDecodeAudio += _mediaBuilder_DoDecodeAudio;
            _mediaBuilder.DoDecodeVideo += _mediaBuilder_DoDecodeVideo;
            _mediaBuilder.DoRenderPin += _mediaBuilder_DoRenderPin;
            _mediaBuilder.GetedEventCode += _mediaBuilder_GetedEventCode;

            Task.Run(() => checkSeconds());
        }

        void checkSeconds()
        {
            while (true)
            {
                Thread.Sleep(1000);
                if (this.Status == PlayerStatus.Running)
                {
                    var total = this.GetDuration();
                    var current = this.GetCurrentPosition();
                    if (this.ProgressChanged != null)
                    {
                        this.ProgressChanged(this, total, current);
                    }
                }
                else if (this.Status == PlayerStatus.Stopped)
                {
                    if (this.ProgressChanged != null)
                    {
                        this.ProgressChanged(this, 0, 0);
                    }
                }
            }
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            if (EvrDisplayControl != null)
                EvrDisplayControl.SetVideoPosition(new MFVideoNormalizedRect(0, 0, 1, 1), new MediaFoundation.Misc.MFRect(0, 0, this.Width, this.Height));
        }
        private void _mediaBuilder_GetedEventCode(EventCode lEventCode, int lparam1, int lParam2)
        {
            if (lEventCode == EventCode.Complete)
            {
                this.Status = PlayerStatus.Stopped;
                _streamSelect = null;
               
                if (PlayCompleted != null)
                {
                    PlayCompleted(this, null);
                }
                SongItem?.OnPlayCompleted();
            }
        }

        public void Pause()
        {
            _mediaBuilder.Pause();
            this.Status = PlayerStatus.Paused;
        }
        public void Play()
        {
            _mediaBuilder.Run();
            this.Status = PlayerStatus.Running;
        }

        private DSFilter _mediaBuilder_DoDecodeVideo(DSPin pin)
        {
            DSFilter lavvideo = new DSFilter(MediaFactory.GetLAVVideo() as IBaseFilter); //new ffdshowAudioDec() as IBaseFilter;
            _mediaBuilder.AddFilter(lavvideo, "lav video");
            MediaFactory.EnableLavHardwareDecode((DirectShowLib.IBaseFilter)lavvideo.Value);
            HRESULT hr = pin.Connect(lavvideo.InputPin);
            hr.Throw();
            return lavvideo;
        }

        private void _mediaBuilder_DoRenderPin(DSPin pin)
        {
            if (pin.IsConnected || pin.MediaTypes.FirstOrDefault() == null)
                return;
            if (pin.MediaTypes.FirstOrDefault().majorType == DirectShow.MediaType.Video)
            {
                Sonic.DSFilter videorender = new Sonic.DSFilter(new Guid("{FA10746C-9B63-4B6C-BC49-FC300EA5F256}"));
                _mediaBuilder.AddFilter(videorender, "videorender");
                pin.Connect(videorender.InputPin).Throw();


                IMFGetService _service = videorender.Value as IMFGetService;
                IntPtr _object;
                _service.GetService(MFServices.MR_VIDEO_RENDER_SERVICE, typeof(IMFVideoDisplayControl).GUID, out _object);
                EvrDisplayControl = (IMFVideoDisplayControl)Marshal.GetObjectForIUnknown(_object);

                EvrDisplayControl.SetVideoWindow(this.Handle);
                EvrDisplayControl.SetVideoPosition(new MFVideoNormalizedRect(0, 0, 1, 1), new MediaFoundation.Misc.MFRect(0, 0, this.Width, this.Height));
                EvrDisplayControl.SetAspectRatioMode(this.IsVideoStretchMode ? MFVideoAspectRatioMode.None : MFVideoAspectRatioMode.PreservePixel);

                this.HasVideo = true;
            }
            else
            {
                _mediaBuilder.RenderPin(pin);
            }
        }

        DSFilter _audioFilter;
        private Sonic.DSFilter _mediaBuilder_DoDecodeAudio(Sonic.DSPin pin)
        {
            _audioFilter = new DSFilter(MediaFactory.GetAC3Filter() as IBaseFilter); //new ffdshowAudioDec() as IBaseFilter;
            _mediaBuilder.AddFilter(_audioFilter, "audioDecode");
            HRESULT hr = pin.Connect(_audioFilter.InputPin);
            hr.Throw();
            return _audioFilter;
        }

        DirectShowLib.IAMStreamSelect _streamSelect;
        private Sonic.DSFilter _mediaBuilder_DoOpenFileWithSplitter(string filepath)
        {
            var filter = (DirectShow.IBaseFilter)MediaFactory.LAVSplitterSource();
            var splitter = new Sonic.DSFilter(filter);

            _mediaBuilder.AddFilter(splitter, "splitter");

            DirectShow.IFileSourceFilter source = splitter.Value as DirectShow.IFileSourceFilter;
            int hr = source.Load(filepath, new AMMediaType());
            try
            {
                DsError.ThrowExceptionForHR(hr);
            }
            catch (Exception ex)
            {
                throw new Exception($"无法播放媒体文件“{filepath}”");
            }

            _streamSelect = source as DirectShowLib.IAMStreamSelect;
            int streamCount;
            DirectShowLib.AMMediaType type;
            DirectShowLib.AMStreamSelectInfoFlags fl;
            int plcid;
            int gr;
            string name;
            object o1, o2;
            _streamSelect.Count(out streamCount);
            CurrentAudioStreams.Clear();

            for (int i = 0; i < streamCount; i++)
            {
                _streamSelect.Info(i, out type, out fl, out plcid, out gr, out name, out o1, out o2);
                if (type.majorType == DirectShowLib.MediaType.Audio)
                {
                    CurrentAudioStreams.Add(new AudioStream()
                    {
                        Index = i,
                        Name = name,
                    });
                }
            }
            try
            {
               
                if (CurrentAudioStreamIndex != 0)
                {
                    _streamSelect.Enable(CurrentAudioStreamIndex, DirectShowLib.AMStreamSelectEnableFlags.Enable);
                }
                CurrentAudioStreams.FirstOrDefault(m=>m.Index == CurrentAudioStreamIndex).IsChecked = true;
            }
            catch
            {

            }
            return splitter;
        }
        public void Open(ShowController.Models.SongItem songitem)
        {
            this.SongItem = songitem;
            songitem.OnBeginPlay();
            if(songitem.FilePath == null)
            {
                this.Stop();
                this.Status = PlayerStatus.Running;
                return;
            }
            try
            {
                this.Open(songitem.FilePath);
            }
            catch
            {
                this.SongItem = null;
                throw;
            }

        }
        public void Open(string file)
        {
            this.HasVideo = false;
            this.Status = PlayerStatus.Stopped;

            if (_mediaBuilder != null)
            {
                _mediaBuilder.OpenFile(file, true);
                this.Status = PlayerStatus.Running;
                this.Visible = this.HasVideo;
            }
        }
        public void Stop()
        {
            

            if (this.Status == PlayerStatus.Running && SongItem.FilePath != null)
            {
                var originalVolume = this.GetVolume();
                try
                {

                    var eachVolume = 10000 / 100;
                    var nowVolume = originalVolume;
                    for (int i = 0; i < 100 && originalVolume > -8000; i++)
                    {
                        nowVolume -= eachVolume;
                        this.Invoke(new ThreadStart(() => {
                            _mediaBuilder.Volumn = nowVolume;
                        }));
                        Thread.Sleep(10);
                    }
                }
                catch { }
                _mediaBuilder.Volumn = originalVolume;
            }
            _streamSelect = null;

            if (_mediaBuilder != null)
            {
                _mediaBuilder.Stop();
            }
            this.Status = PlayerStatus.Stopped;
            if (Stopped != null)
            {
                Stopped(this, null);
            }
            this.SongItem?.OnStop();
        }

        public void SetPosition(double positon)
        {
            if (_mediaBuilder != null)
            {
                _mediaBuilder.SetPosition(positon);

                var total = this.GetDuration();
                var current = this.GetCurrentPosition();
                if (this.ProgressChanged != null)
                {
                    this.ProgressChanged(this, total, current);
                }
            }
        }
        public double GetCurrentPosition()
        {
            if (_mediaBuilder != null)
                return _mediaBuilder.GetCurrentPosition();
            else
                return 0;
        }
        public int GetVolume()
        {
            try
            {
                if (_mediaBuilder != null)
                    return _mediaBuilder.Volumn;
                else
                    return 0;
            }
            catch
            {
                return 0;
            }
        }
        public void SetVolume(int volume)
        {
            if (this.Status == PlayerStatus.Running)
            {
                var originalVolume = _mediaBuilder.Volumn;
                var nowVolume = originalVolume;
                var targetVolume = volume;
                var eachVolume = -100;
                if (targetVolume > nowVolume)
                    eachVolume = 100;

                for(int i = 0; i < 10000; i ++)
                {
                    Thread.Sleep(10);
                    nowVolume += eachVolume;
                    if(originalVolume < targetVolume && nowVolume >= targetVolume)
                    {
                        nowVolume = targetVolume;
                        _mediaBuilder.Volumn = nowVolume;
                        break;
                    }
                    else if (originalVolume > targetVolume && nowVolume <= targetVolume)
                    {
                        nowVolume = targetVolume;
                        _mediaBuilder.Volumn = nowVolume;
                        break;
                    }
                    _mediaBuilder.Volumn = nowVolume;
                }
            }
            else
            {
                if (_mediaBuilder != null)
                {
                    try
                    {
                        _mediaBuilder.Volumn = volume;
                    }
                    catch
                    {

                    }
                }
            }
        }

        public double GetDuration()
        {
            if (_mediaBuilder != null)
                return _mediaBuilder.GetDuration();
            else
                return 0;
        }
    }
}
