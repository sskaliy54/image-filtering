
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
using STimg.Models;
using Emgu.CV.DnnSuperres;
using Emgu.CV.Structure;



namespace STimg.ViewModel
{
    class EditPageVM : BaseVM
    {

        private void ApplyFSRCNN()
        {
            if (UploadedImage == null) return;

            Mat imageMat = BitmapExtension.ToMat(UploadedImage);

            double variance = ComputeLaplacianVariance(imageMat);
            const double blurThreshold = 100.0;

            if (variance < blurThreshold)
            {
                Mat denoisedImage = new Mat();
                CvInvoke.BoxFilter(imageMat, denoisedImage, DepthType.Cv8U, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1), true, BorderType.Default);

                int scaleIncreaseFactor = (int)(variance / blurThreshold) + 1;
                int enhanceSteps = 3 * scaleIncreaseFactor;
                Mat kernel = CreateSharpeningKernel();
                for (int i = 0; i < enhanceSteps; i++)
                {
                    CvInvoke.Filter2D(denoisedImage, imageMat, kernel, new System.Drawing.Point(-1, -1));
                }
            }
            CvInvoke.ConvertScaleAbs(imageMat, imageMat, 0.85, 5);
            CvInvoke.DetailEnhance(imageMat, imageMat, 5f, 0.025f);
            long imageSizeInBytes = imageMat.Total.ToInt64() * imageMat.ElementSize;
            string model;
            switch (imageSizeInBytes <= 3 * 1024 * 1024)
            {
                case true:
                    model = "FSRCNN_x3.pb";
                    break;
                default:
                    model = "FSRCNN-small_x2.pb";
                    break;
            }
            using (var fsrcnn = new DnnSuperResImpl())
            {
                fsrcnn.ReadModel($"Resource\\{model}");
                int scale = model.Contains("x3") ? 3 : 2;
                fsrcnn.SetModel("fsrcnn", scale);
                Mat result = new Mat();
                Mat convertedImage = new Mat();
                CvInvoke.CvtColor(imageMat, convertedImage, ColorConversion.Bgra2Bgr);
                fsrcnn.Upsample(convertedImage, result);
                UploadedImage = MatToBitmapImage(result);
            }
        }

        private Mat CreateSharpeningKernel()
        {
            float[] kernelValues = { -1, -1, -1, -1, 9, -1, -1, -1, -1 };
            Mat kernel = new Mat(3, 3, DepthType.Cv32F, 1);
            kernel.SetTo(kernelValues);
            return kernel;
        }

