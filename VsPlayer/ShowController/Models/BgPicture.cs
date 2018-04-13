using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsPlayer.ShowController.Models
{
    /// <summary>
    /// 背景图
    /// </summary>
    public class BgPicture:Way.Lib.DataModel
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
                if (_Name != value)
                {
                    _Name = value;
                    this.OnPropertyChanged("Name", null, value);
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

        bool _IsDefault;
        /// <summary>
        /// 是否是默认背景图
        /// </summary>
        public bool IsDefault
        {
            get
            {
                return _IsDefault;
            }
            set
            {
                if (_IsDefault != value)
                {
                    _IsDefault = value;
                    this.OnPropertyChanged("IsDefault", null, value);
                }
            }
        }


        bool _IsSelected;
        [Newtonsoft.Json.JsonIgnore]
        public bool IsSelected
        {
            get
            {
                return _IsSelected;
            }
            set
            {
                if (_IsSelected != value)
                {
                    _IsSelected = value;
                    this.OnPropertyChanged("IsSelected", null, value);
                }
            }
        }
        public BgPicture()
        {
        }
        public BgPicture(string filepath)
        {
            this.Name = Path.GetFileNameWithoutExtension(filepath);
            this.FilePath = filepath;
        }
    }
}
