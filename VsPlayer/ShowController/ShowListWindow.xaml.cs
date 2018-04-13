﻿using System;
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
        public class MyModel:Way.Lib.DataModel
        {
            public Models.ProgrammeCollection ProgrammeList { get; set; }
            public ObservableCollection<Models.BgPicture> BgPicList { get; set; }

            public Models.PlayerInfo PlayerInfo { get; set; }
        }
        public static ShowListWindow instance;
        public MyModel DataModel;
        public VideoForm VideoForm;
        public ShowListWindow()
        {
            instance = this;
            VideoForm = new VideoForm();

            var path = $"{AppDomain.CurrentDomain.BaseDirectory}data.txt";
            if (File.Exists(path))
            {
                DataModel = Newtonsoft.Json.JsonConvert.DeserializeObject<MyModel> (System.IO.File.ReadAllText(path));

            }
            else
            {
                DataModel = new MyModel {
                    ProgrammeList = new Models.ProgrammeCollection(),
                    BgPicList = new ObservableCollection<Models.BgPicture>(),
                    PlayerInfo = new Models.PlayerInfo()
                };
                DataModel.BgPicList.Add(new Models.BgPicture() {
                    Name = "黑屏"
                });
            }
            
            this.Resources["BgListSource"] = DataModel.BgPicList;
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
          
            VideoForm.Player.ProgressChanged += Player_ProgressChanged;
            VideoForm.Player.PlayCompleted += Player_PlayCompleted;
            VideoForm.Player.Stopped += Player_Stopped;
           VideoForm.Show();

            playerArea.DataContext = DataModel.PlayerInfo;
            this.DataContext = DataModel;
        }

     
        private void Player_Stopped(object sender, EventArgs e)
        {
            VideoForm.Player.Visible = false;
        }

        private void Player_PlayCompleted(object sender, EventArgs e)
        {
            VideoForm.Player.Visible = false;
        }

        private void Player_ProgressChanged(object sender, double totalSeconds, double currentSeconds)
        {
            DataModel.PlayerInfo.TotalSeconds = totalSeconds;
            DataModel.PlayerInfo.CurrentSeconds = currentSeconds;
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

            System.IO.File.WriteAllText(path, Newtonsoft.Json.JsonConvert.SerializeObject(DataModel));
            VideoForm.Player.Stop();
            VideoForm.Player._mediaBuilder.Dispose();
            base.OnClosed(e);
        }

        /// <summary>
        /// 添加节目
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAddProgramme_Click(object sender, RoutedEventArgs e)
        {
            DataModel.ProgrammeList.Add(new Models.Programme
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
                        DataModel.BgPicList.Add(new Models.BgPicture(filepath));
                    }
                     
                }
            }
        }

        private void menuDeleteSongItem_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(this, "确定删除吗？", "", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                var songitem = (Models.SongItem)((FrameworkElement)sender).DataContext;
                foreach (var programme in DataModel.ProgrammeList)
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
                DataModel.BgPicList.Remove(bgitem);
            }
        }

        private void menuSetDefaultBgItem_Click(object sender, RoutedEventArgs e)
        {
            var bgitem = (Models.BgPicture)((FrameworkElement)sender).DataContext;
            foreach( var item in DataModel.BgPicList )
            {
                item.IsDefault = false;
            }
            bgitem.IsDefault = true;
        }

        private void btnPlaySong_Click(object sender, RoutedEventArgs e)
        {
            var songitem = (Models.SongItem)((FrameworkElement)sender).DataContext;
            SongItemClickPlay(songitem);
        }

        public void SongItemClickPlay(Models.SongItem songitem)
        {
            if (songitem == null)
                return;

            if (songitem.IsPlaying)
            {
                VideoForm.Player.Pause();
            }
            else
            {
                if (VideoForm.Player.Status == PlayerStatus.Stopped || VideoForm.Player.SongItem != songitem)
                {
                    VideoForm.Player.CurrentAudioStreamIndex = songitem.AudioStreamIndex;
                    VideoForm.Player.Open(songitem);
                }
                else
                    VideoForm.Player.Play();
            }
        }

        private void btnProgrammePlay_Click(object sender, RoutedEventArgs e)
        {
            var programme = (Models.Programme)((FrameworkElement)sender).DataContext;
            if (programme.Items.Count == 0)
                return;

            if(programme.IsActivedItem)
            {
                SongItemClickPlay(programme.Items.FirstOrDefault(m=>m.IsActivedItem));
            }
            else
            {
                SongItemClickPlay(programme.Items[0]);
                programme.IsShowedDetail = true;
                foreach( var p in DataModel.ProgrammeList )
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
            if (VideoForm.Player.Status == PlayerStatus.Running)
            {
                VideoForm.Player.Pause();
            }
            else if (VideoForm.Player.Status == PlayerStatus.Paused)
            {
                VideoForm.Player.Play();
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            VideoForm.Player.Stop();
        }

        private void VolumeMenu_ContextMenuOpened(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = sender as ContextMenu;
            foreach (MenuItem menuitem in menu.Items)
            {
                var value = Convert.ToInt32(77 * Convert.ToDouble(menuitem.Header.ToString().Replace("%", "")) / 100);
                menuitem.IsChecked = DataModel.PlayerInfo.Volume == value;
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
            DataModel.PlayerInfo.Volume = Controls.VolumeControl.volumes[index];
        }

        private void menuAddNoSong_Click(object sender, RoutedEventArgs e)
        {
            var programme = (Models.Programme)((FrameworkElement)sender).DataContext;
            programme.Items.Add(new Models.SongItem() {
                Name = "纯背景"
            });
            programme.IsShowedDetail = true;
        }

        private void AudioStreamItemCheck_Click(object sender, RoutedEventArgs e)
        {
            var audioStream = (AudioStream)((FrameworkElement)sender).DataContext;
            VideoForm.Player.CurrentAudioStreamIndex = audioStream.Index;
        }

        private void moveSongMenuOpened_Click(object sender, RoutedEventArgs e)
        {
            var songItem = (Models.SongItem)((FrameworkElement)sender).DataContext;
            var programme = DataModel.ProgrammeList.FirstOrDefault(m => m.Items.Contains(songItem));
            var menu = ((MenuItem)sender);
            menu.Items.Clear();
            foreach( var song in programme.Items )
            {
                var newmenuitem = new MenuItem
                {
                    Header = song.Name,
                    Tag = new Models.SongItem[] {songItem , song }
                };
                newmenuitem.Click += moveSong_Click;
                menu.Items.Add(newmenuitem);
            }
        }

        private void moveSong_Click(object sender, RoutedEventArgs e)
        {
            var menu = ((MenuItem)sender);
            Models.SongItem[] objs = (Models.SongItem[])menu.Tag;
            var target =  objs[1];
            var source = objs[0];
            if(source != target)
            {
                var programme = DataModel.ProgrammeList.FirstOrDefault(m => m.Items.Contains(source));
                programme.Items.Remove(source);
                var index = programme.Items.IndexOf(target);
                programme.Items.Insert(index, source);
            }
        }

        private void bgText_Click(object sender, MouseButtonEventArgs e)
        {
            var bgPic = (Models.BgPicture)((FrameworkElement)sender).DataContext;
            bgPic.IsSelected = true;
        }

        private void PictureChanged(object sender, SelectionChangedEventArgs e)
        {
            var data = ((ListBox)sender).SelectedItem as Models.BgPicture;
            if (data == null)
                return;

            var old = VideoForm.pictureBox.Image;

            try
            {
                if (string.IsNullOrEmpty(data.FilePath))
                {
                    VideoForm.pictureBox.Image = null;
                }
                else
                {
                    using (System.IO.FileStream fs = System.IO.File.OpenRead(data.FilePath))
                    {
                        VideoForm.pictureBox.Image = System.Drawing.Bitmap.FromStream(fs);
                    }
                }
            }
            catch (Exception ex)
            {
                VideoForm.pictureBox.Image = null;
            }
            if (old != null)
            {
                old.Dispose();
            }
        }
    }
}