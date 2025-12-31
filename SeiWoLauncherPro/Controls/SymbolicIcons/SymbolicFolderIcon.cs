using System.Windows.Media;

namespace SeiWoLauncherPro.Controls.SymbolicIcons
{
    public class SymbolicFolderIcon : SymbolicIconBase
    {
        // F1 M24,24z M0,0z 这两个点已经定义了 24x24 的边界，无需额外的 ClipGeometry
        private const string PathData = "F1 M24,24z M0,0z M3,3C2.44772,3,2,3.44772,2,4L2,7 9.58579,7 12,4.58579 10.4142,3 3,3z M14.4142,5L10.4142,9 2,9 2,20C2,20.5523,2.44772,21,3,21L21,21C21.5523,21,22,20.5523,22,20L22,6C22,5.44772,21.5523,5,21,5L14.4142,5z";

        // 静态缓存 Geometry，内存中只有这一份副本
        private static readonly Geometry _cachedGeometry;

        static SymbolicFolderIcon()
        {
            _cachedGeometry = Geometry.Parse(PathData);
            if (_cachedGeometry.CanFreeze) _cachedGeometry.Freeze();
        }

        public SymbolicFolderIcon() => UpdateIcon();

        protected override DrawingImage CreateDrawingImage(Brush brush)
        {
            // 直接构建 GeometryDrawing，省去 DrawingGroup 的包装开销
            var drawing = new GeometryDrawing(brush, null, _cachedGeometry);
            if (drawing.CanFreeze) drawing.Freeze();

            var image = new DrawingImage(drawing);
            if (image.CanFreeze) image.Freeze();

            return image;
        }
    }
}