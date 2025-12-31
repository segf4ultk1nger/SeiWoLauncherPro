using System.Windows.Media;

namespace SeiWoLauncherPro.Controls.SymbolicIcons
{
    public class SymbolicSettingsCogIcon : SymbolicIconBase
    {
        // F1 M24,24z M0,0z 这两个点已经定义了 24x24 的边界，无需额外的 ClipGeometry
        private const string PathData = "F1 M24,24z M0,0z M5.33409,4.54491C6.3494,3.63637 7.55145,2.9322 8.87555,2.49707 9.60856,3.4128 10.7358,3.99928 12,3.99928 13.2642,3.99928 14.3914,3.4128 15.1245,2.49707 16.4486,2.9322 17.6506,3.63637 18.6659,4.54491 18.2405,5.637 18.2966,6.90531 18.9282,7.99928 19.5602,9.09388 20.6314,9.77679 21.7906,9.95392 21.9279,10.6142 22,11.2983 22,11.9993 22,12.7002 21.9279,13.3844 21.7906,14.0446 20.6314,14.2218 19.5602,14.9047 18.9282,15.9993 18.2966,17.0932 18.2405,18.3616 18.6659,19.4536 17.6506,20.3622 16.4486,21.0664 15.1245,21.5015 14.3914,20.5858 13.2642,19.9993 12,19.9993 10.7358,19.9993 9.60856,20.5858 8.87555,21.5015 7.55145,21.0664 6.3494,20.3622 5.33409,19.4536 5.75952,18.3616 5.7034,17.0932 5.0718,15.9993 4.43983,14.9047 3.36862,14.2218 2.20935,14.0446 2.07212,13.3844 2,12.7002 2,11.9993 2,11.2983 2.07212,10.6142 2.20935,9.95392 3.36862,9.77679 4.43983,9.09388 5.0718,7.99928 5.7034,6.90531 5.75952,5.637 5.33409,4.54491z M13.5,14.5974C14.9349,13.7689 15.4265,11.9342 14.5981,10.4993 13.7696,9.0644 11.9349,8.57277 10.5,9.4012 9.06512,10.2296 8.5735,12.0644 9.40192,13.4993 10.2304,14.9342 12.0651,15.4258 13.5,14.5974z";

        // 静态缓存 Geometry，内存中只有这一份副本
        private static readonly Geometry _cachedGeometry;

        static SymbolicSettingsCogIcon()
        {
            _cachedGeometry = Geometry.Parse(PathData);
            if (_cachedGeometry.CanFreeze) _cachedGeometry.Freeze();
        }

        public SymbolicSettingsCogIcon() => UpdateIcon();

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