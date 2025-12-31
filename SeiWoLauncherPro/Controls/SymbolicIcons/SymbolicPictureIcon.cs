using System.Windows.Media;

namespace SeiWoLauncherPro.Controls.SymbolicIcons
{
    public class SymbolicPictureIcon : SymbolicIconBase
    {
        // F1 M24,24z M0,0z 这两个点已经定义了 24x24 的边界，无需额外的 ClipGeometry
        private const string PathData = "F1 M24,24z M0,0z M5,11.1005L7,9.1005 12.5,14.6005 16,11.1005 19,14.1005 19,5 5,5 5,11.1005z M4,3L20,3C20.5523,3,21,3.44772,21,4L21,20C21,20.5523,20.5523,21,20,21L4,21C3.44772,21,3,20.5523,3,20L3,4C3,3.44772,3.44772,3,4,3z M15.5,10C14.6716,10 14,9.32843 14,8.5 14,7.67157 14.6716,7 15.5,7 16.3284,7 17,7.67157 17,8.5 17,9.32843 16.3284,10 15.5,10z";

        // 静态缓存 Geometry，内存中只有这一份副本
        private static readonly Geometry _cachedGeometry;

        static SymbolicPictureIcon()
        {
            _cachedGeometry = Geometry.Parse(PathData);
            if (_cachedGeometry.CanFreeze) _cachedGeometry.Freeze();
        }

        public SymbolicPictureIcon() => UpdateIcon();

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