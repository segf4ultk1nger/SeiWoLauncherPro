using System.Windows;
using System.Windows.Media;

namespace SeiWoLauncherPro.Controls.SymbolicIcons // 记得替换为你的实际命名空间
{
    /// <summary>
    /// 专用于显示 Desktop 电脑图标的组件。
    /// 它不需要设置 Icon 属性，只需要设置 IconColor。
    /// </summary>
    public class SymbolicDesktopIcon : SymbolicIconBase
    {
        // 将 Path 字符串常量化
        private const string PathDataStr = "F1 M24,24z M0,0z M13,18L13,20 17,20 17,22 7,22 7,20 11,20 11,18 2.9918,18C2.44405,18,2,17.5511,2,16.9925L2,4.00748C2,3.45107,2.45531,3,2.9918,3L21.0082,3C21.556,3,22,3.44892,22,4.00748L22,16.9925C22,17.5489,21.5447,18,21.0082,18L13,18z";
        private const string ClipDataStr = "M0,0 V24 H24 V0 H0 Z";

        // 静态缓存解析后的 Geometry，避免每次重绘都解析字符串，这是性能关键点。
        private static readonly Geometry CachedMainGeometry;
        private static readonly Geometry CachedClipGeometry;

        // 静态构造函数确保只解析一次
        static SymbolicDesktopIcon()
        {
            // 解析主路径几何并冻结，使其跨线程安全且高性能
            CachedMainGeometry = Geometry.Parse(PathDataStr);
            if (CachedMainGeometry.CanFreeze) CachedMainGeometry.Freeze();

            // 解析裁剪几何并冻结
            CachedClipGeometry = Geometry.Parse(ClipDataStr);
            if (CachedClipGeometry.CanFreeze) CachedClipGeometry.Freeze();
        }

        public SymbolicDesktopIcon()
        {
            // 因为这个控件已经确定了图标内容，我们在这里触发一次初始化更新
            // 这样即使在 XAML 中没有设置 IconColor，它也会用默认黑色显示出来
            UpdateIcon();
        }

        /// <summary>
        /// 核心实现：利用缓存的几何体和传入的动态 Brush 构建 DrawingImage
        /// </summary>
        protected override DrawingImage CreateDrawingImage(Brush brush)
        {
            // 注意：对于这个特定的类，iconData 参数被忽略了，
            // 因为图标本身的形状是这个类内在决定的。

            // 1. 创建 GeometryDrawing，这是唯一动态变化的部分（因为 Brush 变了）
            var drawing = new GeometryDrawing
            {
                // 这里的 brush 是从基类的 IconColorProperty 传进来的
                Brush = brush,
                // 使用静态缓存的几何体
                Geometry = CachedMainGeometry
            };
            // 冻结 Drawing 以提升性能
            if (drawing.CanFreeze) drawing.Freeze();

            // 2. 创建 DrawingGroup 来应用 Clip
            // 你的原始 XAML 中有一个 ClipGeometry，我们需要在这里还原它
            var drawingGroup = new DrawingGroup
            {
                ClipGeometry = CachedClipGeometry
            };
            drawingGroup.Children.Add(drawing);
            // 冻结 Group
            if (drawingGroup.CanFreeze) drawingGroup.Freeze();

            // 3. 最终封装成 DrawingImage
            var finalImage = new DrawingImage(drawingGroup);

            // 极其重要：冻结最终结果。这使得它成为一个不可变的资源，WPF 渲染引擎最喜欢这个。
            if (finalImage.CanFreeze) finalImage.Freeze();

            return finalImage;
        }
    }
}