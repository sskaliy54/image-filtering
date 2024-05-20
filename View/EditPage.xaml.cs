using Microsoft.Win32;
using STimg.ViewModel;
using System;
using System.Collections.Generic;
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

namespace STimg.View
{

    public partial class EditPage : UserControl
    {
        public EditPage()
        {
            InitializeComponent();
        }

        private void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            ImgZone.Fill = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

            DZone.Children.Clear();
        }
    }
}
