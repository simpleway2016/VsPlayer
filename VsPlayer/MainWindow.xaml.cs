using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
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
        internal Config Config;
        public MainModel DataModel;
        public MainWindow()
        {
            InitializeComponent();
            
            this.Config = Config.GetInstance();
            if (Config.WindowWidth != null)
                this.Width = Config.WindowWidth.Value;
            if (Config.WindowHeight != null)
                this.Height = Config.WindowHeight.Value;

            DataModel = new MainModel();
            this.DataContext = DataModel;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Config.WindowWidth = this.Width;
            Config.WindowHeight = this.Height;
            Config.Save();
            System.Diagnostics.Process.GetCurrentProcess().Kill();
            base.OnClosing(e);
        }

        Point _downPoint;
        bool _resizing = false;
        Size _windowSize;
        private void ctrlResize_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            ctrlResize.CaptureMouse();
            _downPoint = e.GetPosition(this);
            _windowSize = new Size(this.Width, this.Height);
            _resizing = true;
        }

        private void ctrlResize_MouseMove(object sender, MouseEventArgs e)
        {
            if(_resizing)
            {
                e.Handled = true;
                var point = e.GetPosition(this);
                this.Width = _windowSize.Width + point.X - _downPoint.X;
                this.Height = _windowSize.Height + point.Y - _downPoint.Y;
            }
        }

        private void ctrlResize_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_resizing)
            {
                e.Handled = true;
                ctrlResize.ReleaseMouseCapture();
                _resizing = false;
            }
        }

        private void btnClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
    }
}
