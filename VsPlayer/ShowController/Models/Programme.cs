using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsPlayer.ShowController.Models
{
    /// <summary>
    /// 节目
    /// </summary>
    public class Programme : Way.Lib.DataModel
    {
        string _Name;
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                if(_Name != value)
                {
                    _Name = value;
                    this.OnPropertyChanged("Name" , null,value);
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
                    _IsActivedItem = value;
                    this.OnPropertyChanged("IsActivedItem", null, value);
                }
            }
        }


        ObservableCollection<SongItem> _Items = new ObservableCollection<SongItem>();
        public ObservableCollection<SongItem> Items
        {
            get
            {
                return _Items;
            }
        }

        public Programme()
        {
            MediaPlayer.instance.StatusChanged += Instance_StatusChanged;
        }

        private void Instance_StatusChanged(object sender, EventArgs e)
        {
            this.IsActivedItem = this.Items.Contains(MediaPlayer.instance.SongItem);

            if (this.IsActivedItem)
            {
                this.IsPlaying = (MediaPlayer.instance.Status == PlayerStatus.Running);
            }
            else
            {
                this.IsPlaying = false;
            }
        }
    }

    public class ProgrammeCollection : ObservableCollection<Programme>
    {

    }
}
