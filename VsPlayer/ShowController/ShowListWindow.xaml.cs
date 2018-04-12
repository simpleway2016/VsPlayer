using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VsPlayer.ShowController
{
    /// <summary>
    /// ShowListWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ShowListWindow : Window
    {
        public class MyModel
        {
            public Models.ProgrammeCollection ProgrammeList { get; set; }
            public ObservableCollection<Models.BgPicture> BgPicList { get; set; }
            public Models.PlayerInfo PlayerInfo { get; set; }
        }
        MyModel _dataModel;
        VideoForm _VideoForm;
        public ShowListWindow()
        {
            _VideoForm = new VideoForm();

            var path = $"{AppDomain.CurrentDomain.BaseDirectory}data.txt";
            if (File.Exists(path))
            {
                _dataModel = Newtonsoft.Json.JsonConvert.DeserializeObject<MyModel> (System.IO.File.ReadAllText(path));

            }
            else
            {
                _dataModel = new MyModel {
                    ProgrammeList = new Models.ProgrammeCollection(),
                    BgPicList = new ObservableCollection<Models.BgPicture>(),
                    PlayerInfo = new Models.PlayerInfo()
                };
                _dataModel.BgPicList.Add(new Models.BgPicture());
            }
            
            this.Resources["BgListSource"] = _dataModel.BgPicList;
            var NextStepSource = new List<object>();
            var names = Enum.GetNames(typeof(Models.NextStep));
            foreach (var name in names)
            {
                NextStepSource.Add(new
                {
                    Name = name,
                    Value = Enum.Parse(typeof(Models.NextStep), name)
                });
            }
            this.Resources["NextStepSource"] = NextStepSource;


            InitializeComponent();
          
            _VideoForm.Player.ProgressChanged += Player_ProgressChanged;
            _VideoForm.Player.PlayCompleted += Player_PlayCompleted;
            _VideoForm.Player.Stopped += Player_Stopped;
           _VideoForm.Show();

            playerArea.DataContext = _dataModel.PlayerInfo;
            this.DataContext = _dataModel;
        }

     
        private void Player_Stopped(object sender, EventArgs e)
        {
            
        }

        private void Player_PlayCompleted(object sender, EventArgs e)
        {
        }

        private void Player_ProgressChanged(object sender, double totalSeconds, double currentSeconds)
        {
            _dataModel.PlayerInfo.TotalSeconds = totalSeconds;
            _dataModel.PlayerInfo.CurrentSeconds = currentSeconds;
        }

        private void menuAddSong_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog fd = new System.Windows.Forms.OpenFileDialog())
            {
                fd.Multiselect = true;
                if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var programme = (Models.Programme)((FrameworkElement)sender).DataContext;
                    foreach( var filepath in fd.FileNames )
                    {
                        programme.Items.Add(new Models.SongItem(filepath));
                    }
                    programme.IsShowedDetail = true;
                }
            }
            
        }

        protected override void OnClosed(EventArgs e)
        {
            var path = $"{AppDomain.CurrentDomain.BaseDirectory}data.txt";
            if (File.Exists(path))
                File.Delete(path);

            System.IO.File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(_dataModel));
            _VideoForm.Player.Stop();
            _VideoForm.Player._mediaBuilder.Dispose();
            base.OnClosed(e);
        }

        /// <summary>
        /// 添加节目
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAddProgramme_Click(object sender, RoutedEventArgs e)
        {
            _dataModel.ProgrammeList.Add(new Models.Programme
            {
                Name = "节目"
            });
        }

        private void btnShowProgammeDetail_Click(object sender, RoutedEventArgs e)
        {
            var programme = (Models.Programme)((FrameworkElement)sender).DataContext;
            programme.IsShowedDetail = !programme.IsShowedDetail;
        }

        private void btnShowItemDetail_Click(object sender, RoutedEventArgs e)
        {
            var songitem = (Models.SongItem)((FrameworkElement)sender).DataContext;
            songitem.IsShowedDetail = !songitem.IsShowedDetail;
        }

        private void btnAddBgPicture_Click(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog fd = new System.Windows.Forms.OpenFileDialog())
            {
                fd.Multiselect = true;
                if (fd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {                   
                    foreach (var filepath in fd.FileNames)
                    {
                        _dataModel.BgPicList.Add(new Models.BgPicture(filepath));
                    }
                     
                }
            }
        }

        private void menuDeleteSongItem_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(this, "确定删除吗？", "", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                var songitem = (Models.SongItem)((FrameworkElement)sender).DataContext;
                foreach (var programme in _dataModel.ProgrammeList)
                {
                    if (programme.Items.Contains(songitem))
                    {
                        programme.Items.Remove(songitem);
                        break;
                    }
                }
            }
        }

        private void menuDeleteBgItem_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(this, "确定删除吗？", "", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                var bgitem = (Models.BgPicture)((FrameworkElement)sender).DataContext;
                _dataModel.BgPicList.Remove(bgitem);
            }
        }

        private void menuSetDefaultBgItem_Click(object sender, RoutedEventArgs e)
        {
            var bgitem = (Models.BgPicture)((FrameworkElement)sender).DataContext;
            foreach( var item in _dataModel.BgPicList )
            {
                item.IsDefault = false;
            }
            bgitem.IsDefault = true;
        }

        private void btnPlaySong_Click(object sender, RoutedEventArgs e)
        {
            var songitem = (Models.SongItem)((FrameworkElement)sender).DataContext;
            songItemClickPlay(songitem);
        }

        void songItemClickPlay(Models.SongItem songitem)
        {
            if (songitem == null)
                return;

            if (songitem.IsPlaying)
            {
                _VideoForm.Player.Pause();
            }
            else
            {
                if (_VideoForm.Player.Status == PlayerStatus.Stopped || _VideoForm.Player.SongItem != songitem)
                {
                    _VideoForm.Player.CurrentAudioStreamIndex = songitem.AudioStreamIndex;
                    _VideoForm.Player.Open(songitem);
                }
                else
                    _VideoForm.Player.Play();
            }
        }

        private void btnProgrammePlay_Click(object sender, RoutedEventArgs e)
        {
            var programme = (Models.Programme)((FrameworkElement)sender).DataContext;
            if (programme.Items.Count == 0)
                return;

            if(programme.IsActivedItem)
            {
                songItemClickPlay(programme.Items.FirstOrDefault(m=>m.IsActivedItem));
            }
            else
            {
                songItemClickPlay(programme.Items[0]);
                programme.IsShowedDetail = true;
                foreach( var p in _dataModel.ProgrammeList )
                {
                    if(p != programme)
                    {
                        p.IsShowedDetail = false;
                    }
                }
            }
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (_VideoForm.Player.Status == PlayerStatus.Running)
            {
                _VideoForm.Player.Pause();
            }
            else if (_VideoForm.Player.Status == PlayerStatus.Paused)
            {
                _VideoForm.Player.Play();
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            _VideoForm.Player.Stop();
        }

        private void VolumeMenu_ContextMenuOpened(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = sender as ContextMenu;
            foreach (MenuItem menuitem in menu.Items)
            {
                var value = Convert.ToInt32(77 * Convert.ToDouble(menuitem.Header.ToString().Replace("%", "")) / 100);
                menuitem.IsChecked = _dataModel.PlayerInfo.Volume == value;
            }
        }

        private void VolumeMenu_Click(object sender, RoutedEventArgs e)
        {
            MenuItem menuitem = sender as MenuItem;
            var index = Convert.ToInt32((Controls.VolumeControl.volumes.Count - 1) * (Convert.ToDouble(menuitem.Header.ToString().Replace("%", "")) / 100));
            if (index < 0)
                index = 0;
            else if (index >= Controls.VolumeControl.volumes.Count)
                index = Controls.VolumeControl.volumes.Count - 1;
            _dataModel.PlayerInfo.Volume = Controls.VolumeControl.volumes[index];
        }

        private void menuAddNoSong_Click(object sender, RoutedEventArgs e)
        {
            var programme = (Models.Programme)((FrameworkElement)sender).DataContext;
            programme.Items.Add(new Models.SongItem() {
                Name = "纯背景"
            });
            programme.IsShowedDetail = true;
        }
    }
}
