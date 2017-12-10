using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VsPlayer
{
    public partial class VideoForm : Form
    {
        public MediaPlayer Player;
        public VideoForm()
        {
            InitializeComponent();
            Player = new MediaPlayer();
            Player.Visible = false;
            Player.Dock = DockStyle.Fill;
            this.Controls.Add(Player);
        }
        
    }
}
