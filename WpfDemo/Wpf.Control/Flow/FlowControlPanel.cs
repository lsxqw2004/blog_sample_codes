using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Wpf.Control.Flow
{
    public class FlowControlPanel : Panel
    {
        public static readonly DependencyProperty AnimationDurationProperty = DependencyProperty.Register(
            "AnimationDuration", typeof (Duration), typeof (FlowControlPanel), new PropertyMetadata(default(Duration)));

        public Duration AnimationDuration
        {
            get { return (Duration) GetValue(AnimationDurationProperty); }
            set { SetValue(AnimationDurationProperty, value); }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var s = base.MeasureOverride(availableSize);
            foreach (UIElement element in this.Children)
            {
                element.Measure(availableSize);
            }
            return availableSize;
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            const double y = 0;
            double margin = 0;

            foreach (UIElement child in Children)
            {
                var newMargin = child.DesiredSize.Width / 2;
                if (newMargin > margin)
                {
                    margin = newMargin;
                }
            }

            //double lastX = 0; todo
            foreach (ContentPresenter element in Children)
            {
                var node = element.Content as FlowItem;
                var x = Convert.ToDouble(node.OffsetRate) * (finalSize.Width - margin * 2);
                element.Arrange(new Rect(0, y, element.DesiredSize.Width, element.DesiredSize.Height));

                //来自http://www.mgenware.com/blog/?p=326
                var transform = element.RenderTransform as TranslateTransform;
                if (transform == null)
                    element.RenderTransform = transform = new TranslateTransform();

                transform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, x, AnimationDuration));

            }
            return finalSize;
        }
    }
}