        private double ComputeLaplacianVariance(Mat image)
        {
            Mat gray = new Mat();
            if (image.NumberOfChannels == 3)
            {
                CvInvoke.CvtColor(image, gray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
            }
            else
            {
                gray = image.Clone();
            }

            Mat laplacian = new Mat();
            CvInvoke.Laplacian(gray, laplacian, Emgu.CV.CvEnum.DepthType.Cv64F);

            MCvScalar mean = new MCvScalar();
            MCvScalar stddev = new MCvScalar();
            CvInvoke.MeanStdDev(laplacian, ref mean, ref stddev);

            double variance = stddev.V0;
            return variance * variance;
        }
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
        public RelayCommand ApplyNoiseReductionCommand { get; private set; }
        public RelayCommand ApplyContrastEnhancementCommand { get; private set; }
        public RelayCommand ApplyBrightnessAdjustmentCommand { get; private set; }
        public RelayCommand ApplyWhiteBalanceCommand { get; private set; }
        public RelayCommand ApplyMultipleFiltersCommand { get; private set; }
        public RelayCommand SaveImageCommand { get; private set; }
        public RelayCommand AnalyzeAndSuggestCommand { get; private set; }

        private RelayCommand _applyFSRCNNCommand;
        public RelayCommand ApplyFSRCNNCommand
        {
            get
            {
                return _applyFSRCNNCommand ?? (_applyFSRCNNCommand = new RelayCommand((_) => ApplyFSRCNN()));
            }
        }


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
            ApplyEdgeDetectionCommand = new RelayCommand((_) => ApplyFilter(FilterType.Bilateral));
            ApplyPinkCommand = new RelayCommand((_) => ApplyFilter(FilterType.Pink));
            ApplyNoiseReductionCommand = new RelayCommand((_) => ApplyFilter(FilterType.NoiseReduction));
            ApplyContrastEnhancementCommand = new RelayCommand((_) => ApplyFilter(FilterType.ContrastEnhancement));
            ApplyBrightnessAdjustmentCommand = new RelayCommand((_) => ApplyFilter(FilterType.BrightnessAdjustment));
            ApplyWhiteBalanceCommand = new RelayCommand((_) => ApplyFilter(FilterType.WhiteBalance));
            ApplyMultipleFiltersCommand = new RelayCommand(ApplyMultipleFilters);
            SaveImageCommand = new RelayCommand(SaveImage);
            AnalyzeAndSuggestCommand = new RelayCommand(AnalyzeAndSuggest);
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

        private void ApplyFilter(FilterType filterType, Mat imageMat = null)
        {
            if (UploadedImage == null) return;

            imageMat = BitmapExtension.ToMat(UploadedImage);
            VectorOfMat channels;
            bool isGrayImage = (imageMat.NumberOfChannels == 1);

            switch (filterType)
            {
                case FilterType.Gray:
                    if (!isGrayImage)
                    {
                        CvInvoke.CvtColor(imageMat, imageMat, ColorConversion.Bgr2Gray);
                        CvInvoke.MedianBlur(imageMat, imageMat, 3);
                    }
                    break;
                case FilterType.Warm:
                    if (!isGrayImage)
                    {
                        channels = new VectorOfMat();
                        CvInvoke.Split(imageMat, channels);
                        ApplyLookupTable(channels[2], BuildLookupTable(0.95));
                        ApplyLookupTable(channels[0], BuildLookupTable(1.1));
                        CvInvoke.Merge(channels, imageMat);
                    }
                    break;
                case FilterType.DetailEnhancing:
                    if (!isGrayImage)
                    {
                        CvInvoke.DetailEnhance(imageMat, imageMat, 10f, 0.15f);
                    }
                    break;
                case FilterType.Cold:
                    if (!isGrayImage)
                    {
                        channels = new VectorOfMat();
                        CvInvoke.Split(imageMat, channels);
                        ApplyLookupTable(channels[2], BuildLookupTable(1.25));
                        ApplyLookupTable(channels[0], BuildLookupTable(0.95));
                        CvInvoke.Merge(channels, imageMat);
                    }
                    break;
                case FilterType.Retro:
                    if (!isGrayImage)
                    {
                        channels = new VectorOfMat();
                        CvInvoke.Split(imageMat, channels);
                        ApplyLookupTable(channels[2], BuildLookupTable(0.95));
                        ApplyLookupTable(channels[1], BuildLookupTable(0.8));
                        ApplyLookupTable(channels[0], BuildLookupTable(0.95));
                        ApplyGammaCorrection(channels, 0.65);
                        CvInvoke.Merge(channels, imageMat);
                        CvInvoke.BoxFilter(imageMat, imageMat, DepthType.Cv8U, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));
                    }
                    break;
                case FilterType.Bilateral:
                    CvInvoke.CvtColor(imageMat, imageMat, ColorConversion.Bgr2Rgb);
                    CvInvoke.MedianBlur(imageMat, imageMat, 3);
                    Mat bilateral = new Mat();
                    CvInvoke.BilateralFilter(imageMat, bilateral, 9, 20, 20, BorderType.Reflect101);
                    CvInvoke.CvtColor(bilateral, bilateral, ColorConversion.Bgr2Rgb);
                    imageMat = bilateral;
                    break;
                case FilterType.Pink:
                    if (!isGrayImage)
                    {
                        channels = new VectorOfMat();
                        CvInvoke.Split(imageMat, channels);
                        ApplyLookupTable(channels[2], BuildLookupTable(1.4));
                        ApplyLookupTable(channels[1], BuildLookupTable(1.4));
                        ApplyLookupTable(channels[0], BuildLookupTable(1.2));
                        ApplyGammaCorrection(channels, 0.95);
                        CvInvoke.Merge(channels, imageMat);
                        CvInvoke.BoxFilter(imageMat, imageMat, DepthType.Cv8U, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));
                    }
                    break;
                case FilterType.GaussianBlur:
                    CvInvoke.GaussianBlur(imageMat, imageMat, new System.Drawing.Size(3, 3), 1);
                    break;
                case FilterType.Box:
                    CvInvoke.BoxFilter(imageMat, imageMat, DepthType.Cv8U, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1));
                    break;
                case FilterType.NoiseReduction:
                    if (!isGrayImage)
                    {
                        CvInvoke.FastNlMeansDenoisingColored(imageMat, imageMat, 10, 10, 7, 21);
                    }
                    break;
                case FilterType.ContrastEnhancement:
                    CvInvoke.CvtColor(imageMat, imageMat, ColorConversion.Bgr2Lab);
                    channels = new VectorOfMat();
                    CvInvoke.Split(imageMat, channels);
                    CvInvoke.EqualizeHist(channels[0], channels[0]);
                    CvInvoke.Merge(channels, imageMat);
                    CvInvoke.CvtColor(imageMat, imageMat, ColorConversion.Lab2Bgr);
                    break;
                case FilterType.BrightnessAdjustment:
                    channels = new VectorOfMat();
                    CvInvoke.Split(imageMat, channels);
                    ApplyGammaCorrection(channels, 0.45);
                    break;
                case FilterType.WhiteBalance:
                    if (!isGrayImage)
                    {
                        channels = new VectorOfMat();
                        CvInvoke.Split(imageMat, channels);
                        double avgR = CvInvoke.Mean(channels[2]).V0;
                        double avgG = CvInvoke.Mean(channels[1]).V0;
                        double avgB = CvInvoke.Mean(channels[0]).V0;
                        double avgGray = (avgB + avgG + avgR) / 3;
                        double scaleFactorR = avgGray / avgR;
                        double scaleFactorG = avgGray / avgG;
                        double scaleFactorB = avgGray / avgB;
                        CvInvoke.ConvertScaleAbs(channels[2], channels[2], scaleFactorR, 0);
                        CvInvoke.ConvertScaleAbs(channels[1], channels[1], scaleFactorG, 0);
                        CvInvoke.ConvertScaleAbs(channels[0], channels[0], scaleFactorB, 0);
                        CvInvoke.Merge(channels, imageMat);
                    }
                    break;
                case FilterType.BlurReduction:
                    double variance = GetBlurLevel(imageMat);
                    int scaleIncreaseFactor = (int)(variance / 100) + 1;
                    Mat kernel = CreateSharpeningKernel();
                    for (int i = 0; i < scaleIncreaseFactor; i++)
                    {
                        CvInvoke.Filter2D(imageMat, imageMat, kernel, new System.Drawing.Point(-1, -1));
                    }
                    break;

            }

            UploadedImage = MatToBitmapImage(imageMat);
        }
        private void ApplyMultipleFilters(object obj)
        {
            if (UploadedImage == null) return;

            List<FilterType> filters = new List<FilterType> { FilterType.Gray, FilterType.ContrastEnhancement, FilterType.NoiseReduction };
            Mat imageMat = BitmapExtension.ToMat(UploadedImage);

            foreach (var filter in filters)
            {
                ApplyFilter(filter, imageMat);
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
            System.Runtime.InteropServices.Marshal.Copy(lookupTable, 0, lookup.DataPointer, lookupTable.Length);
            CvInvoke.LUT(channel, lookup, channel);
        }
        private void ApplyGammaCorrection(VectorOfMat channels, double gamma)
        {
            Mat lut = BuildGammaLut(gamma);
            for (int i = 0; i < channels.Size; i++)
            {
                CvInvoke.LUT(channels[i], lut, channels[i]);
            }
        }
        public enum FilterType
        {
            Box,
            GaussianBlur,
            Gray,
            Warm,
            DetailEnhancing,
            Cold,
            Retro,
            Bilateral,
            Pink,
            NoiseReduction,
            ContrastEnhancement,
            BrightnessAdjustment,
            WhiteBalance,
            BlurReduction
        }

        private const double MaxNoiseLevel = 70;
        private const double MaxBlurLevel = 100;
        private const double MinContrast = 50;
        private const double MaxAvgColorValue = 100;

        private void AnalyzeAndSuggest(object obj)
        {
            if (UploadedImage == null) return;

            Mat imageMat = BitmapExtension.ToMat(UploadedImage);
            List<FilterType> suggestedFilters = AnalyzeAndSuggestFilters(imageMat);

            foreach (var filter in suggestedFilters)
            {
                ApplyFilter(filter);
            }
        }

        private List<FilterType> AnalyzeAndSuggestFilters(Mat imageMat)
        {
            double[] avgColor = GetAverageColor(imageMat);
            double brightness = GetBrightness(imageMat);
            double contrast = GetContrast(imageMat);
            double noiseLevel = GetNoiseLevel(imageMat);
            double blurLevel = GetBlurLevel(imageMat);
            bool hasHighlights = HasHighlights(imageMat);
            bool hasShadows = HasShadows(imageMat);
            bool whiteBalanceIssue = HasWhiteBalanceIssue(avgColor);


            List<FilterType> suggestedFilters = new List<FilterType>();

            if (noiseLevel < MaxNoiseLevel)
            {
                suggestedFilters.Add(FilterType.Box);
            }
            if (blurLevel < MaxBlurLevel)
            {
                suggestedFilters.Add(FilterType.BlurReduction);
            }
            if (contrast < MinContrast)
            {
                suggestedFilters.Add(FilterType.ContrastEnhancement);
            }
            if (hasShadows)
            {
                suggestedFilters.Add(FilterType.DetailEnhancing);
            }
            if (whiteBalanceIssue)
            {
                suggestedFilters.Add(FilterType.WhiteBalance);
            }
            if (hasHighlights)
            {
                suggestedFilters.Add(FilterType.BrightnessAdjustment);
            }

            if (avgColor.All(color => color < MaxAvgColorValue))
            {
                suggestedFilters.Add(FilterType.Retro);
            }

            if (!suggestedFilters.Any())
            {
                suggestedFilters.Add(FilterType.Retro);
            }

            return suggestedFilters;
        }


        private double[] GetAverageColor(Mat img)
        {
            Mat bgr = new Mat();
            if (img.NumberOfChannels == 1)
            {
                CvInvoke.CvtColor(img, bgr, ColorConversion.Gray2Bgr);
            }
            else
            {
                bgr = img;
            }

            Mat hsv = new Mat();
            CvInvoke.CvtColor(bgr, hsv, ColorConversion.Bgr2Hsv);
            MCvScalar avgHsv = CvInvoke.Mean(hsv);
            return new double[] { avgHsv.V0, avgHsv.V1, avgHsv.V2 };
        }

        private double GetBrightness(Mat img)
        {
            Mat gray = new Mat();
            if (img.NumberOfChannels == 3)
            {
                CvInvoke.CvtColor(img, gray, ColorConversion.Bgr2Gray);
            }
            else
            {
                gray = img;
            }

            MCvScalar avgBrightness = CvInvoke.Mean(gray);
            return avgBrightness.V0;
        }

        private double GetContrast(Mat img)
        {
            Mat gray = new Mat();
            if (img.NumberOfChannels == 3)
            {
                CvInvoke.CvtColor(img, gray, ColorConversion.Bgr2Gray);
            }
            else
            {
                gray = img;
            }

            MCvScalar mean = new MCvScalar();
            MCvScalar stddev = new MCvScalar();
            CvInvoke.MeanStdDev(gray, ref mean, ref stddev);
            return stddev.V0;
        }

        private double GetNoiseLevel(Mat img)
        {
            Mat bgr = new Mat();
            if (img.NumberOfChannels == 1)
            {
                CvInvoke.CvtColor(img, bgr, ColorConversion.Gray2Bgr);
            }
            else
            {
                bgr = img;
            }
            Mat floatImage = new Mat();
            bgr.ConvertTo(floatImage, DepthType.Cv32F);
            Mat mean = new Mat();
            Mat stddev = new Mat();
            CvInvoke.MeanStdDev(floatImage, mean, stddev);
            double[] stddevValues = new double[1];
            stddev.CopyTo(stddevValues);
            double noiseLevel = stddevValues[0];
            return noiseLevel;
        }

        private double GetBlurLevel(Mat img)
        {
            Mat gray = new Mat();
            if (img.NumberOfChannels == 3)
            {
                CvInvoke.CvtColor(img, gray, ColorConversion.Bgr2Gray);
            }
            else
            {
                gray = img;
            }

            Mat laplacian = new Mat();
            CvInvoke.Laplacian(gray, laplacian, DepthType.Cv64F);

            MCvScalar mean = new MCvScalar();
            MCvScalar stddev = new MCvScalar();
            CvInvoke.MeanStdDev(laplacian, ref mean, ref stddev);

            double variance = stddev.V0;
            return variance * variance;
        }

        private bool HasHighlights(Mat img)
        {
            Mat gray = new Mat();
            if (img.NumberOfChannels == 1)
            {
                CvInvoke.CvtColor(img, img, ColorConversion.Gray2Bgr);
            }
            CvInvoke.CvtColor(img, gray, ColorConversion.Bgr2Gray);

            double highlightThreshold = 240;
            Mat highlightMask = new Mat();
            CvInvoke.Threshold(gray, highlightMask, highlightThreshold, 255, ThresholdType.Binary);
            int highlightCount = CvInvoke.CountNonZero(highlightMask);
            return highlightCount > (gray.Rows * gray.Cols * 0.05);
        }

        private bool HasShadows(Mat img)
        {
            Mat gray = new Mat();
            if (img.NumberOfChannels == 1)
            {
                CvInvoke.CvtColor(img, img, ColorConversion.Gray2Bgr);
            }
            CvInvoke.CvtColor(img, gray, ColorConversion.Bgr2Gray);
            double shadowThreshold = 20;
            Mat shadowMask = new Mat();
            CvInvoke.Threshold(gray, shadowMask, shadowThreshold, 255, ThresholdType.BinaryInv);
            int shadowCount = CvInvoke.CountNonZero(shadowMask);
            return shadowCount > (gray.Rows * gray.Cols * 0.05);
        }

        private bool HasWhiteBalanceIssue(double[] avgColor)
        {
            double maxDiff = 100;
            return Math.Abs(avgColor[0] - avgColor[1]) > maxDiff ||
                   Math.Abs(avgColor[0] - avgColor[2]) > maxDiff ||
                   Math.Abs(avgColor[1] - avgColor[2]) > maxDiff;
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
