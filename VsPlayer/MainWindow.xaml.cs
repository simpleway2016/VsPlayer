using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VideoConnectLib.Control.Encoding;

namespace VsPlayer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        public static MainWindow instance;
        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_MOVE = 0xF010;
        public const int HTCAPTION = 0x0002;

        internal Config Config;
        public MainModel DataModel;
        VideoForm _videoForm;
        bool _LockedPosition;
        WayControls.Windows.Hook.KeyBordHook _keyHook;
        internal List<HistoryItem> HistoryItems = new List<HistoryItem>();
        [DllImport("kernel32.dll")]
        static extern uint SetThreadExecutionState(ExecutionFlag flags);

        [Flags]
        enum ExecutionFlag : uint
        {
            System = 0x00000001,
            Display = 0x00000002,
            Continus = 0x80000000,
        }

        public MainWindow()
        {
            instance = this;

            //设置应用程序处理异常方式：ThreadException处理
            System.Windows.Forms.Application.SetUnhandledExceptionMode( System.Windows.Forms.UnhandledExceptionMode.CatchException);
            //处理UI线程异常
            System.Windows.Forms.Application.ThreadException += Application_ThreadException;
            //处理非UI线程异常
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            InitializeComponent();

            //阻止系统休眠，直到线程结束恢复休眠策略
            SetThreadExecutionState(ExecutionFlag.System | ExecutionFlag.Display | ExecutionFlag.Continus);

            this.Config = Config.GetInstance();

            try
            {
                string json = System.IO.File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "history.json", System.Text.Encoding.UTF8);
                HistoryItems = new List<HistoryItem>(Newtonsoft.Json.JsonConvert.DeserializeObject<HistoryItem[]>(json));
            }
            catch
            {

            }


            if (Config.WindowWidth != null)
                this.Width = Config.WindowWidth.Value;
            if (Config.WindowHeight != null)
                this.Height = Config.WindowHeight.Value;

            DataModel = new MainModel();
            foreach (var f in Config.PlayList)
            {
                f.Continer = DataModel.PlayList;
                DataModel.PlayList.Add(f);
            }
            foreach (var f in Config.BackgroundList)
            {
                f.Continer = DataModel.BackgroundList;
                DataModel.BackgroundList.Add(f);
            }
            DataModel.VolumnBgWidth = (int)(Config.VolumnBgWidth ?? 77);
            this.DataModel.IsSetLastTimeVolume = Config.IsSetLastTimeVolume;
            this.DataModel.IsVideoStretchMode = this.Config.IsVideoStretchMode;
            this.DataModel.IsListLoop = this.Config.IsListLoop;
            this.DataModel.IsSingleLoop = this.Config.IsSingleLoop;
            this.DataModel.ShowSerialNumber = this.Config.ShowSerialNumber;
            this.Config.PlayList.Clear();
            this.Config.BackgroundList.Clear();
            this.DataContext = DataModel;

            _videoForm = new VideoForm();
            _videoForm.Player.PlayCompleted += (s, e) =>
            {
                this.DataModel.State = PlayState.Stopped;
                rememberHistory();
                _videoForm.Player.CurrentAudioStreamIndex = 0;
                if (this.DataModel.IsSingleLoop)
                {
                    btnPlay_MouseDown(null, null);
                }
                else
                {
                    if (this.DataModel.IsListLoop && lstPlayList.SelectedIndex == lstPlayList.Items.Count - 1)
                    {
                        try
                        {
                            lstPlayList.SelectedIndex = 0;
                        }
                        catch { }
                        btnPlay_MouseDown(null, null);
                    }
                    else
                    {
                        try
                        {
                            lstPlayList.SelectedIndex = lstPlayList.SelectedIndex + 1;
                        }
                        catch { }
                    }
                }
            };
            _videoForm.Show();
            _videoForm.Player.SetVolume(this.DataModel.Volumn);
            new Thread(updatePosition).Start();

            if (this.Config.IsStretchMode)
            {
                chkStretchMode_MenuItem_Click(null, null);
            }

            _keyHook = new WayControls.Windows.Hook.KeyBordHook();
            _keyHook.OnKeyDownEvent += _keyHook_OnKeyDownEvent;
            _keyHook.Start((int)WayControls.Windows.API.GetCurrentThreadId());

            menu_audioTracks.ItemsSource = _videoForm.Player.CurrentAudioStreams;
            OnRenderSizeChanged(null);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if(e.ExceptionObject != null)
            {
                var ex = e.ExceptionObject as Exception;
                if(ex != null)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            saveConfig();
        }

        private void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            if (e.Exception != null)
            {
                MessageBox.Show(e.Exception.Message);
            }
            saveConfig();
        }

        PlayListItemModel _lastPlayingModel = null;
        void rememberHistory(bool otherThreadSave = true)
        {

            var playingModel = _lastPlayingModel;
            if (playingModel != null)
            {
                try
                {
                    string filename = System.IO.Path.GetFileName(playingModel.FilePath);
                    long filelen = new System.IO.FileInfo(playingModel.FilePath).Length;
                    var historyitem = HistoryItems.FirstOrDefault(m => m.FileLength == filelen && string.Equals(m.FileName, filename, StringComparison.CurrentCultureIgnoreCase));
                    if (historyitem == null)
                    {
                        historyitem = new HistoryItem()
                        {
                            FileLength = filelen,
                            FileName = filename,
                            AudioStreamIndex = _videoForm.Player.CurrentAudioStreamIndex,
                            Volume = this.DataModel.IsSetLastTimeVolume ? new int?(_videoForm.Player.GetVolume()) : null,
                        };
                        HistoryItems.Add(historyitem);
                        if (HistoryItems.Count > 200)
                            HistoryItems.RemoveAt(0);
                    }
                    else
                    {
                        historyitem.AudioStreamIndex = _videoForm.Player.CurrentAudioStreamIndex;
                        if (this.DataModel.IsSetLastTimeVolume)
                        {
                            historyitem.Volume = _videoForm.Player.GetVolume();
                        }
                    }
                    if (otherThreadSave && this.DataModel.IsSetLastTimeVolume)
                    {
                        Task.Run(() =>
                        {
                            try
                            {
                                lock (HistoryItems)
                                {
                                    System.IO.File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "history.json",
                                        Newtonsoft.Json.JsonConvert.SerializeObject(HistoryItems),
                                        System.Text.Encoding.UTF8);
                                }
                            }
                            catch
                            {

                            }
                        });
                    }
                    else
                    {
                        System.IO.File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "history.json",
                                       Newtonsoft.Json.JsonConvert.SerializeObject(HistoryItems),
                                       System.Text.Encoding.UTF8);
                    }
                }
                catch
                {

                }
            }
        }
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if (sizeInfo != null)
                base.OnRenderSizeChanged(sizeInfo);

            var bs = lstPlayList.GetChildsByName<ScrollBar>(null).FirstOrDefault(m=>m.Orientation ==  Orientation.Vertical);
            double more = 0;
            if(bs != null && bs.IsVisible)
            {
                //滚动条出现了
                more = bs.ActualWidth;
            }
            this.DataModel.PlayerListWidth = lstPlayList.ActualWidth - 10  - more;

            bs = lstPicture.GetChildsByName<ScrollBar>(null).FirstOrDefault(m => m.Orientation == Orientation.Vertical);
            more = 0;
            if (bs != null && bs.IsVisible)
            {
                more = bs.ActualWidth;
            }
            this.DataModel.BackgroundListWidth = lstPicture.ActualWidth - 15 - more;
        }
        private void _keyHook_OnKeyDownEvent(object sender, WayControls.Windows.Hook.WayKeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Up)
            {
                e.CallNextHookEx = false;
                this.DataModel.UpVolume();
                _videoForm.Player.SetVolume(this.DataModel.Volumn);
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.Down)
            {
                e.CallNextHookEx = false;
                this.DataModel.DownVolume();
                _videoForm.Player.SetVolume(this.DataModel.Volumn);
            }
        }

        void updatePosition()
        {
            while (true)
            {
                Thread.Sleep(1000);
                if (this.DataModel.State == PlayState.Playing)
                {
                    _videoForm.Invoke(new ThreadStart(() =>
                    {
                        int seconds = (int)_videoForm.Player.GetCurrentPosition();
                        int total = (int)_videoForm.Player.GetDuration();
                        this.DataModel.TotalSeconds = total;
                        if (_LockedPosition == false)
                        {
                            this.DataModel.CurrentPosition = seconds;
                        }
                    }));

                }
            }
        }

        void saveConfig()
        {
            Config.WindowWidth = this.Width;
            Config.WindowHeight = this.Height;
            this.Config.IsVideoStretchMode = this.DataModel.IsVideoStretchMode;
            this.Config.IsSetLastTimeVolume = this.DataModel.IsSetLastTimeVolume;
            this.Config.IsListLoop = this.DataModel.IsListLoop;
            this.Config.IsSingleLoop = this.DataModel.IsSingleLoop;
            this.Config.ShowSerialNumber = this.DataModel.ShowSerialNumber;
            this.Config.IsStretchMode = (chkStretchMode_MenuItem.IsChecked == true);
            Config.VolumnBgWidth = DataModel.VolumnBgWidth;
            Config.PlayList.AddRange(this.DataModel.PlayList);
            Config.BackgroundList.AddRange(this.DataModel.BackgroundList);
            Config.Save();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            saveConfig();

            if (this.DataModel.State != PlayState.Stopped)
            {
                rememberHistory(false);
            }

            System.Diagnostics.Process.GetCurrentProcess().Kill();
            base.OnClosing(e);
        }
        private void ctrlResize_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();

            int WMSZ_BOTTOMRIGHT = 0xF008;
            WindowInteropHelper wihHandle = new WindowInteropHelper(this);// 获得该window的句柄
            SendMessage(wihHandle.Handle, WM_SYSCOMMAND, WMSZ_BOTTOMRIGHT, 0);
        }

        private void btnClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void gridTitle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();
            WindowInteropHelper wihHandle = new WindowInteropHelper(this);// 获得该window的句柄
            SendMessage(wihHandle.Handle, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0);
        }

        private void lstPlayList_Drop(object sender, DragEventArgs e)
        {
            var listboxItem = ((FrameworkElement)e.OriginalSource).GetParentByName<ListBoxItem>(null);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var arr = ((System.Array)e.Data.GetData(DataFormats.FileDrop));
                var filenames = new string[arr.Length];
                for (int i = 0; i < filenames.Length; i++)
                {
                    filenames[i] = arr.GetValue(i).ToString();
                }
                filenames = filenames.OrderBy(m => m).ToArray();
                int index = this.DataModel.PlayList.Count;
                if (listboxItem != null)
                {
                    var targetModel = listboxItem.DataContext as PlayListItemModel;
                    targetModel.BgColor = null;
                    index = this.DataModel.PlayList.IndexOf(targetModel);
                }
                foreach (var filename in filenames)
                {
                    var model = new PlayListItemModel(this.DataModel.PlayList)
                    {
                        FilePath = filename
                    };

                    this.DataModel.PlayList.Insert(index, model);
                    index++;
                }
            }
            else
            {
                var model = e.Data.GetData(typeof(PlayListItemModel)) as PlayListItemModel;
                if (model.Continer == this.DataModel.BackgroundList)
                    return;
                if (model != null && listboxItem != null && listboxItem.DataContext != model)
                {
                    //移动位置
                    var targetModel = listboxItem.DataContext as PlayListItemModel;
                    targetModel.BgColor = null;
                    this.DataModel.PlayList.Remove(model);
                    var index = this.DataModel.PlayList.IndexOf(targetModel);
                    this.DataModel.PlayList.Insert(index, model);
                }
                else if (model != null && listboxItem == null)
                {
                    this.DataModel.PlayList.Remove(model);
                    this.DataModel.PlayList.Add(model);
                }
                foreach (var data in this.DataModel.PlayList)
                {
                    data.OnPropertyChange("Text");
                }
            }

        }
        private void ListBoxItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    var curFileObj = this.DataModel.PlayList.FirstOrDefault(m => m.IsSelected);
                    if (curFileObj == null)
                        return;

                    if (this.DataModel.State != PlayState.Stopped)
                    {
                        _videoForm.Player.Stop();
                        this.DataModel.State = PlayState.Stopped;
                        rememberHistory();
                    }

                    try
                    {
                        long filelen = new System.IO.FileInfo(curFileObj.FilePath).Length;
                        string filename = System.IO.Path.GetFileName(curFileObj.FilePath);
                        var historyItem = HistoryItems.FirstOrDefault(m => m.FileLength == filelen && string.Equals(m.FileName, filename, StringComparison.CurrentCultureIgnoreCase));
                        if (historyItem != null)
                        {
                            if (historyItem.Volume != null && this.DataModel.IsSetLastTimeVolume)
                            {
                                this.DataModel.SetVolume(historyItem.Volume.Value);
                                _videoForm.Player.SetVolume(historyItem.Volume.Value);
                            }
                            _videoForm.Player.CurrentAudioStreamIndex = historyItem.AudioStreamIndex;
                        }
                    }
                    catch
                    {

                    }
                    _lastPlayingModel = curFileObj;
                    try
                    {
                        txtPlaying.Text = System.IO.Path.GetFileName(curFileObj.FilePath);
                        this._videoForm.Player.Open(curFileObj.FilePath);
                        this.DataModel.State = PlayState.Playing;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message);
                    }
                    return;

                }
                ListBoxItem listboxitem = sender as ListBoxItem;
                var model = listboxitem.DataContext as PlayListItemModel;
                model.IsSelected = true;
                DragDrop.DoDragDrop(listboxitem, model, DragDropEffects.Move);
            }
        }
        private void ListBoxItem_DragEnter(object sender, DragEventArgs e)
        {
            ListBoxItem listboxitem = sender as ListBoxItem;
            if (e.Data.GetData(typeof(PlayListItemModel)) == listboxitem.DataContext)
                return;
            var model = listboxitem.DataContext as PlayListItemModel;
            model.BgColor = "#262d6c";
        }

        private void ListBoxItem_DragLeave(object sender, DragEventArgs e)
        {
            ListBoxItem listboxitem = sender as ListBoxItem;
            var model = listboxitem.DataContext as PlayListItemModel;
            model.BgColor = null;
        }

        private void btnPlayItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Grid grid = ((Grid)sender);
            var data = grid.DataContext as PlayListItemModel;
            data.IsSelected = true;
            btnPlay_MouseDown(null, null);

        }
        private void btnPlay_MouseDown(object sender, MouseButtonEventArgs e)
        {

            try
            {
                if (this.DataModel.State == PlayState.Playing && sender != null)
                {
                    this._videoForm.Player.Pause();
                    this.DataModel.State = PlayState.Paused;
                }
                else if (this.DataModel.State == PlayState.Paused && sender != null)
                {
                    this._videoForm.Player.Play();
                    this.DataModel.State = PlayState.Playing;
                }
                else
                {
                    var curFileObj = this.DataModel.PlayList.FirstOrDefault(m => m.IsSelected);
                    if (curFileObj == null)
                        return;

                    try
                    {

                        long filelen = new System.IO.FileInfo(curFileObj.FilePath).Length;
                        string filename = System.IO.Path.GetFileName(curFileObj.FilePath);
                        var historyItem = HistoryItems.FirstOrDefault(m => m.FileLength == filelen && string.Equals(m.FileName, filename, StringComparison.CurrentCultureIgnoreCase));
                        if (historyItem != null)
                        {
                            if (historyItem.Volume != null && this.DataModel.IsSetLastTimeVolume)
                            {
                                this.DataModel.SetVolume(historyItem.Volume.Value);
                                _videoForm.Player.SetVolume(historyItem.Volume.Value);
                            }
                            _videoForm.Player.CurrentAudioStreamIndex = historyItem.AudioStreamIndex;
                        }

                    }
                    catch
                    {

                    }

                    _lastPlayingModel = curFileObj;
                    try {
                        txtPlaying.Text = System.IO.Path.GetFileName( curFileObj.FilePath);
                        this._videoForm.Player.Open(curFileObj.FilePath);
                        this.DataModel.State = PlayState.Playing;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void btnStop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (this.DataModel.State != PlayState.Stopped)
                {
                    this._videoForm.Player.Stop();
                    this.DataModel.State = PlayState.Stopped;
                    rememberHistory();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        double _mouseDownX = -1;
        private void areaPlayPostion_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            _mouseDownX = e.GetPosition(areaPlayPostion).X;
            if (_mouseDownX >= 0)
            {
                areaPlayPostion.CaptureMouse();
                _LockedPosition = true;
                this.DataModel.CurrentPosition = (int)(this.DataModel.TotalSeconds * (_mouseDownX / areaPlayPostion.ActualWidth));
            }
        }

        private void areaPlayPostion_MouseMove(object sender, MouseEventArgs e)
        {
            if (_mouseDownX >= 0)
            {
                this.DataModel.CurrentPosition = (int)(this.DataModel.TotalSeconds * (e.GetPosition(areaPlayPostion).X / areaPlayPostion.ActualWidth));
            }
        }

        private void areaPlayPostion_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_mouseDownX >= 0)
            {
                _mouseDownX = -1;
                _LockedPosition = false;
                _videoForm.Player.SetPosition(this.DataModel.CurrentPosition);
                areaPlayPostion.ReleaseMouseCapture();
            }
        }



        double _mouseVolumnDownX = -1;
        private void areaVolumn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            _mouseVolumnDownX = e.GetPosition(areaVolumn).X;
            if (_mouseVolumnDownX >= 0)
            {
                areaVolumn.CaptureMouse();
                try
                {
                    this.DataModel.VolumnBgWidth = (int)_mouseVolumnDownX;
                    _videoForm.Player.SetVolume(this.DataModel.Volumn);
                }
                catch
                {

                }
            }
        }

        private void areaVolumn_MouseMove(object sender, MouseEventArgs e)
        {
            if (_mouseVolumnDownX >= 0)
            {
                try
                {
                    this.DataModel.VolumnBgWidth = (int)e.GetPosition(areaVolumn).X;
                }
                catch { }
                _videoForm.Player.SetVolume(this.DataModel.Volumn);
            }
        }

        private void areaVolumn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_mouseVolumnDownX >= 0)
            {
                _mouseVolumnDownX = -1;
                areaVolumn.ReleaseMouseCapture();
            }
        }

        private void btnSetting_MouseDown(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.Screen[] screens = System.Windows.Forms.Screen.AllScreens;
            ContextMenu menu = new ContextMenu();
            menu.PlacementTarget = btnSetting;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
            foreach (var screen in screens)
            {
                MenuItem item = new MenuItem();
                item.Header = screen.DeviceName;
                if (screen == System.Windows.Forms.Screen.PrimaryScreen)
                {
                    item.Header = "主屏幕";
                    item.Tag = null;
                }
                else
                {
                    item.Tag = screen;
                }
                item.Click += ScreenItem_Click;
                menu.Items.Add(item);
            }
            menu.IsOpen = true;
        }

        System.Drawing.Size _originalSize = new System.Drawing.Size(500, 300);
        private void ScreenItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            System.Windows.Forms.Screen screen = item.Tag as System.Windows.Forms.Screen;
            if (screen == null)
            {
                //主屏幕
                _videoForm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
                _videoForm.Location = new System.Drawing.Point(System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Left, System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Top);
                _videoForm.ClientSize = _originalSize;
            }
            else
            {
                //
                _originalSize = _videoForm.ClientSize;
                _videoForm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                _videoForm.Location = new System.Drawing.Point(screen.Bounds.Left, screen.Bounds.Top);
                _videoForm.ClientSize = screen.Bounds.Size;
            }
        }

        //char[] titleArr = new char[] { 'i', 'c', 'k', 'y', '\'' };
        private void txtTitle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //if (txtTitle.Text == "VsPlayer")
            //{
            //    txtTitle.Text = "VsPlayer ";
            //    Task.Run(() =>
            //    {
            //        StringBuilder buffer = new StringBuilder();
            //        for (int i = 0; i < titleArr.Length; i++)
            //        {

            //            buffer.Append(titleArr[i]);
            //            this.Dispatcher.Invoke(() =>
            //            {
            //                txtTitle.Text = $"V{buffer}s Player";
            //            });
            //            Thread.Sleep(100);
            //        }
            //    });
            //}
        }

        private void lstPicture_Drop(object sender, DragEventArgs e)
        {
            var listboxItem = ((FrameworkElement)e.OriginalSource).GetParentByName<ListBoxItem>(null);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var arr = ((System.Array)e.Data.GetData(DataFormats.FileDrop));
                var filenames = new string[arr.Length];
                for (int i = 0; i < filenames.Length; i++)
                {
                    filenames[i] = arr.GetValue(i).ToString();
                }
                filenames = filenames.OrderBy(m => m).ToArray();
                int index = this.DataModel.BackgroundList.Count;
                if (listboxItem != null)
                {
                    var targetModel = listboxItem.DataContext as PlayListItemModel;
                    targetModel.BgColor = null;
                    index = this.DataModel.BackgroundList.IndexOf(targetModel);
                }
                foreach (var filename in filenames)
                {
                    var model = new PlayListItemModel(this.DataModel.BackgroundList)
                    {
                        FilePath = filename
                    };

                    this.DataModel.BackgroundList.Insert(index, model);
                    index++;
                }
            }
            else
            {
                var model = e.Data.GetData(typeof(PlayListItemModel)) as PlayListItemModel;
                if (model.Continer == this.DataModel.PlayList)
                    return;
                if (model != null && listboxItem != null && listboxItem.DataContext != model)
                {
                    //移动位置
                    var targetModel = listboxItem.DataContext as PlayListItemModel;
                    targetModel.BgColor = null;
                    this.DataModel.BackgroundList.Remove(model);
                    var index = this.DataModel.BackgroundList.IndexOf(targetModel);
                    this.DataModel.BackgroundList.Insert(index, model);
                }
                else if (model != null && listboxItem == null)
                {
                    this.DataModel.BackgroundList.Remove(model);
                    this.DataModel.BackgroundList.Add(model);
                }
                foreach (var data in this.DataModel.BackgroundList)
                {
                    data.OnPropertyChange("Text");
                }
            }
        }

        private void lstPicture_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstPicture.SelectedIndex < 0)
                return;
            var model = lstPicture.SelectedItem as PlayListItemModel;
            var old = _videoForm.pictureBox.Image;
           
            try
            {
                _videoForm.pictureBox.Image = System.Drawing.Bitmap.FromFile(model.FilePath);
            }
            catch (Exception ex)
            {
                _videoForm.pictureBox.Image = null;
            }
            if (old != null)
            {
                old.Dispose();
            }
        }


        private void btnDelItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement ctrl = sender as FrameworkElement;
            PlayListItemModel model = ctrl.DataContext as PlayListItemModel;
            model.Continer.Remove(model);
        }

        private void ClearPlayList_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(this, "确定清空播放列表吗？", "", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                this.DataModel.PlayList.Clear();
            }
        }

        private void AddMovie_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog fd = new System.Windows.Forms.OpenFileDialog())
            {
                fd.Multiselect = true;
                if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    foreach (var filename in fd.FileNames)
                    {
                        var model = new PlayListItemModel(this.DataModel.PlayList)
                        {
                            FilePath = filename
                        };

                        this.DataModel.PlayList.Add(model);
                    }
                }
            }
        }

        private void AddPic_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog fd = new System.Windows.Forms.OpenFileDialog())
            {
                fd.Multiselect = true;
                if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    foreach (var filename in fd.FileNames)
                    {
                        var model = new PlayListItemModel(this.DataModel.BackgroundList)
                        {
                            FilePath = filename
                        };

                        this.DataModel.BackgroundList.Add(model);
                    }
                }
            }
        }

        private void AudioStream_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuitem = sender as MenuItem;
            if (menuitem.DataContext is AudioStream)
            {
                if (menuitem.IsChecked != true)
                {
                    var audiostream = menuitem.DataContext as AudioStream;
                    _videoForm.Player.CurrentAudioStreamIndex = _videoForm.Player.CurrentAudioStreams.IndexOf(audiostream);
                }

            }
        }

        private void chkStretchMode_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            chkStretchMode_MenuItem.IsChecked = !chkStretchMode_MenuItem.IsChecked;
            if (chkStretchMode_MenuItem.IsChecked == true)
            {
                _videoForm.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            }
            else
            {
                _videoForm.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            }
        }

        private void menuVideoStretchMode_Checked(object sender, RoutedEventArgs e)
        {
            _videoForm.Player.IsVideoStretchMode = this.DataModel.IsVideoStretchMode;
        }
    }
}
