using System.Windows;
using System.Windows.Controls;

namespace Wpf.Control.Flow
{
    [TemplatePart(Name = "PART_NodeRadioButton", Type = typeof(RadioButton))]
    public class FlowNodeControl : System.Windows.Controls.Control
    {
        static FlowNodeControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(FlowNodeControl), new FrameworkPropertyMetadata(typeof(FlowNodeControl)));
        }

        #region Dependency Property

        public static readonly DependencyProperty OffsetRateProperty = DependencyProperty.Register(
            "OffsetRate", typeof(double), typeof(FlowNodeControl), new PropertyMetadata(default(double)));

        public double OffsetRate
        {
            get { return (double)GetValue(OffsetRateProperty); }
            set { SetValue(OffsetRateProperty, value); }
        }

        public static readonly DependencyProperty NodeTitleProperty = DependencyProperty.Register(
            "NodeTitle", typeof(string), typeof(FlowNodeControl), new PropertyMetadata(string.Empty));

        public string NodeTitle
        {
            get { return (string)GetValue(NodeTitleProperty); }
            set { SetValue(NodeTitleProperty, value); }
        }

        //用于向上通知哪个Node被点击
        public static readonly DependencyProperty IdProperty = DependencyProperty.Register(
            "Id", typeof(int), typeof(FlowNodeControl), new PropertyMetadata(default(int)));

        public int Id
        {
            get { return (int)GetValue(IdProperty); }
            set { SetValue(IdProperty, value); }
        }

        private const double NodeWidthDefault = 30;
        public static readonly DependencyProperty NodeWidthProperty = DependencyProperty.Register(
            "NodeWidth", typeof(double), typeof(FlowNodeControl), new PropertyMetadata(NodeWidthDefault));

        public double NodeWidth
        {
            get { return (double)GetValue(NodeWidthProperty); }
            set { SetValue(NodeWidthProperty, value); }
        }

        #endregion

        private RadioButton nodeRadioButton;

        public override void OnApplyTemplate()
        {
            if (nodeRadioButton != null)
            {
                nodeRadioButton.Click -= nodeRadioButton_Click;
            }

            base.OnApplyTemplate();

            nodeRadioButton = GetTemplateChild("PART_NodeRadioButton") as RadioButton;

            if (nodeRadioButton != null)
            {
                nodeRadioButton.Click += nodeRadioButton_Click;
            }
        }

        void nodeRadioButton_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(NodeSelectedEvent,this));
        }

        //route event
        public static readonly RoutedEvent NodeSelectedEvent = EventManager.RegisterRoutedEvent(
                "NodeSelected", RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(FlowNodeControl));

        public event RoutedEventHandler NodeSelected
        {
            add { AddHandler(NodeSelectedEvent, value); }
            remove { RemoveHandler(NodeSelectedEvent, value); }
        }
    }
}
