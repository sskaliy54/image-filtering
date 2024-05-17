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
    /// <summary>
    /// Interaction logic for Edit.xaml
    /// </summary>
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


        /*   private void btnUpload_Click(object sender, RoutedEventArgs e)
  {
      OpenFileDialog openDialog = new OpenFileDialog();
      openDialog.Filter = "Image files| *.bmp;*jpg;*.png";
      openDialog.FilterIndex = 1;
      if (openDialog.ShowDialog()==true)
      {
          string filePath = openDialog.FileName;
          BitmapImage bitmap = new BitmapImage(new Uri(filePath));

          Image image = new Image();
          image.Source = bitmap;

          double maxWidth = ImgZone.Width;
          double maxHeight = ImgZone.Height;
          double aspectRatio = bitmap.PixelWidth / (double)bitmap.PixelHeight;

          if (aspectRatio > 1)
          {
              // Зображення ширше, ніж високе
              image.Width = maxWidth*0.95;
              image.Height = (maxWidth *0.95)/ aspectRatio;
          }
          else
          {
              // Зображення вище, ніж широке або квадратне
              image.Height = maxHeight* 0.95;
              image.Width = maxHeight * aspectRatio* 0.95;
          }

          // Перевірка, чи не перевищують розміри зображення розміри прямокутника
          if (image.Width > maxWidth)
          {
              image.Width = maxWidth* 0.95;
              image.Height =( maxWidth* 0.95) / aspectRatio;
          }
          if (image.Height > maxHeight)
          {
              image.Height = maxHeight * 0.95;
              image.Width = maxHeight * aspectRatio * 0.95;
          }
          ImgZone.Fill = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

          DZone.Children.Clear();
          DZone.Children.Add(image);
      }
  }*/
    }
}
