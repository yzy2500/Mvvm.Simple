using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mvvm.Simple
{
    /// <summary>
    /// ToastTipBox.xaml 的交互逻辑
    /// </summary>
    public partial class ToastTipBox : UserControl
    {
        public ToastTipBox()
        {
            InitializeComponent();
        }

        
    }

    public class ToastTipAdorner : Adorner
    {
        public ToastTipAdorner(UIElement adornedElement) : base(adornedElement)
        {
        }

        readonly ToastTipBox box = new ToastTipBox() { IsEnabled = false, IsHitTestVisible = false };
        //protected override Visual GetVisualChild(int index)
        //{
        //    return box;
        //}
        //protected override int VisualChildrenCount => 1;
        protected override Size ArrangeOverride(Size finalSize)
        {
            box.Arrange(new Rect(finalSize));
            return finalSize;
        }

        public async void AddMessage(string message, TipLevel level = TipLevel.None)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            if (box.Content is Panel p)
            {
                var label = new Label() { Content = message, Uid = level.ToString() };
                p.Children.Add(label);
                //await Task.Delay(1);
                InvalidateVisual();
                await Task.Delay(3000);
                p.Children.Remove(label);
                InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext draw)
        {
            if (box.Content is FrameworkElement f && f.ActualHeight > 14) 
            {
                VisualBrush brush = new VisualBrush(f);
                draw.DrawRectangle(Brushes.Transparent, null, new Rect(box.RenderSize));
                draw.DrawRectangle(brush, null, new Rect((box.ActualWidth - f.ActualWidth) / 2, (box.ActualHeight - f.ActualHeight + 14) / 2, f.ActualWidth, f.ActualHeight - 14));
            }
            
        }
    }
}
