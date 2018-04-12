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
    class PlayerProgressBar : Grid
    {
        Point? _downPoint;
        Rectangle _bgFLAG;
        Models.PlayerInfo _playerInfo;
        public PlayerProgressBar()
        {
            this.Loaded += PlayerProgressBar_Loaded;
        }

        private void PlayerProgressBar_Loaded(object sender, RoutedEventArgs e)
        {
            _playerInfo = (Models.PlayerInfo)this.DataContext;
               _bgFLAG = (Rectangle)this.FindName("bgFLAG");
        }

        double getSeconds(Point point)
        {
            var percent = point.X / _bgFLAG.ActualWidth;
            if (percent < 0)
                percent = 0;
            else if (percent > 1)
                percent = 1;

            var seconds = _playerInfo.TotalSeconds * percent;
            return seconds;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            this.CaptureMouse();
            _downPoint = e.GetPosition(_bgFLAG);
            _playerInfo.IsMovingSecond = true;

            var seconds = getSeconds(e.GetPosition(_bgFLAG));
            _playerInfo.CurrentSecondString = Models.PlayerInfo.GetSecondString(seconds);
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if( _downPoint != null )
            {
                var seconds = getSeconds(e.GetPosition(_bgFLAG));
                _playerInfo.CurrentSecondString = Models.PlayerInfo.GetSecondString(seconds);
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
                var seconds = getSeconds(point);
                MediaPlayer.instance.SetPosition(seconds);
                _playerInfo.IsMovingSecond = false;
            }
                base.OnMouseUp(e);
        }
    }
}
