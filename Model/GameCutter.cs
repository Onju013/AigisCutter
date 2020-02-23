using ImageLib;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace AigisCutter.Model
{
    public static class GameCutter
    {
        private static int Diff(
            int value,
            int nextValue)
        {
            return nextValue - value;
        }

        private static int DiffWhiteCount(
            int value,
            int nextValue,
            int delta)
        {
            int min = 0xFF - delta;

            if (value < min && nextValue < min)
                return 0;

            if (Math.Abs(nextValue - value) < delta)
                return 0;

            if (value > nextValue)
                return -1;

            return 1;
        }

        private static int DiffBlack(
            int value,
            int nextValue,
            int delta = 0)
        {
            if (value > delta && nextValue > delta)
                return 0;

            return value - nextValue;
        }

        private static int DiffBlackCount(
            int value,
            int nextValue,
            int delta = 0)
        {
            if (value > delta && nextValue > delta)
                return 0;

            if (Math.Abs(nextValue - value) < delta)
                return 0;

            if (value < nextValue)
                return -1;

            return 1;
        }

        public static void CutPcSq(
            string outImagePath,
            string srcImagePath,
            int cutWidth,
            int cutHeight)
        {
            CutBySq(outImagePath,
                srcImagePath,
                cutWidth,
                cutHeight,
                Diff);
        }

        public static void CutPcCount(
            string outImagePath,
            string srcImagePath,
            int cutWidth,
            int cutHeight,
            int delta)
        {
            CutBySq(outImagePath,
                srcImagePath,
                cutWidth,
                cutHeight,
                (v, n) => { return DiffWhiteCount(v, n, delta); });
        }

        public static void CutIosAndroid(
            string outImagePath,
            string srcImagePath,
            int delta)
        {
            if (!File.Exists(srcImagePath))
                throw new FileNotFoundException($"{srcImagePath}が見つかりません。");

            int cutHeight;

            using (var temp = new Bitmap(srcImagePath))
            {
                cutHeight = temp.Height;
            }

            CutByEdge(outImagePath,
                srcImagePath,
                cutHeight * 3 / 2,
                cutHeight,
                (v, n) => { return DiffBlackCount(v, n, delta); });
        }

        public static void CutIosPoint(
            string outImagePath,
            string srcImagePath,
            int homebarHeight)
        {
            if (!File.Exists(srcImagePath))
                throw new FileNotFoundException($"{srcImagePath}が見つかりません。");

            int srcWidth;
            int srcHeight;

            using (var temp = new Bitmap(srcImagePath))
            {
                srcWidth = temp.Width;
                srcHeight = temp.Height;
            }

            if (srcWidth > srcHeight * 3 / 2)
            {
                int cutHeight = srcHeight - homebarHeight;
                int cutWidth = cutHeight * 3 / 2;

                OutputImage(
                    outImagePath,
                    srcImagePath,
                    (srcWidth - cutWidth) / 2,
                    0,
                    cutWidth,
                    cutHeight);
            }
            else
            {
                int cutWidth = srcWidth - homebarHeight;
                int cutHeight = srcWidth * 2 / 3;

                OutputImage(
                    outImagePath,
                    srcImagePath,
                    0,
                    (srcHeight - cutHeight) / 2,
                    srcWidth,
                    cutWidth);
            }
        }

        private static void CutBySq(
            string outImagePath,
            string srcImagePath,
            int cutWidth,
            int cutHeight,
            Func<int, int, int> diffFunc)
        {
            int left = 0;
            int up = 0;

            if (!File.Exists(srcImagePath))
                throw new FileNotFoundException($"{srcImagePath}が見つかりません。");

            ImageData srcData = new ImageData(srcImagePath);

            if (srcData.Width < cutWidth || srcData.Height < cutHeight)
                throw new ArgumentOutOfRangeException(
                    $"{nameof(srcData.Width)} x {nameof(srcData.Height)}",
                    null,
                    $"画像サイズ{srcData.Width}x{srcData.Height}に対して"
                    + $"切り取りサイズ{cutWidth}x{cutHeight}が大きすぎます。");

            if (srcData.Width == cutWidth || srcData.Height == cutHeight)
            {
                CutByEdge(outImagePath, srcImagePath, cutWidth, cutHeight, diffFunc);
                return;
            }

#if DEBUG
            string basePath =
                Path.GetDirectoryName(srcImagePath)
                + "\\"
                + Path.GetFileNameWithoutExtension(srcImagePath);
            srcData.SaveGrayImage(basePath + "_gray.png", true);
#endif  //DEBUG

            //x方向微分
            var dxData = new ImageData(srcData.Width - 1, srcData.Height);
            for (int y = 0; y < dxData.Height; ++y)
            {
                for (int x = 0; x < dxData.Width; ++x)
                {
                    dxData.SetValue(
                        x,
                        y,
                        diffFunc(srcData.GetValue(x, y), srcData.GetValue(x + 1, y)));
                }
            }
#if DEBUG
            dxData.SaveGrayImage(basePath + "_dx.png", true);
#endif  //DEBUG

            //x方向微分のy方向SUM
            var dxSum = new ImageData(dxData.Width, dxData.Height - cutHeight);
            for (int x = 0; x < dxSum.Width; ++x)
            {
                //先頭だけ先に計算
                var dxSumTop = Enumerable.Range(0, cutHeight)
                    .Select(cy => dxData.GetValue(x, cy))
                    .Aggregate((sum, next) => sum += next);
                dxSum.SetValue(
                    x,
                    0,
                    dxSumTop);

                for (int y = 1; y < dxSum.Height; ++y)
                {
                    var temp = dxSum.GetValue(x, y - 1);
                    temp -= dxData.GetValue(x, y - 1);
                    temp += dxData.GetValue(x, y + cutHeight - 1);
                    dxSum.SetValue(
                        x,
                        y,
                        temp);
                }
            }
#if DEBUG
            dxSum.SaveGrayImage(basePath + "_dxSum.png", true);
#endif  //DEBUG

            //y方向微分
            var dyData = new ImageData(srcData.Width, srcData.Height - 1);
            for (int y = 0; y < dyData.Height; ++y)
            {
                for (int x = 0; x < dyData.Width; ++x)
                {
                    dyData.SetValue(
                        x,
                        y,
                        diffFunc(srcData.GetValue(x, y), srcData.GetValue(x, y + 1)));
                }
            }
#if DEBUG
            dyData.SaveGrayImage(basePath + "_dy.png", true);
#endif  //DEBUG

            //y方向微分のx方向SUM
            var dySum = new ImageData(dyData.Width - cutWidth, dyData.Height);
            for (int y = 0; y < dyData.Height; ++y)
            {
                //先頭だけ先に計算
                var dySumTop = Enumerable.Range(0, cutWidth)
                .Select(cx => dyData.GetValue(cx, y))
                .Aggregate((sum, next) => sum += next);
                dySum.SetValue(
                    0,
                    y,
                    dySumTop);

                for (int x = 1; x < dySum.Width; ++x)
                {
                    var temp = dySum.GetValue(x - 1, y);
                    temp -= dyData.GetValue(x - 1, y);
                    temp += dyData.GetValue(x + cutWidth - 1, y);
                    dySum.SetValue(
                        x,
                        y,
                        temp);
                }
            }
#if DEBUG
            dySum.SaveGrayImage(basePath + "_dySum.png", true);
#endif  //DEBUG

            //和
            var sumTable = new ImageData(srcData.Width - cutWidth - 1, srcData.Height - cutHeight - 1);
            int maxValue = int.MinValue;
            for (int y = 0; y < sumTable.Height; ++y)
            {
                for (int x = 0; x < sumTable.Width; ++x)
                {
                    var value =
                        dxSum.GetValue(x + cutWidth, y)
                        - dxSum.GetValue(x, y)
                        + dySum.GetValue(x, y + cutHeight)
                        - dySum.GetValue(x, y);

                    sumTable.SetValue(
                        x,
                        y,
                        value);

                    if (value > maxValue)
                    {
                        left = x + 1;
                        up = y + 1;
                        maxValue = value;
                    }
                }
            }
#if DEBUG
            sumTable.SaveGrayImage(basePath + "_sumTable.png", true);
            Debug.WriteLine($"({left},{up})={maxValue}");
#endif  //DEBUG

            //出力
            OutputImage(outImagePath, srcImagePath, left, up, cutWidth, cutHeight);
        }

        private static void CutByEdge(
            string outImagePath,
            string srcImagePath,
            int cutWidth,
            int cutHeight,
            Func<int, int, int> diffFunc)
        {
            if (!File.Exists(srcImagePath))
                throw new FileNotFoundException($"{srcImagePath}が見つかりません。");

            ImageData srcData = new ImageData(srcImagePath);

            if (srcData.Width < cutWidth || srcData.Height < cutHeight)
                throw new ArgumentOutOfRangeException(
                    $"{nameof(srcData.Width)} x {nameof(srcData.Height)}",
                    null,
                    $"画像サイズ{srcData.Width}x{srcData.Height}に対して"
                    + $"切り取りサイズ{cutWidth}x{cutHeight}が大きすぎます。");

#if DEBUG
            string basePath =
                Path.GetDirectoryName(srcImagePath)
                + "\\"
                + Path.GetFileNameWithoutExtension(srcImagePath);
            srcData.SaveGrayImage(basePath + "_gray.png", true);
#endif  //DEBUG

            int left = 0;
            int up = 0;

            if (srcData.Width > cutWidth)
            {
                //x方向微分
                var dxData = new ImageData(srcData.Width - 1, srcData.Height);
                for (int y = 0; y < dxData.Height; ++y)
                {
                    for (int x = 0; x < dxData.Width; ++x)
                    {
                        dxData.SetValue(
                            x,
                            y,
                            diffFunc(srcData.GetValue(x, y), srcData.GetValue(x + 1, y)));
                    }
                }
#if DEBUG
                dxData.SaveGrayImage(basePath + "_dx.png", true);
#endif  //DEBUG

                //x方向微分のy方向SUM
                var dxSum = new int[dxData.Width];
                for (int x = 0; x < dxData.Width; ++x)
                {
                    dxSum[x] =
                        Enumerable.Range(0, dxData.Height)
                        .Sum(y => dxData.GetValue(x, y));
                }

#if DEBUG
                var dxSumData = new ImageData(dxSum.Length, 100);
                for(int y = 0; y < dxSumData.Height; ++y)
                    for(int x = 0; x < dxSumData.Width; ++x)
                    {
                        dxSumData.SetValue(
                            x,
                            y,
                            dxSum[x]);
                    }
                dxSumData.SaveGrayImage(basePath + "_dxSum.png", true);
#endif  //DEBUG

                //左端を特定する
                left =
                    Enumerable.Range(0, dxSum.Length - cutWidth)
                    .Select(x => dxSum[x + cutWidth] - dxSum[x])
                    .Select((v, i) => new { V = v, I = i })
                    .Aggregate((max, current) => current.V > max.V ? current : max)
                    .I + 1;

                if ((cutHeight & 1) == 1)
                {
                    var left2 =
                        Enumerable.Range(0, dxSum.Length - cutWidth - 1)
                        .Select(x => dxSum[x + cutWidth + 1] - dxSum[x])
                        .Select((v, i) => new { V = v, I = i })
                        .Aggregate((max, current) => current.V > max.V ? current : max)
                        .I + 1;

                    if (dxSum[left2 + cutWidth] - dxSum[left2 - 1]
                        > dxSum[left + cutWidth - 1] - dxSum[left - 1])
                    {
                        left = left2;
                        ++cutWidth;
                    }
                }
            }

            if (srcData.Height > cutHeight)
            {
                //y方向微分
                var dyData = new ImageData(cutWidth, srcData.Height - 1);
                for (int y = 0; y < dyData.Height; ++y)
                    for (int x = 0; x < dyData.Width; ++x)
                    {
                        dyData.SetValue(
                            x,
                            y,
                            diffFunc(srcData.GetValue(left + x, y), srcData.GetValue(left + x, y + 1)));
                    }
#if DEBUG
                dyData.SaveGrayImage(basePath + "_dy.png", true);
#endif  //DEBUG

                //y方向微分のx方向SUM
                var dySum = new int[dyData.Height];
                for (int y = 0; y < dyData.Height; ++y)
                {
                    dySum[y] =
                        Enumerable.Range(0, dyData.Width)
                        .Sum(x => dyData.GetValue(x, y));
                }

#if DEBUG
                var dySumData = new ImageData(100, dySum.Length);
                for(int y = 0; y < dySumData.Height; ++y)
                    for(int x = 0; x < dySumData.Width; ++x)
                    {
                        dySumData.SetValue(
                            x,
                            y,
                            dySum[y]);
                    }
                dySumData.SaveGrayImage(basePath + "_dySum.png", true);
#endif  //DEBUG

                //上端を特定する
                up =
                    Enumerable.Range(0, dySum.Length - cutHeight)
                    .Select(y => dySum[y + cutHeight] - dySum[y])
                    .Select((v, i) => new { V = v, I = i })
                    .Aggregate((max, current) => current.V > max.V ? current : max)
                    .I + 1;
            }

            //出力
            OutputImage(outImagePath, srcImagePath, left, up, cutWidth, cutHeight);
        }

        private static void OutputImage(
            string outImagePath,
            string srcImagePath,
            int left,
            int up,
            int width,
            int height)
        {
            using (var srcImage = new Bitmap(srcImagePath))
            using (var outImage = new Bitmap(width, height))
            using (var g = Graphics.FromImage(outImage))
            {
                var destRect = new Rectangle(0, 0, width, height);
                var srcRect = new Rectangle(left, up, width, height);
                g.DrawImage(srcImage, destRect, srcRect, GraphicsUnit.Pixel);
                outImage.Save(outImagePath, ImageFormat.Png);
            }

            var timeStamp = TimeStamp.GetTimeStamp(srcImagePath);

            File.SetCreationTime(outImagePath, timeStamp.CreationTime);
            File.SetLastWriteTime(outImagePath, timeStamp.LastWriteTime);
            File.SetLastAccessTime(outImagePath, timeStamp.LastAccessTime);
        }
    }
}