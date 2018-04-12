using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace VsPlayer.ShowController.Controls
{
    class VolumeControl : Grid
    {
        public static List<int> volumes = new List<int>(new int[]{-10000,-6418,-6147,-6000,
        -5892,-4826,-4647,-4540
        -4477, -4162,-3876, -3614, -3500,
        -3492,-3374,-3261,-3100,-3153,-3048,-2947,-2849,-2755,-2700,
        -2663,-2575,-2520,-2489,-2406,-2325,-2280,-2246,-2170,-2095,-2050,
        -2023,-1952,-1900, -1884,-1834, -1820, -1800,-1780, -1757,-1695,-1636,-1579,
        -1521,-1500,-1464,-1436,-1420, -1408,-1353,-1299,-1246,-1195,-1144,
        -1096,-1060, -1049,-1020,-1003,-957,-912,-868, -800, -774,-784, -760, -744,
        -705,-667,-630,-610,-594,-570 ,-558,-525,-493,-462,-432,-403,
        -375,-348,-322,-297,-285, -273,-250,-228,-207,-187,-176, -168,
        -150,-102,-75,-19,-10,0,0});

        Point? _downPoint;
        Rectangle _bgFLAG;
        Models.PlayerInfo _playerInfo;
        public VolumeControl()
        {
            this.Loaded += PlayerProgressBar_Loaded;
        }

        private void PlayerProgressBar_Loaded(object sender, RoutedEventArgs e)
        {
            _playerInfo = (Models.PlayerInfo)this.DataContext;
               _bgFLAG = (Rectangle)this.FindName("bgFLAG2");
        }

        int getVolume(Point point)
        {
            var percent = point.X / _bgFLAG.ActualWidth;
            if (percent < 0)
                percent = 0;
            else if (percent > 1)
                percent = 1;

            var index = (int)(percent * (Controls.VolumeControl.volumes.Count - 1));
            return volumes[index];
        }
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.CaptureMouse();
                _downPoint = e.GetPosition(_bgFLAG);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if( _downPoint != null )
            {
                //MediaPlayer.instance._mediaBuilder.Volumn = getVolume(e.GetPosition(_bgFLAG));
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (_downPoint != null)
            {
                this.ReleaseMouseCapture();
                _downPoint = null;
                Point point = e.GetPosition(_bgFLAG);
                _playerInfo.Volume = getVolume(e.GetPosition(_bgFLAG));
            }
                base.OnMouseUp(e);
        }
    }
}
