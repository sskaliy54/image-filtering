
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using STimg.Helpers;
using STimg.View;
using System.IO;
using Microsoft.Win32;
using System.Windows.Media.Imaging;
using Emgu.CV.CvEnum;
using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.DnnSuperres;
using Emgu.CV.Structure;
using BitmapExtension = STimg.Extension.BitmapExtension;
using STimg.Enums;
using STimg.Services;
using System.Reflection;


namespace STimg.ViewModel
{
    public class EditPageVM : BaseVM
    {
        private const double MaxNoiseLevel = 70;
        private const double MaxBlurLevel = 100;
        private const double MinContrast = 50;
        private const double MaxAvgColorValue = 100;

        private BitmapImage _uploadedImage;
        private RelayCommand _applyFSRCNNCommand;
        private RelayCommand _resetImageCommand;
        private BitmapImage _originalImage;


        private FilterImageService filterImageService = new FilterImageService();
        public RelayCommand UploadCommand { get; private set; }
        public RelayCommand ApplyBoxFilterCommand { get; private set; }
        public RelayCommand ApplyGaussianBlurCommand { get; private set; }
        public RelayCommand ApplyGrayCommand { get; private set; }
        public RelayCommand ApplyWarmCommand { get; private set; }
        public RelayCommand ApplyDetailEnhancingCommand { get; private set; }
        public RelayCommand ApplyColdCommand { get; private set; }
        public RelayCommand ApplyRetroCommand { get; private set; }
        public RelayCommand ApplyBilateralCommand { get; private set; }
        public RelayCommand ApplyPinkCommand { get; private set; }
        public RelayCommand ApplyNoiseReductionCommand { get; private set; }
        public RelayCommand ApplyContrastEnhancementCommand { get; private set; }
        public RelayCommand ApplyBrightnessAdjustmentCommand { get; private set; }
        public RelayCommand ApplyWhiteBalanceCommand { get; private set; }
        public RelayCommand ApplyMultipleFiltersCommand { get; private set; }
        public RelayCommand SaveImageCommand { get; private set; }
        public RelayCommand AnalyzeAndSuggestCommand { get; private set; }

        public RelayCommand ApplyFSRCNNCommand
        {
            get
            {
                return _applyFSRCNNCommand ?? (_applyFSRCNNCommand = new RelayCommand((_) => ApplyFSRCNN()));
            }
        }

        public RelayCommand ResetImageCommand
        {
            get
            {
                return _resetImageCommand ?? (_resetImageCommand = new RelayCommand((_) => ResetImage()));
            }
        }

        public BitmapImage UploadedImage
        {
            get { return _uploadedImage; }
            set
            {
                _uploadedImage = value;
                OnPropertyChanged(nameof(UploadedImage));
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
            ApplyBilateralCommand = new RelayCommand((_) => ApplyFilter(FilterType.Bilateral));
            ApplyPinkCommand = new RelayCommand((_) => ApplyFilter(FilterType.Pink));
            ApplyNoiseReductionCommand = new RelayCommand((_) => ApplyFilter(FilterType.NoiseReduction));
            ApplyContrastEnhancementCommand = new RelayCommand((_) => ApplyFilter(FilterType.ContrastEnhancement));
            ApplyBrightnessAdjustmentCommand = new RelayCommand((_) => ApplyFilter(FilterType.BrightnessAdjustment));
            ApplyWhiteBalanceCommand = new RelayCommand((_) => ApplyFilter(FilterType.WhiteBalance));
            ApplyMultipleFiltersCommand = new RelayCommand(ApplyMultipleFilters);
            SaveImageCommand = new RelayCommand(SaveImage);
            AnalyzeAndSuggestCommand = new RelayCommand(AnalyzeAndSuggest);
        }

        public void ApplyFilter(FilterType filterType, Mat imageMat = null)
        {
            if (UploadedImage == null) return;

            imageMat = STimg.Extension.BitmapExtension.ToMat(UploadedImage);
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
                        filterImageService.ApplyLookupTable(channels, new[] { (2, 0.95), (0, 1.1) });
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
                        filterImageService.ApplyLookupTable(channels, new[] { (2, 1.25), (0, 0.95) });
                        CvInvoke.Merge(channels, imageMat);
                    }
                    break;
                case FilterType.Retro:
                    if (!isGrayImage)
                    {
                        channels = new VectorOfMat();
                        CvInvoke.Split(imageMat, channels);
                        filterImageService.ApplyLookupTable(channels, new[] { (2, 0.95), (1, 0.8), (0, 0.95) });
                        filterImageService.ApplyGammaCorrection(channels, 0.65);
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
                        filterImageService.ApplyLookupTable(channels, new[] { (2, 1.4), (1, 1.4), (0, 1.2) });
                        filterImageService.ApplyGammaCorrection(channels, 0.95);
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
                    if (!isGrayImage)
                    {
                        CvInvoke.CvtColor(imageMat, imageMat, ColorConversion.Bgr2Lab);
                        channels = new VectorOfMat();
                        CvInvoke.Split(imageMat, channels);
                        CvInvoke.EqualizeHist(channels[0], channels[0]);
                        CvInvoke.Merge(channels, imageMat);
                        CvInvoke.CvtColor(imageMat, imageMat, ColorConversion.Lab2Bgr);
                    }
                    break;
                case FilterType.BrightnessAdjustment:
                    channels = new VectorOfMat();
                    CvInvoke.Split(imageMat, channels);
                    filterImageService.ApplyGammaCorrection(channels, 0.45);
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
                    double variance = filterImageService.GetBlurLevel(imageMat);
                    int scaleIncreaseFactor = (int)(variance / 100) + 1;
                    Mat kernel = filterImageService.CreateSharpeningKernel();
                    for (int i = 0; i < scaleIncreaseFactor; i++)
                    {
                        CvInvoke.Filter2D(imageMat, imageMat, kernel, new System.Drawing.Point(-1, -1));
                    }
                    break;

            }

            UploadedImage = filterImageService.MatToBitmapImage(imageMat);
        }

