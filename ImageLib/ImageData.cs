using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ImageLib
{
    public class ImageData
    {
        public int[] Values { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public double WeightR { get; set; } = 0.299;
        public double WeightG { get; set; } = 0.587;
        public double WeightB { get; set; } = 0.114;

        public ImageData()
        { }

        public ImageData(
            int[] values,
            int width,
            int height)
        {
            Values = new int[width * height];
            Width = width;
            Height = height;

            if(values == null)
                return;

            if(values.Length < width * height)
                throw new ArgumentException($"長さが足りません 長さ：{values.Length} 必要な長さ：{width * height}");

            Array.Copy(values, this.Values, width * height);
        }

        public ImageData(
            int width,
            int height)
            : this(null, width, height)
        { }

        public ImageData(
            ImageData imageData)
            : this(imageData.Values, imageData.Width, imageData.Height)
        { }

        public ImageData(
            string imagePath)
        {
            if(!File.Exists(imagePath))
                throw new FileNotFoundException(imagePath);

            using(var bitmap = new Bitmap(imagePath))
            {
                LoadFromBitmap(bitmap);
            }
        }

        public ImageData(
            string imagePath,
            double weightR,
            double weightG,
            double weightB)
        {
            if(imagePath == null)
                throw new ArgumentOutOfRangeException("画像ファイルパスが指定されていません");

            if(!File.Exists(imagePath))
                throw new FileNotFoundException(imagePath);

            WeightR = weightR;
            WeightG = weightG;
            WeightB = weightB;

            using(var bitmap = new Bitmap(imagePath))
            {
                LoadFromBitmap(bitmap);
            }
        }

        public ImageData(
            Bitmap bitmap)
        {
            LoadFromBitmap(bitmap);
        }

        public int GetValue(
            int x,
            int y)
        {
            return Values[y * Width + x];
        }

        public void SetValue(
            int x,
            int y,
            int value)
        {
            Values[y * Width + x] = value;
        }

        public void SetValueFromRgb(
            int x,
            int y,
            int r,
            int g,
            int b)
        {
            SetValue(
                x,
                y,
                (int)(WeightR * r + WeightG * g + WeightB * b));
        }

        public void LoadFromBitmap(Bitmap bitmap)
        {
            Width = bitmap.Width;
            Height = bitmap.Height;
            Values = new int[Width * Height];

            var bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, Width, Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

            var bytes = new byte[bitmapData.Stride * Height];
            Marshal.Copy(bitmapData.Scan0, bytes, 0, bytes.Length);

            int index = 0;
            int nResidual = bitmapData.Stride - Width * 3;

            //グレイスケール化
            for(int y = 0; y < Height; y++)
            {
                for(int x = 0; x < Width; x++)
                {
                    SetValueFromRgb(
                        x,
                        y,
                        bytes[index + 2],
                        bytes[index + 1],
                        bytes[index]);
                    index += 3;
                }
                index += nResidual;
            }

            bitmap.UnlockBits(bitmapData);            
        }

        public void SaveGrayImage(
            string outImagePath,
            bool clobber = false)
        {
            if(!clobber && File.Exists(outImagePath))
                throw new Exception($"ファイルが存在します（{outImagePath}）");

            int min = Values.Min();
            int max = Values.Max();

            var buff = new byte[Width * Height * 4];
            for(int i = 0; i < Values.Length; ++i)
            {
                if(min == max)
                    continue;
                var value = (byte)(255 * (Values[i] - min) / (max - min));
                buff[i * 4] = value;
                buff[i * 4 + 1] = value;
                buff[i * 4 + 2] = value;
                buff[i * 4 + 3] = 255;
            }

            using(var img = new Bitmap(Width, Height))
            {
                var data = img.LockBits(
                    new Rectangle(0, 0, Width, Height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppArgb);

                Marshal.Copy(buff, 0, data.Scan0, buff.Length);

                img.UnlockBits(data);
                img.Save(outImagePath, ImageFormat.Png);
            }
        }

        public void OutputGrayImage(
            string outImagePath,
            int destX,
            int destY,
            int width,
            int height,
            bool clobber = false)
        {
            if(!clobber && File.Exists(outImagePath))
                throw new Exception($"ファイルが存在します（{outImagePath}）");

            if(destX + width > Width
                || destY + height > Height)
                throw new ArgumentOutOfRangeException($"切り取りサイズが異常です（destX:{destX}, destY:{destY}, width:{width}, height:{height}）");

            var smallData = new ImageData(width, height);

            for(int y = 0; y < height; ++y)
                for(int x = 0; x < width; ++x)
                {
                    smallData.SetValue(
                        x,
                        y,
                        GetValue(destX + x, destY + y));
                }

            smallData.SaveGrayImage(outImagePath, clobber);
        }
    }
}