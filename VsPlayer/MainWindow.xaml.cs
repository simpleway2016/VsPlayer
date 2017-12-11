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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VsPlayer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        

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
        public MainWindow()
        {
            InitializeComponent();
            
            this.Config = Config.GetInstance();
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
            DataModel.VolumnBgWidth = (int)(Config.VolumnBgWidth??77);
            this.Config.PlayList.Clear();
            this.Config.BackgroundList.Clear();
            this.DataContext = DataModel;

            _videoForm = new VideoForm();
            _videoForm.Player.PlayCompleted += (s, e) => {
                _videoForm.Player.Visible = false;
                this.DataModel.State = PlayState.Stopped;
                try
                {
                    lstPlayList.SelectedIndex = lstPlayList.SelectedIndex + 1;
                }
                catch { }
            };
            _videoForm.Show();
            _videoForm.Player.SetVolumn(this.DataModel.Volumn);
            new Thread(updatePosition).Start();

            chkStretchMode.IsChecked = this.Config.IsStretchMode;

            _keyHook = new WayControls.Windows.Hook.KeyBordHook();
            _keyHook.OnKeyDownEvent += _keyHook_OnKeyDownEvent;
            _keyHook.Start((int)WayControls.Windows.API.GetCurrentThreadId());

            OnRenderSizeChanged(null);
        }
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            if(sizeInfo != null)
                base.OnRenderSizeChanged(sizeInfo);
            this.DataModel.PlayerListWidth = lstPlayList.ActualWidth - 10;
            this.DataModel.BackgroundListWidth = lstPicture.ActualWidth - 15;
        }
        private void _keyHook_OnKeyDownEvent(object sender, WayControls.Windows.Hook.WayKeyEventArgs e)
        {
           if(e.KeyCode == System.Windows.Forms.Keys.Up)
            {
                e.CallNextHookEx = false;
                this.DataModel.UpVolume();
                _videoForm.Player.SetVolumn(this.DataModel.Volumn);
            }
            else if (e.KeyCode == System.Windows.Forms.Keys.Down)
            {
                e.CallNextHookEx = false;
                this.DataModel.DownVolume();
                _videoForm.Player.SetVolumn(this.DataModel.Volumn);
            }
        }

        void updatePosition()
        {
            while (true)
            {
                Thread.Sleep(1000);
                if (this.DataModel.State == PlayState.Playing)
                {
                    _videoForm.Invoke(new ThreadStart(()=> {
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

        protected override void OnClosing(CancelEventArgs e)
        {
            Config.WindowWidth = this.Width;
            Config.WindowHeight = this.Height;
            this.Config.IsStretchMode = (chkStretchMode.IsChecked == true);
            Config.VolumnBgWidth = DataModel.VolumnBgWidth;
            Config.PlayList.AddRange(this.DataModel.PlayList);
            Config.BackgroundList.AddRange(this.DataModel.BackgroundList);
            Config.Save();
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
                for(int i = 0; i < filenames.Length; i ++)
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
                    this.DataModel.PlayList.Add( model);
                }
                foreach(var data in this.DataModel.PlayList)
                {
                    data.OnPropertyChange("Text");
                }
            }

        }
        private void ListBoxItem_MouseDown(object sender, MouseButtonEventArgs e)
        {

            if(e.ClickCount == 2)
            {
                var curFileObj = this.DataModel.PlayList.FirstOrDefault(m => m.IsSelected);
                if (curFileObj == null)
                    return;

                _videoForm.Player.Stop();
                this.DataModel.State = PlayState.Stopped;

                _videoForm.Player.Visible = true;
                this._videoForm.Player.Open(curFileObj.FilePath);
                this.DataModel.State = PlayState.Playing;
                return;

            }
            ListBoxItem listboxitem = sender as ListBoxItem;
            var model = listboxitem.DataContext as PlayListItemModel;
            model.IsSelected = true;
            DragDrop.DoDragDrop(listboxitem, model, DragDropEffects.Move);
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

        private void btnPlay_MouseDown(object sender, MouseButtonEventArgs e)
        {

            try
            {
                if (this.DataModel.State == PlayState.Playing)
                {
                    this._videoForm.Player.Pause();
                    this.DataModel.State = PlayState.Paused;
                }
                else if (this.DataModel.State == PlayState.Paused)
                {
                    this._videoForm.Player.Play();
                    this.DataModel.State = PlayState.Playing;
                }
                else
                {
                    var curFileObj = this.DataModel.PlayList.FirstOrDefault(m => m.IsSelected);
                    if (curFileObj == null)
                        return;
                    _videoForm.Player.Visible = true;
                    this._videoForm.Player.Open(curFileObj.FilePath);
                    this.DataModel.State = PlayState.Playing;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(this , ex.Message);
            }
        }

        private void btnStop_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (this.DataModel.State != PlayState.Paused)
                {
                    this._videoForm.Player.Stop();
                    _videoForm.Player.Visible = false;
                    this.DataModel.State = PlayState.Stopped;
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
            if(_mouseDownX >= 0)
            {
               this.DataModel.CurrentPosition = (int)(this.DataModel.TotalSeconds *(e.GetPosition(areaPlayPostion).X / areaPlayPostion.ActualWidth));
            }
        }

        private void areaPlayPostion_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if(_mouseDownX >= 0)
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
                this.DataModel.VolumnBgWidth = (int)_mouseVolumnDownX;
                _videoForm.Player.SetVolumn(this.DataModel.Volumn);
            }
        }

        private void areaVolumn_MouseMove(object sender, MouseEventArgs e)
        {
            if(_mouseVolumnDownX >= 0)
            {
                try
                {
                    this.DataModel.VolumnBgWidth = (int)e.GetPosition(areaVolumn).X;
                }
                catch { }
                _videoForm.Player.SetVolumn(this.DataModel.Volumn);
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
            foreach( var screen in screens )
            {
                MenuItem item = new MenuItem();
                item.Header = screen.DeviceName;
                if( screen == System.Windows.Forms.Screen.PrimaryScreen)
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

        System.Drawing.Size _originalSize = new System.Drawing.Size(500,300);
        private void ScreenItem_Click(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            System.Windows.Forms.Screen screen = item.Tag as System.Windows.Forms.Screen;
            if( screen == null )
            {
                //主屏幕
                _videoForm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
                _videoForm.Location = new System.Drawing.Point(System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Left , System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Top);
                _videoForm.ClientSize = _originalSize;
            }
            else
            {
                //
                _originalSize = _videoForm.ClientSize;
                _videoForm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                _videoForm.Location = new System.Drawing.Point(screen.Bounds.Left,screen.Bounds.Top);                
                _videoForm.ClientSize = screen.Bounds.Size;
            }
        }

        char[] titleArr = new char[] {'i','c','k','y','\'' };
        private void txtTitle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(txtTitle.Text == "VsPlayer")
            {
                txtTitle.Text = "VsPlayer ";
                Task.Run(()=> {
                    StringBuilder buffer = new StringBuilder();
                    for(int i = 0; i < titleArr.Length; i ++)
                    {
                       
                        buffer.Append(titleArr[i]);
                        this.Dispatcher.Invoke(()=> {
                            txtTitle.Text = $"V{buffer}s Player";
                        });
                        Thread.Sleep(100);
                    }
                });
            }
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
           var model =  lstPicture.SelectedItem as PlayListItemModel;
            try
            {
                _videoForm.pictureBox.Image = System.Drawing.Bitmap.FromFile(model.FilePath);
            }
            catch(Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void chkStretchMode_Checked(object sender, RoutedEventArgs e)
        {
            if(chkStretchMode.IsChecked == true)
            {
                _videoForm.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            }
            else
            {
                _videoForm.pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
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
                if(fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
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
    }
}
