using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SeiWoLauncherPro.Utils {
    public static class NoiseTextureGenerator {
        /// <summary>
        /// 生成一个高性能的噪点画笔
        /// </summary>
        /// <param name="opacity">噪点的透明度 (0.0 - 1.0)，建议 0.02 ~ 0.05 之间</param>
        /// <param name="scale">纹理缩放比例，越小噪点越细密</param>
        public static ImageBrush CreateNoiseBrush(double opacity = 0.03, double scale = 1.0) {
            // 1. 定义纹理大小 (256x256 足够平铺了，太大会浪费显存)
            int width = 256;
            int height = 256;
            int bytesPerPixel = 4; // BGRA 格式
            int stride = width * bytesPerPixel;
            byte[] pixels = new byte[height * stride];

            // 2. 随机数生成器
            var rand = new Random();

            // 3. 暴力填充像素 (Bit-banging)
            for (int i = 0; i < pixels.Length; i += 4) {
                // 生成灰度噪点：R=G=B
                // 这种做法模拟的是“单色胶片颗粒”感
                byte gray = (byte)rand.Next(0, 256);

                // 核心逻辑：我们不直接改颜色，而是让颜色接近黑/白，利用 Alpha 通道控制强度
                // 这里我们生成纯黑或纯白的噪点，效果更像真实磨砂
                // 如果想要更柔和，可以用纯白噪点

                pixels[i]     = gray; // Blue
                pixels[i + 1] = gray; // Green
                pixels[i + 2] = gray; // Red

                // Alpha 通道：基于传入的 opacity 动态计算
                // 这里做一个随机扰动，让噪点有深有浅，而不是死板的一样透
                double randomAlpha = opacity * (0.5 + rand.NextDouble());
                pixels[i + 3] = (byte)(255 * randomAlpha);
            }

            // 4. 创建位图源
            var bitmap = BitmapSource.Create(
                width, height,
                96, 96, // DPI
                PixelFormats.Bgra32,
                null,
                pixels,
                stride);

            // 冻结位图以支持跨线程访问并提升性能
            bitmap.Freeze();

            // 5. 封装成 Brush
            var brush = new ImageBrush(bitmap) {
                // 关键：开启平铺模式
                TileMode = TileMode.Tile,
                // 关键：使用绝对单位，确保无论窗口多大，噪点颗粒大小不变
                ViewportUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(0, 0, width * scale, height * scale),
                Opacity = 1.0 // 画笔本身不透明，透明度由像素控制
            };

            // 冻结画笔
            brush.Freeze();
            return brush;
        }
    }
}