        public void ApplyFSRCNN()
        {
            if (UploadedImage == null) return;
            Mat imageMat = STimg.Extension.BitmapExtension.ToMat(UploadedImage);
            double variance = filterImageService.ComputeLaplacianVariance(imageMat);
            const double blurThreshold = 100.0;
            bool isGrayImage = (imageMat.NumberOfChannels == 1);


            if (variance < blurThreshold)
            {
                Mat denoisedImage = new Mat();
                CvInvoke.BoxFilter(imageMat, denoisedImage, DepthType.Cv8U, new System.Drawing.Size(3, 3), new System.Drawing.Point(-1, -1), true, BorderType.Default);

                int scaleIncreaseFactor = (int)(variance / blurThreshold) + 1;
                int enhanceSteps = 3 * scaleIncreaseFactor;
                Mat kernel = filterImageService.CreateSharpeningKernel();
                for (int i = 0; i < enhanceSteps; i++)
                {
                    CvInvoke.Filter2D(denoisedImage, imageMat, kernel, new System.Drawing.Point(-1, -1));
                }
            }
            CvInvoke.ConvertScaleAbs(imageMat, imageMat, 0.85, 5);
            if (!isGrayImage)
            {
                CvInvoke.DetailEnhance(imageMat, imageMat, 5f, 0.025f);
            }

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
                UploadedImage = filterImageService.MatToBitmapImage(result);
            }
        }
        public void AnalyzeAndSuggest(object obj)
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
            double[] avgColor = filterImageService.GetAverageColor(imageMat);
            double brightness = filterImageService.GetBrightness(imageMat);
            double contrast = filterImageService.GetContrast(imageMat);
            double noiseLevel = filterImageService.GetNoiseLevel(imageMat);
            double blurLevel = filterImageService.GetBlurLevel(imageMat);
            bool hasHighlights = filterImageService.HasHighlights(imageMat);
            bool hasShadows = filterImageService.HasShadows(imageMat);
            bool whiteBalanceIssue = filterImageService.HasWhiteBalanceIssue(avgColor);

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

        private void ShowCustomMessageBox(string message)
        {
            CustomMessageBox customMessageBox = new CustomMessageBox(message);
            customMessageBox.ShowDialog();
        }
        private void UploadImage(object obj)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp";

            if (openFileDialog.ShowDialog() == true)
            {
                FileInfo fileInfo = new FileInfo(openFileDialog.FileName);

                if (fileInfo.Length > 5 * 1024 * 1024) 
                {
                    ShowCustomMessageBox("The selected file is larger than 5MB. Please select a smaller file.");
                }
                else
                {
                    UploadedImage = new BitmapImage(new Uri(openFileDialog.FileName));
                    _originalImage = new BitmapImage(new Uri(openFileDialog.FileName));
                }
            }
        }

        private void ResetImage()
        {
            if (_originalImage != null)
            {
                UploadedImage = _originalImage;
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
                Mat imageMat = STimg.Extension.BitmapExtension.ToMat(UploadedImage);

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
        private void ApplyMultipleFilters(object obj)
        {
            if (UploadedImage == null) return;

            List<FilterType> filters = new List<FilterType> { FilterType.Gray, FilterType.ContrastEnhancement, FilterType.NoiseReduction };
            Mat imageMat = BitmapExtension.ToMat(UploadedImage);

            foreach (var filter in filters)
            {
                ApplyFilter(filter, imageMat);
            }
            UploadedImage = filterImageService.MatToBitmapImage(imageMat);
        }

    }
}
