using Emgu.CV.CvEnum;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows;

namespace STimg.Extension
{
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
