using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
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
        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_MOVE = 0xF010;
        public const int HTCAPTION = 0x0002;

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
        private void ctrlResize_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();

            int WMSZ_BOTTOMRIGHT = 0xF008;
            WindowInteropHelper wihHandle = new WindowInteropHelper(this);// 获得该window的句柄
            SendMessage(wihHandle.Handle, WM_SYSCOMMAND, WMSZ_BOTTOMRIGHT, 0);
        }

        private void btnClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void gridTitle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ReleaseMouseCapture();
            WindowInteropHelper wihHandle = new WindowInteropHelper(this);// 获得该window的句柄
            SendMessage(wihHandle.Handle, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0);
        }

        private void lstPlayList_Drop(object sender, DragEventArgs e)
        {
            var listboxItem = ((FrameworkElement)e.OriginalSource).GetParentByName<ListBoxItem>(null);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var filename = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
                var model = new PlayListItemModel(this.DataModel.PlayList)
                {
                    FilePath = filename
                };

                if (listboxItem != null)
                {
                    var targetModel = listboxItem.DataContext as PlayListItemModel;
                    targetModel.BgColor = null;
                    var index = this.DataModel.PlayList.IndexOf(targetModel);
                    this.DataModel.PlayList.Insert(index, model);
                }
                else
                {
                    this.DataModel.PlayList.Add(model);
                }
            }

        }

        private void ListBoxItem_DragEnter(object sender, DragEventArgs e)
        {
            ListBoxItem listboxitem = sender as ListBoxItem;
            var model = listboxitem.DataContext as PlayListItemModel;
            model.BgColor = "#262d6c";
        }

        private void ListBoxItem_DragLeave(object sender, DragEventArgs e)
        {
            ListBoxItem listboxitem = sender as ListBoxItem;
            var model = listboxitem.DataContext as PlayListItemModel;
            model.BgColor = null;
        }
    }
}
