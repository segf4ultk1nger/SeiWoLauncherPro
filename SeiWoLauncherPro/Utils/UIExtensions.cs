using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace SeiWoLauncherPro
{
    public static class UIExtensions
    {
        #region 1. 布局容器类 (Layout Extensions)

        /// <summary>
        /// 定义 Grid 行，支持 "1*, Auto, 100" 格式
        /// </summary>
        public static Grid Rows(this Grid grid, string definitions)
        {
            grid.RowDefinitions.Clear();
            foreach (var def in definitions.Split(','))
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = ParseLength(def) });
            }
            return grid;
        }

        /// <summary>
        /// 定义 Grid 列，支持 "1*, Auto, 100" 格式
        /// </summary>
        public static Grid Cols(this Grid grid, string definitions)
        {
            grid.ColumnDefinitions.Clear();
            foreach (var def in definitions.Split(','))
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = ParseLength(def) });
            }
            return grid;
        }

        private static GridLength ParseLength(string def)
        {
            var s = def.Trim();
            if (s.Equals("Auto", StringComparison.OrdinalIgnoreCase)) return GridLength.Auto;
            if (s.EndsWith("*"))
            {
                var val = s.Replace("*", "");
                return new GridLength(string.IsNullOrEmpty(val) ? 1 : double.Parse(val), GridUnitType.Star);
            }
            return new GridLength(double.Parse(s));
        }

        /// <summary>
        /// 一次性填充子元素
        /// </summary>
        public static T Children<T>(this T panel, params FrameworkElement[] elements) where T : Panel
        {
            foreach (var el in elements) panel.Children.Add(el);
            return panel;
        }

        /// <summary>
        /// 设置 Grid 位置
        /// </summary>
        public static T Cell<T>(this T element, int row = 0, int col = 0, int rowSpan = 1, int colSpan = 1) where T : FrameworkElement
        {
            Grid.SetRow(element, row);
            Grid.SetColumn(element, col);
            Grid.SetRowSpan(element, rowSpan);
            Grid.SetColumnSpan(element, colSpan);
            return element;
        }

        public static T Margin<T>(this T element, double uniform) where T : FrameworkElement
        {
            element.Margin = new Thickness(uniform);
            return element;
        }

        public static T Margin<T>(this T element, double horizontal, double vertical) where T : FrameworkElement
        {
            element.Margin = new Thickness(horizontal, vertical, horizontal, vertical);
            return element;
        }

        public static T Padding<T>(this T element, double uniform) where T : Control
        {
            element.Padding = new Thickness(uniform);
            return element;
        }

        #endregion

        #region 2. 属性注入与生命周期类 (Lifecycle & Property)

        /// <summary>
        /// 截获当前实例并赋值给外部变量
        /// </summary>
        public static T Assign<T>(this T element, out T variable)
        {
            variable = element;
            return element;
        }

        /// <summary>
        /// 逃生口：直接对实例进行任意操作
        /// </summary>
        public static T With<T>(this T element, Action<T>? action)
        {
            action?.Invoke(element);
            return element;
        }

        public static T Name<T>(this T element, string name) where T : FrameworkElement
        {
            element.Name = name;
            return element;
        }

        /// <summary>
        /// 简化版绑定：Bind(TextBlock.TextProperty, "Title")
        /// </summary>
        public static T Bind<T>(this T element, DependencyProperty dp, string path, BindingMode mode = BindingMode.Default, IValueConverter? converter = null) where T : DependencyObject
        {
            var binding = new Binding(path) { Mode = mode, Converter = converter };
            BindingOperations.SetBinding(element, dp, binding);
            return element;
        }

        #endregion

        #region 3. 样式与装饰类 (Styling & Decoration)

        public static T Font<T>(this T element, double size, FontWeight weight = default) where T : Control
        {
            element.FontSize = size;
            if (weight != default) element.FontWeight = weight;
            return element;
        }

        // 针对 TextBlock 的 Font 特化（因为它不继承 Control）
        public static TextBlock Font(this TextBlock element, double size, FontWeight weight = default)
        {
            element.FontSize = size;
            if (weight != default) element.FontWeight = weight;
            return element;
        }

        public static T Color<T>(this T element, Brush foreground, Brush? background = null) where T : Control
        {
            element.Foreground = foreground;
            if (background != null) element.Background = background;
            return element;
        }

        public static T Opacity<T>(this T element, double value) where T : UIElement
        {
            element.Opacity = value;
            return element;
        }

        public static T VisibleIf<T>(this T element, bool condition, Visibility elseVisibility = Visibility.Collapsed) where T : UIElement
        {
            element.Visibility = condition ? Visibility.Visible : elseVisibility;
            return element;
        }

        #endregion

        #region 通用事件处理 (Universal Event Handling)

        /// <summary>
        /// 通用路由事件挂载
        /// </summary>
        public static T On<T>(this T element, RoutedEvent routedEvent, RoutedEventHandler handler) where T : UIElement
        {
            element.AddHandler(routedEvent, handler);
            return element;
        }

        #endregion
    }
}