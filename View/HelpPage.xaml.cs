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
    /// <summary>
    /// Interaction logic for HelpPage.xaml
    /// </summary>
    public partial class HelpPage : UserControl
    {
        public HelpPage()
        {
            InitializeComponent();
        }

        private void btnInst_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.instagram.com/sskaliy54/");
        }

        private void btnMail_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://mail.google.com/mail/?view=cm&fs=1&to=tetiana.skalii.ki.2020@lpnu.ua");
        }

        private void btnGit_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/sskaliy54/image-filtering");
        }


    }
}
