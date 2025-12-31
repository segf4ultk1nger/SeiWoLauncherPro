using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SeiWoLauncherPro.Utils
{
    /// <summary>
    /// 提供基于指针操作的高性能图像模糊算法
    /// </summary>
    public static unsafe class UnsafeBlur
    {
        public static void ApplyStackBlur(Bitmap bmp, int radius)
        {
            if (radius < 1) return;

            int w = bmp.Width;
            int h = bmp.Height;
            BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            try
            {
                byte* ptr = (byte*)data.Scan0;
                int stride = data.Stride;
                int byteCount = stride * h;

                // 3-Pass Box Blur 模拟高斯模糊
                byte[] tempBuffer = new byte[byteCount];
                fixed (byte* tempPtr = tempBuffer)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        HorizontalBlur(ptr, tempPtr, w, h, stride, radius);
                        VerticalBlur(tempPtr, ptr, w, h, stride, radius);
                    }
                }
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        private static void HorizontalBlur(byte* src, byte* dest, int w, int h, int stride, int r)
        {
            Parallel.For(0, h, y =>
            {
                byte* rowSrc = src + y * stride;
                byte* rowDest = dest + y * stride;

                for (int x = 0; x < w; x++)
                {
                    long bSum = 0, gSum = 0, rSum = 0;
                    int count = 0;

                    int left = x - r; if (left < 0) left = 0;
                    int right = x + r; if (right >= w) right = w - 1;

                    for (int k = left; k <= right; k++)
                    {
                        byte* px = rowSrc + k * 4;
                        bSum += px[0]; gSum += px[1]; rSum += px[2];
                        count++;
                    }

                    byte* dPx = rowDest + x * 4;
                    dPx[0] = (byte)(bSum / count);
                    dPx[1] = (byte)(gSum / count);
                    dPx[2] = (byte)(rSum / count);
                    dPx[3] = 255;
                }
            });
        }

        private static void VerticalBlur(byte* src, byte* dest, int w, int h, int stride, int r)
        {
            Parallel.For(0, w, x =>
            {
                for (int y = 0; y < h; y++)
                {
                    long bSum = 0, gSum = 0, rSum = 0;
                    int count = 0;

                    int top = y - r; if (top < 0) top = 0;
                    int bottom = y + r; if (bottom >= h) bottom = h - 1;

                    for (int k = top; k <= bottom; k++)
                    {
                        byte* px = src + k * stride + x * 4;
                        bSum += px[0]; gSum += px[1]; rSum += px[2];
                        count++;
                    }

                    byte* dPx = dest + y * stride + x * 4;
                    dPx[0] = (byte)(bSum / count);
                    dPx[1] = (byte)(gSum / count);
                    dPx[2] = (byte)(rSum / count);
                    dPx[3] = 255;
                }
            });
        }
    }
}