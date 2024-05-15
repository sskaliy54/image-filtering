
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
            ApplyBoxFilterCommand = new RelayCommand(ApplyBoxFilter);
            ApplyGaussianBlurCommand = new RelayCommand(ApplyGaussianBlurFilter);
            ApplyGrayCommand = new RelayCommand(ApplyGrayFilter);
            ApplyWarmCommand = new RelayCommand(ApplyWarmFilter);
            ApplyDetailEnhancingCommand = new RelayCommand(ApplyDetailEnhancing);
            ApplyColdCommand = new RelayCommand(ApplyColdFilter);
            ApplyRetroCommand = new RelayCommand(ApplyRetroFilter);
            ApplyEdgeDetectionCommand = new RelayCommand(ApplyEdgeDetectionFilter);
            ApplyPinkCommand = new RelayCommand(ApplyPinkSkyFilter);
            SaveImageCommand = new RelayCommand(SaveImage);
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


        //--------------------------Save----------------------------------

        private void SaveImage(object obj)
        {
            if (UploadedImage != null)
            {
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.Filter = "JPEG Image|*.jpg|Bitmap Image|*.bmp|PNG Image|*.png";
                saveDialog.Title = "Save an Image File";
                if (saveDialog.ShowDialog() == true)
                {
                    string filePath = saveDialog.FileName;

                    // Convert BitmapImage to Mat
                    Mat imageMat = BitmapExtension.ToMat(UploadedImage);

                    // Save the image using the specified format
                    switch (saveDialog.FilterIndex)
                    {
                        case 1: // JPEG
                            CvInvoke.Imwrite(filePath, imageMat, new KeyValuePair<ImwriteFlags, int>(ImwriteFlags.JpegQuality, 100));
                            break;
                        case 2: // BMP
                            CvInvoke.Imwrite(filePath, imageMat);
                            break;
                        case 3: // PNG
                            CvInvoke.Imwrite(filePath, imageMat, new KeyValuePair<ImwriteFlags, int>(ImwriteFlags.PngCompression, 9));
                            break;
                    }
                }
            }
        }
        //-------------------------------Filters---------------------------

        //GrayFilter
        private void ApplyGrayFilter(object obj)
        {
            if (UploadedImage != null)
            {
                // Convert BitmapImage to Mat
                Mat imageMat = BitmapExtension.ToMat(UploadedImage);

                // Convert to grayscale
                Mat grayMat = new Mat();
                CvInvoke.CvtColor(imageMat, grayMat, ColorConversion.Bgr2Gray);


                // Apply Median blur to reduce noise
                CvInvoke.MedianBlur(grayMat, grayMat, 3);

                // Convert back to BitmapImage and update UploadedImage
                UploadedImage = MatToBitmapImage(grayMat);
            }
        }

        //Warm Filter
        private void ApplyWarmFilter(object obj)
        {
            if (UploadedImage != null)
            {
                // Convert BitmapImage to Mat
                Mat imageMat = BitmapExtension.ToMat(UploadedImage);

                // Split the blue, green, and red channel of the image.
                VectorOfMat channels = new VectorOfMat();
                CvInvoke.Split(imageMat, channels);

                // Define lookup tables for intensity adjustments
                byte[] increaseTable = new byte[256];
                byte[] decreaseTable = new byte[256];
                for (int i = 0; i < 256; i++)
                {
                    increaseTable[i] = (byte)Math.Min(255, 1.1 * i);
                    decreaseTable[i] = (byte)Math.Max(0, 0.95 * i);
                }

                // Increase red channel intensity using the constructed lookup table.
                Mat redLookupTable = new Mat(256, 1, DepthType.Cv8U, 1);
                redLookupTable.SetTo(increaseTable);
                CvInvoke.LUT(channels[2], redLookupTable, channels[2]);

                // Decrease blue channel intensity using the constructed lookup table.
                Mat blueLookupTable = new Mat(256, 1, DepthType.Cv8U, 1);
                blueLookupTable.SetTo(decreaseTable);
                CvInvoke.LUT(channels[0], blueLookupTable, channels[0]);


                // Merge the blue, green, and red channel. 
                Mat outputImage = new Mat();
                CvInvoke.Merge(channels, outputImage);

                // Convert back to BitmapImage and update UploadedImage
                UploadedImage = MatToBitmapImage(outputImage);
            }
        }

        //Detail Enchancing
        private void ApplyDetailEnhancing(object obj)
        {
            if (UploadedImage != null)
            {
                // Convert BitmapImage to Mat
                Mat imageMat = BitmapExtension.ToMat(UploadedImage);

                // Apply the detail enhancing effect by enhancing the details of the image

                CvInvoke.DetailEnhance(imageMat, imageMat, 10f, 0.15f);

                // Convert back to BitmapImage and update UploadedImage
                UploadedImage = MatToBitmapImage(imageMat);
            }
        }

        //Cold Filter
        private void ApplyColdFilter(object obj)
        {
            if (UploadedImage != null)
            {
                // Convert BitmapImage to Mat
                Mat imageMat = BitmapExtension.ToMat(UploadedImage);

                // Split the blue, green, and red channel of the image.
                VectorOfMat channels = new VectorOfMat();
                CvInvoke.Split(imageMat, channels);

                // Define lookup tables for intensity adjustments
                byte[] midtoneContrastIncrease = new byte[256];
                byte[] lowermidsIncrease = new byte[256];
                byte[] uppermidsDecrease = new byte[256];
                for (int i = 0; i < 256; i++)
                {
                    midtoneContrastIncrease[i] = (byte)Math.Min(255, 0.95 * i);
                    lowermidsIncrease[i] = (byte)Math.Min(255, 1.25 * i);
                    uppermidsDecrease[i] = (byte)Math.Max(0, 0.95 * i);
                }

                // Boost the mid-tone red channel contrast using the constructed lookup table.
                Mat redLookupTable = new Mat(256, 1, DepthType.Cv8U, 1);
                redLookupTable.SetTo(midtoneContrastIncrease);
                CvInvoke.LUT(channels[2], redLookupTable, channels[2]);

                // Boost the Blue channel in lower-mids using the constructed lookup table. 
                Mat blueLookupTable1 = new Mat(256, 1, DepthType.Cv8U, 1);
                blueLookupTable1.SetTo(lowermidsIncrease);
                CvInvoke.LUT(channels[0], blueLookupTable1, channels[0]);

                // Decrease the Blue channel in upper-mids using the constructed lookup table.
                Mat blueLookupTable2 = new Mat(256, 1, DepthType.Cv8U, 1);
                blueLookupTable2.SetTo(uppermidsDecrease);
                CvInvoke.LUT(channels[0], blueLookupTable2, channels[0]);

                // Merge the blue, green, and red channel. 
                Mat outputImage = new Mat();
                CvInvoke.Merge(channels, outputImage);

                // Convert back to BitmapImage and update UploadedImage
                UploadedImage = MatToBitmapImage(outputImage);
            }
        }

        //Retro filter
        private void ApplyRetroFilter(object obj)
        {
            if (UploadedImage != null)
            {
                // Convert BitmapImage to Mat
                Mat imageMat = BitmapExtension.ToMat(UploadedImage);

                // Split the blue, green, and red channels of the image
                VectorOfMat channels = new VectorOfMat();
                CvInvoke.Split(imageMat, channels);

                // Define lookup tables for intensity adjustments
                byte[] redIncreaseTable = new byte[256];
                byte[] redDecreaseTable = new byte[256];
                byte[] greenIncreaseTable = new byte[256];
                byte[] greenDecreaseTable = new byte[256];
                byte[] blueIncreaseTable = new byte[256];
                byte[] blueDecreaseTable = new byte[256];

                for (int i = 0; i < 256; i++)
                {
                    // Increase contrast by adjusting gamma
                    // Adjusting red channel
                    redIncreaseTable[i] = (byte)Math.Min(255, Math.Pow(i / 255.0, 0.95) * 255); // Increase red channel
                    redDecreaseTable[i] = (byte)Math.Max(0, Math.Pow(i / 255.0, 0.8) * 255); // Decrease red channel

                    // Adjusting green channel to make it more greenish
                    greenIncreaseTable[i] = (byte)Math.Min(255, Math.Pow(i / 255.0, 0.7) * 255); // Increase green channel
                    greenDecreaseTable[i] = (byte)Math.Max(0, Math.Pow(i / 255.0, 0.8) * 255); // Decrease green channel

                    // Adjusting blue channel
                    blueIncreaseTable[i] = (byte)Math.Min(255, Math.Pow(i / 255.0, 0.95) * 255); // Increase blue channel
                    blueDecreaseTable[i] = (byte)Math.Max(0, Math.Pow(i / 255.0, 0.8) * 255); // Decrease blue channel
                }

                // Apply intensity adjustments
                ApplyLookupTable(channels[2], redIncreaseTable);
                ApplyLookupTable(channels[1], greenDecreaseTable);
                ApplyLookupTable(channels[0], blueIncreaseTable);


                // Apply gamma correction
                double gamma = 0.65; // Example value, you can adjust it
                ApplyGammaCorrection(channels, gamma);


                // Merge the channels back together
                Mat resultMat = new Mat();
                CvInvoke.Merge(channels, resultMat);

                // Convert to Cv8U depth type
                resultMat.ConvertTo(resultMat, DepthType.Cv8U);

                // Apply Box Filter
                CvInvoke.BoxFilter(resultMat, resultMat, DepthType.Cv8U, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1), true, BorderType.Default);

                // Convert back to BitmapImage and update UploadedImage
                UploadedImage = MatToBitmapImage(resultMat);
            }
        }




        //bilateral тут видаляється шум різного виду 
        private void ApplyEdgeDetectionFilter(object obj)
        {
            if (UploadedImage != null)
            {
                Mat src = BitmapExtension.ToMat(UploadedImage);

                CvInvoke.CvtColor(src, src, ColorConversion.Bgr2Rgb);
                CvInvoke.MedianBlur(src, src, 3);
                Mat bilateral = new Mat();

                CvInvoke.BilateralFilter(src, bilateral, 9, 20, 20, BorderType.Reflect101);
                CvInvoke.CvtColor(bilateral, bilateral, ColorConversion.Bgr2Rgb);
                UploadedImage = MatToBitmapImage(bilateral);
            }


        }

        //Pink Filter
        private void ApplyPinkSkyFilter(object obj)
        {
            if (UploadedImage == null)
                return;

            Mat imageMat = BitmapExtension.ToMat(UploadedImage);

            // Define lookup tables for intensity adjustments
            byte[] redIncreaseTable = new byte[256];
            byte[] greenDecreaseTable = new byte[256];
            byte[] blueIncreaseTable = new byte[256];

            for (int i = 0; i < 256; i++)
            {
                // Increase red channel
                redIncreaseTable[i] = (byte)Math.Min(255, Math.Pow(i / 255.0, 1.4) * 255);

                // Decrease green channel
                greenDecreaseTable[i] = (byte)Math.Max(0, Math.Pow(i / 255.0, 1.4) * 255);

                // Increase blue channel
                blueIncreaseTable[i] = (byte)Math.Min(255, Math.Pow(i / 255.0, 1.2) * 255);
            }

            // Split the blue, green, and red channels of the image
            VectorOfMat channels = new VectorOfMat();
            CvInvoke.Split(imageMat, channels);

            // Apply intensity adjustments
            ApplyLookupTable(channels[2], redIncreaseTable);
            ApplyLookupTable(channels[1], greenDecreaseTable);
            ApplyLookupTable(channels[0], blueIncreaseTable);

            // Apply gamma correction
            double gamma = 0.95; // Example value, you can adjust it
            ApplyGammaCorrection(channels, gamma);

            // Merge the channels back together
            Mat resultMat = new Mat();
            CvInvoke.Merge(channels, resultMat);

            // Apply Box Filter
            CvInvoke.BoxFilter(resultMat, resultMat, DepthType.Cv8U, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1), true, BorderType.Default);

            // Convert back to BitmapImage and update UploadedImage
            UploadedImage = MatToBitmapImage(resultMat);
        }

        //Gaussian Filter
        private void ApplyGaussianBlurFilter(object obj)
        {
            if (UploadedImage != null)
            {
                // Convert BitmapImage to Mat
                Mat imageMat = BitmapExtension.ToMat(UploadedImage);

                // Convert to Cv8U depth type
                Mat convertedMat = new Mat();
                imageMat.ConvertTo(convertedMat, DepthType.Cv8U);

                // Apply GaussianBlur Filter
                CvInvoke.GaussianBlur(convertedMat, convertedMat, new System.Drawing.Size(3, 3), 1);

                // Convert back to BitmapImage and update UploadedImage
                UploadedImage = MatToBitmapImage(convertedMat);
            }
        }

        //Box Filter
        private void ApplyBoxFilter(object obj)
        {
            if (UploadedImage != null)
            {
                // Convert BitmapImage to Mat
                Mat imageMat = BitmapExtension.ToMat(UploadedImage);

                // Convert to Cv8U depth type
                Mat convertedMat = new Mat();
                imageMat.ConvertTo(convertedMat, DepthType.Cv8U);

                // Apply Box Filter
                CvInvoke.BoxFilter(convertedMat, convertedMat, DepthType.Cv8U, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1), true, BorderType.Default);

                // Convert back to BitmapImage and update UploadedImage
                UploadedImage = MatToBitmapImage(convertedMat);
            }
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
