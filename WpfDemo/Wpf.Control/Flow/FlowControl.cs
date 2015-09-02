using System.Windows;
using System.Windows.Controls;

namespace Wpf.Control.Flow
{
    public class FlowControl : ItemsControl
    {

        static FlowControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FlowControl), new FrameworkPropertyMetadata(typeof(FlowControl)));
        }

        #region dependency property

        private const double NodeWidthDefault = 30;

        public static readonly DependencyProperty NodeWidthProperty = DependencyProperty.Register(
            "NodeWidth", typeof(double), typeof(FlowControl),
            new PropertyMetadata(NodeWidthDefault));

        public double NodeWidth
        {
            get { return (double)GetValue(NodeWidthProperty); }
            set
            {
                SetValue(NodeWidthProperty, value);
            }
        }

        private const double BarHeightDefault = 10;

        public static readonly DependencyProperty BarHeightProperty = DependencyProperty.Register(
            "BarHeight", typeof(double), typeof(FlowControl), new PropertyMetadata(BarHeightDefault));

        public double BarHeight
        {
            get { return (double)GetValue(BarHeightProperty); }
            set { SetValue(BarHeightProperty, value); }
        }

        public static readonly DependencyProperty BarMarginLeftProperty = DependencyProperty.Register(
            "BarMarginLeft", typeof(double), typeof(FlowControl), new PropertyMetadata(0.0));

        public double BarMarginLeft
        {
            get { return (double)GetValue(BarMarginLeftProperty); }
            set { SetValue(BarMarginLeftProperty, value); }
        }

        public static readonly DependencyProperty BarMarginTopProperty = DependencyProperty.Register(
            "BarMarginTop", typeof(double), typeof(FlowControl), new PropertyMetadata(default(double)));

        private double BarMarginTop
        {
            get { return (double)GetValue(BarMarginTopProperty); }
            set { SetValue(BarMarginTopProperty, value); }
        }

        public static readonly DependencyProperty ShadowWidthProperty = DependencyProperty.Register(
            "ShadowWidth", typeof(double), typeof(FlowControl), new PropertyMetadata(default(double)));

        private double ShadowWidth
        {
            get { return (double)GetValue(ShadowWidthProperty); }
            set { SetValue(ShadowWidthProperty, value); }
        }

        public static readonly DependencyProperty AnimationDurationProperty = DependencyProperty.Register(
            "AnimationDuration", typeof(Duration), typeof(FlowControl), new PropertyMetadata(default(Duration)));

        public Duration AnimationDuration
        {
            get { return (Duration)GetValue(AnimationDurationProperty); }
            set { SetValue(AnimationDurationProperty, value); }
        }
        
        #endregion

        protected override Size MeasureOverride(Size constraint)
        {
            SetValue(BarMarginLeftProperty, NodeWidth / 2);
            SetValue(BarMarginTopProperty, (NodeWidth - BarHeight) / 2);
            SetValue(ShadowWidthProperty, constraint.Width - BarMarginLeft * 2);

            return base.MeasureOverride(new Size(constraint.Width, NodeWidth * 3));
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            return base.ArrangeOverride(arrangeBounds);
        }

        #region override itemscontrol

        //不使用路由事件时，可以这样传递事件
        //protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        //{
        //    var node = element as FlowNodeControl;
        //    if (node !=null && FlowNodeSelected != null)
        //        node.NodeSelected += FlowNodeSelected;

        //    base.PrepareContainerForItemOverride(element, item);
        //}

        //protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        //{
        //    var node = element as FlowNodeControl;
        //    if (node != null && FlowNodeSelected != null)
        //        node.NodeSelected -= FlowNodeSelected;

        //    base.ClearContainerForItemOverride(element, item);
        //}

        #endregion

        #region route event

        //route event
        public static readonly RoutedEvent NodeSelectedEvent =
            FlowNodeControl.NodeSelectedEvent.AddOwner(typeof(FlowControl));

        public event RoutedEventHandler NodeSelected
        {
            add { AddHandler(FlowNodeControl.NodeSelectedEvent, value, false); }
            remove { RemoveHandler(FlowNodeControl.NodeSelectedEvent, value); }
        }

        #endregion

    }
}
