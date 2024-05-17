
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
using Emgu.CV.CvEnum;
using Emgu.CV;
using Emgu.CV.Util;




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
        public RelayCommand ApplyBoxFilterCommand { get; private set; }
        public RelayCommand ApplyGaussianBlurCommand { get; private set; }
        public RelayCommand ApplyGrayCommand { get; private set; }
        public RelayCommand ApplyWarmCommand { get; private set; }
        public RelayCommand ApplyDetailEnhancingCommand { get; private set; }
        public RelayCommand ApplyColdCommand { get; private set; }
        public RelayCommand ApplyRetroCommand { get; private set; }
        public RelayCommand ApplyEdgeDetectionCommand { get; private set; }
        public RelayCommand ApplyPinkCommand { get; private set; }
        public RelayCommand SaveImageCommand { get; private set; }

        public EditPageVM()
        {
            UploadCommand = new RelayCommand(UploadImage);
            ApplyBoxFilterCommand = new RelayCommand((_) => ApplyFilter(FilterType.Box));
            ApplyGaussianBlurCommand = new RelayCommand((_) => ApplyFilter(FilterType.GaussianBlur));
            ApplyGrayCommand = new RelayCommand((_) => ApplyFilter(FilterType.Gray));
            ApplyWarmCommand = new RelayCommand((_) => ApplyFilter(FilterType.Warm));
            ApplyDetailEnhancingCommand = new RelayCommand((_) => ApplyFilter(FilterType.DetailEnhancing));
            ApplyColdCommand = new RelayCommand((_) => ApplyFilter(FilterType.Cold));
            ApplyRetroCommand = new RelayCommand((_) => ApplyFilter(FilterType.Retro));
            ApplyEdgeDetectionCommand = new RelayCommand((_) => ApplyFilter(FilterType.EdgeDetection));
            ApplyPinkCommand = new RelayCommand((_) => ApplyFilter(FilterType.Pink));
            SaveImageCommand = new RelayCommand(SaveImage);
        }

        private void UploadImage(object obj)
        {
            OpenFileDialog openDialog = new OpenFileDialog
            {
                Filter = "Image files| *.bmp;*jpg;*.png",
                FilterIndex = 1
            };
            if (openDialog.ShowDialog() == true)
            {
                string filePath = openDialog.FileName;
                BitmapImage bitmap = new BitmapImage(new Uri(filePath));
                UploadedImage = bitmap;
            }
        }

        private BitmapImage MatToBitmapImage(Mat imageMat)
        {
            using (var stream = new MemoryStream())
            {
                imageMat.ToBitmap().Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                stream.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
        }

        private Mat BuildGammaLut(double gamma)
        {
            byte[] lut = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                lut[i] = (byte)Math.Min(255, Math.Pow(i / 255.0, 1 / gamma) * 255);
            }
            Mat lutMat = new Mat(1, 256, DepthType.Cv8U, 1);
            lutMat.SetTo(lut);
            return lutMat;
        }

        private void ApplyLookupTable(Mat channel, byte[] lookupTable)
        {
            Mat lookup = new Mat(256, 1, DepthType.Cv8U, 1);
            lookup.SetTo(lookupTable);
            CvInvoke.LUT(channel, lookup, channel);
        }

        private void ApplyGammaCorrection(VectorOfMat channels, double gamma)
        {
            for (int i = 0; i < channels.Size; i++)
            {
                Mat channel = channels[i];
                Mat correctedChannel = new Mat();
                CvInvoke.LUT(channel, BuildGammaLut(gamma), correctedChannel);
                correctedChannel.CopyTo(channels[i]);
            }
        }

        private void SaveImage(object obj)
        {
            if (UploadedImage == null) return;

            SaveFileDialog saveDialog = new SaveFileDialog
            {
                Filter = "JPEG Image|*.jpg|Bitmap Image|*.bmp|PNG Image|*.png",
                Title = "Save an Image File"
            };
            if (saveDialog.ShowDialog() == true)
            {
                string filePath = saveDialog.FileName;
                Mat imageMat = BitmapExtension.ToMat(UploadedImage);

                switch (saveDialog.FilterIndex)
                {
                    case 1:
                        CvInvoke.Imwrite(filePath, imageMat, new KeyValuePair<ImwriteFlags, int>(ImwriteFlags.JpegQuality, 100));
                        break;
                    case 2:
                        CvInvoke.Imwrite(filePath, imageMat);
                        break;
                    case 3:
                        CvInvoke.Imwrite(filePath, imageMat, new KeyValuePair<ImwriteFlags, int>(ImwriteFlags.PngCompression, 9));
                        break;
                }
            }
        }

        private void ApplyFilter(FilterType filterType)
        {
            if (UploadedImage == null) return;

            Mat imageMat = BitmapExtension.ToMat(UploadedImage);
            VectorOfMat channels;
            switch (filterType)
            {
                case FilterType.Gray:
                    CvInvoke.CvtColor(imageMat, imageMat, ColorConversion.Bgr2Gray);
                    CvInvoke.MedianBlur(imageMat, imageMat, 3);
                    break;
                case FilterType.Warm:
                    channels = new VectorOfMat();
                    CvInvoke.Split(imageMat, channels);
                    ApplyLookupTable(channels[2], BuildLookupTable(0.95));
                    ApplyLookupTable(channels[0], BuildLookupTable(1.1));
                    CvInvoke.Merge(channels, imageMat);
                    break;
                case FilterType.DetailEnhancing:
                    CvInvoke.DetailEnhance(imageMat, imageMat, 10f, 0.15f);
                    break;
                case FilterType.Cold:
                    channels = new VectorOfMat();
                    CvInvoke.Split(imageMat, channels);
                    ApplyLookupTable(channels[2], BuildLookupTable(1.25));
                    ApplyLookupTable(channels[0], BuildLookupTable(0.95));
                    CvInvoke.Merge(channels, imageMat);
                    break;
                case FilterType.Retro:
                    channels = new VectorOfMat();
                    CvInvoke.Split(imageMat, channels);
                    ApplyLookupTable(channels[2], BuildLookupTable(0.95));
                    ApplyLookupTable(channels[1], BuildLookupTable(0.8));
                    ApplyLookupTable(channels[0], BuildLookupTable(0.95));
                    ApplyGammaCorrection(channels, 0.65);
                    CvInvoke.Merge(channels, imageMat);
                    CvInvoke.BoxFilter(imageMat, imageMat, DepthType.Cv8U, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));
                    break;
                case FilterType.EdgeDetection:
                    CvInvoke.CvtColor(imageMat, imageMat, ColorConversion.Bgr2Rgb);
                    CvInvoke.MedianBlur(imageMat, imageMat, 3);
                    Mat bilateral = new Mat();
                    CvInvoke.BilateralFilter(imageMat, bilateral, 9, 20, 20, BorderType.Reflect101);
                    CvInvoke.CvtColor(bilateral, bilateral, ColorConversion.Bgr2Rgb);
                    imageMat = bilateral;
                    break;
                case FilterType.Pink:
                    channels = new VectorOfMat();
                    CvInvoke.Split(imageMat, channels);
                    ApplyLookupTable(channels[2], BuildLookupTable(1.4));
                    ApplyLookupTable(channels[1], BuildLookupTable(1.4));
                    ApplyLookupTable(channels[0], BuildLookupTable(1.2));
                    ApplyGammaCorrection(channels, 0.95);
                    CvInvoke.Merge(channels, imageMat);
                    CvInvoke.BoxFilter(imageMat, imageMat, DepthType.Cv8U, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));
                    break;
                case FilterType.GaussianBlur:
                    CvInvoke.GaussianBlur(imageMat, imageMat, new System.Drawing.Size(3, 3), 1);
                    break;
                case FilterType.Box:
                    CvInvoke.BoxFilter(imageMat, imageMat, DepthType.Cv8U, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));
                    break;
            }
            UploadedImage = MatToBitmapImage(imageMat);
        }

        private byte[] BuildLookupTable(double gamma)
        {
            byte[] lookupTable = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                lookupTable[i] = (byte)Math.Min(255, (int)(Math.Pow(i / 255.0, gamma) * 255.0));
            }
            return lookupTable;
        }

        enum FilterType
        {
            Box,
            GaussianBlur,
            Gray,
            Warm,
            DetailEnhancing,
            Cold,
            Retro,
            EdgeDetection,
            Pink
        }
    }

    public static class BitmapExtension
    {
        public static Mat ToMat(this BitmapSource source)
        {
            int channels = source.Format.BitsPerPixel / 8;

            Mat result = new Mat();
            result.Create(source.PixelHeight, source.PixelWidth, DepthType.Cv8U, channels);

            source.CopyPixels(Int32Rect.Empty, result.DataPointer, result.Step * result.Rows, result.Step);

            return result;
        }
    }
}
