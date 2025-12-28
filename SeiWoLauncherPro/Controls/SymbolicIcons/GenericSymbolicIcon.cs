using System.Windows.Media;

namespace SeiWoLauncherPro.Controls.SymbolicIcons {
    public class GenericSymbolicIcon : SymbolicIconBase
    {
        protected override DrawingImage CreateDrawingImage(Brush brush) {
            return new DrawingImage();
        }
    }
}