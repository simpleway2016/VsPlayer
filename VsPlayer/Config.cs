using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VsPlayer
{
    class Config
    {
        public double? WindowWidth;
        public double? WindowHeight;
        public double? VolumnBgWidth;
        public bool IsStretchMode;
        public List<PlayListItemModel> PlayList = new List<PlayListItemModel>();
        public List<PlayListItemModel> BackgroundList = new List<PlayListItemModel>();
        public static Config GetInstance()
        {
            try
            {
                string json = System.IO.File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "config.json", System.Text.Encoding.UTF8);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(json);
            }
            catch
            {
                return new Config();
            }
          
        }

        public void Save()
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(this);
            System.IO.File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "config.json", json, System.Text.Encoding.UTF8);
        }
    }
}
