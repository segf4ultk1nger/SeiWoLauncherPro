using System.Windows.Media;

namespace SeiWoLauncherPro.Controls.SymbolicIcons
{
    public class SymbolicLockIcon : SymbolicIconBase
    {
        // F1 M24,24z M0,0z 这两个点已经定义了 24x24 的边界，无需额外的 ClipGeometry
        private const string PathData = "F1 M24,24z M0,0z M18,8L20,8C20.5523,8,21,8.44772,21,9L21,21C21,21.5523,20.5523,22,20,22L4,22C3.44772,22,3,21.5523,3,21L3,9C3,8.44772,3.44772,8,4,8L6,8 6,7C6,3.68629 8.68629,1 12,1 15.3137,1 18,3.68629 18,7L18,8z M11,15.7324L11,18 13,18 13,15.7324C13.5978,15.3866 14,14.7403 14,14 14,12.8954 13.1046,12 12,12 10.8954,12 10,12.8954 10,14 10,14.7403 10.4022,15.3866 11,15.7324z M16,8L16,7C16,4.79086 14.2091,3 12,3 9.79086,3 8,4.79086 8,7L8,8 16,8z";

        // 静态缓存 Geometry，内存中只有这一份副本
        private static readonly Geometry _cachedGeometry;

        static SymbolicLockIcon()
        {
            _cachedGeometry = Geometry.Parse(PathData);
            if (_cachedGeometry.CanFreeze) _cachedGeometry.Freeze();
        }

        public SymbolicLockIcon() => UpdateIcon();

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