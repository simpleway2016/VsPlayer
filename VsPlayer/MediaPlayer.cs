using DirectShow;
using MediaFoundation;
using MediaFoundation.EVR;
using Sonic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using VideoConnectLib.Control.Encoding;
using Way.Media;

namespace VsPlayer
{
    public class MediaPlayer : System.Windows.Forms.Control
    {
        public event EventHandler PlayCompleted;
        MediaBuilder _mediaBuilder;
        IMFVideoDisplayControl EvrDisplayControl;
        public MediaPlayer()
        {

               _mediaBuilder = new MediaBuilder();

            _mediaBuilder.DoOpenFileWithSplitter += _mediaBuilder_DoOpenFileWithSplitter;
            _mediaBuilder.DoDecodeAudio += _mediaBuilder_DoDecodeAudio;
            _mediaBuilder.DoDecodeVideo += _mediaBuilder_DoDecodeVideo;
            _mediaBuilder.DoRenderPin += _mediaBuilder_DoRenderPin;
            _mediaBuilder.GetedEventCode += _mediaBuilder_GetedEventCode;
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
                if(PlayCompleted != null)
                {
                    PlayCompleted(this, null);
                }
            }
        }

        public void Pause()
        {
            _mediaBuilder.Pause();
        }
        public void Play()
        {
            _mediaBuilder.Run();
        }

        private DSFilter _mediaBuilder_DoDecodeVideo(DSPin pin)
        {
            DSFilter lavvideo = new DSFilter(MediaFactory.GetLAVVideo() as IBaseFilter); //new ffdshowAudioDec() as IBaseFilter;
            _mediaBuilder.AddFilter(lavvideo, "lav video");
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
                EvrDisplayControl.SetVideoPosition(new MFVideoNormalizedRect(0, 0, 1, 1), new MediaFoundation.Misc.MFRect(0, 0, this.Width,this.Height));


            }
            else
            {
                _mediaBuilder.RenderPin(pin);
            }
        }

        private Sonic.DSFilter _mediaBuilder_DoDecodeAudio(Sonic.DSPin pin)
        {
            DSFilter lavaudio = new DSFilter(MediaFactory.LAVAudio() as IBaseFilter); //new ffdshowAudioDec() as IBaseFilter;
            _mediaBuilder.AddFilter(lavaudio, "audioDecode");
            HRESULT hr = pin.Connect(lavaudio.InputPin);
            hr.Throw();
            return lavaudio;
        }

        private Sonic.DSFilter _mediaBuilder_DoOpenFileWithSplitter(string filepath)
        {
            var filter = (DirectShow.IBaseFilter)MediaFactory.LAVSplitterSource();
            var splitter = new Sonic.DSFilter(filter);

            _mediaBuilder.AddFilter(splitter, "splitter");

            DirectShow.IFileSourceFilter source = splitter.Value as DirectShow.IFileSourceFilter;
            int hr = source.Load(filepath, new AMMediaType());
            DsError.ThrowExceptionForHR(hr);

            return splitter;
        }

        public void Open(string file)
        {
            _mediaBuilder.OpenFile(file, true);
        }
        public void Stop()
        {
            _mediaBuilder.Stop();
        }

        public void SetPosition(double positon)
        {
            _mediaBuilder.SetPosition(positon);
        }
        public double GetCurrentPosition()
        {
            return _mediaBuilder.GetCurrentPosition();
        }

        public void SetVolumn(int volumn)
        {
            _mediaBuilder.Volumn = volumn;
        }

        public double GetDuration()
        {
            return _mediaBuilder.GetDuration();
        }
    }
}
