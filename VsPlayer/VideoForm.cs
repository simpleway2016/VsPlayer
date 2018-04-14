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
        public PictureBox pictureBox;
        public VideoForm()
        {
            InitializeComponent();
           
            pictureBox = new PictureBox();
            pictureBox.BackColor = Color.Black;
            pictureBox.Dock = DockStyle.Fill;
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            this.Controls.Add(pictureBox);

            Player = new MediaPlayer();
            Player.Visible = false;
            Player.Dock = DockStyle.Fill;
            this.Controls.Add(Player);
            Player.BringToFront();
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            e.Cancel = true;
            base.OnFormClosing(e);
        }
    }
}
