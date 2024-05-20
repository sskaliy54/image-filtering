using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV;
using System;
using Emgu.CV.Util;
using STimg.Enums;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;


namespace STimg.Services
{
    public class FilterImageService
    {
        public BitmapImage MatToBitmapImage(Mat imageMat)
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

        public byte[] BuildLookupTable(double gamma)
        {
            byte[] lookupTable = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                lookupTable[i] = (byte)Math.Min(255, (int)(Math.Pow(i / 255.0, gamma) * 255.0));
            }
            return lookupTable;
        }

        public Mat BuildGammaLut(double gamma)
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

        public void ApplyLookupTable(Mat channel, byte[] lookupTable)
        {
            Mat lookup = new Mat(256, 1, DepthType.Cv8U, 1);
            System.Runtime.InteropServices.Marshal.Copy(lookupTable, 0, lookup.DataPointer, lookupTable.Length);
            CvInvoke.LUT(channel, lookup, channel);
        }

        public void ApplyGammaCorrection(VectorOfMat channels, double gamma)
        {
            Mat lut = BuildGammaLut(gamma);
            for (int i = 0; i < channels.Size; i++)
            {
                CvInvoke.LUT(channels[i], lut, channels[i]);
            }
        }

        public Mat CreateSharpeningKernel()
        {
            float[] kernelValues = { -1, -1, -1, -1, 9, -1, -1, -1, -1 };
            Mat kernel = new Mat(3, 3, DepthType.Cv32F, 1);
            kernel.SetTo(kernelValues);
            return kernel;
        }

        public double ComputeLaplacianVariance(Mat image)
        {
            Mat gray = new Mat();
            if (image.NumberOfChannels == 3)
            {
                CvInvoke.CvtColor(image, gray, ColorConversion.Bgr2Gray);
            }
            else
            {
                gray = image.Clone();
            }

            Mat laplacian = new Mat();
            CvInvoke.Laplacian(gray, laplacian, DepthType.Cv64F);

            MCvScalar mean = new MCvScalar();
            MCvScalar stddev = new MCvScalar();
            CvInvoke.MeanStdDev(laplacian, ref mean, ref stddev);

            double variance = stddev.V0;
            return variance * variance;
        }
    

    public double[] GetAverageColor(Mat img)
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

        public double GetBrightness(Mat img)
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

        public double GetContrast(Mat img)
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

        public double GetNoiseLevel(Mat img)
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

        public double GetBlurLevel(Mat img)
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

        public bool HasHighlights(Mat img)
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

        public bool HasShadows(Mat img)
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

        public bool HasWhiteBalanceIssue(double[] avgColor)
        {
            double maxDiff = 100;
            return Math.Abs(avgColor[0] - avgColor[1]) > maxDiff ||
                   Math.Abs(avgColor[0] - avgColor[2]) > maxDiff ||
                   Math.Abs(avgColor[1] - avgColor[2]) > maxDiff;
        }
    }
}
