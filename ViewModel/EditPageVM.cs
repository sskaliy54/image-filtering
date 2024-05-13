
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using STimg.Helpers;
using STimg.View;
using System.IO;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;


namespace STimg.ViewModel
{
    class EditPageVM : BaseVM
    {
        private BitmapImage _uploadedImage;
        public BitmapImage UploadedImage
        {
            get { return _uploadedImage; }
            set
            {
                _uploadedImage = value;
                OnPropertyChanged(nameof(UploadedImage));
            }
        }

        public RelayCommand UploadCommand { get; private set; }

        public EditPageVM()
        {
            UploadCommand = new RelayCommand(UploadImage);
        }

        private void UploadImage(object obj)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "Image files| *.bmp;*jpg;*.png";
            openDialog.FilterIndex = 1;
            if (openDialog.ShowDialog() == true)
            {
                string filePath = openDialog.FileName;
                BitmapImage bitmap = new BitmapImage(new Uri(filePath));
                UploadedImage = bitmap;


            }
        }
    }
}
