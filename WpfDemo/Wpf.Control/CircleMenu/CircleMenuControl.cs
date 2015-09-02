using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Wpf.Control.Flow;

namespace Wpf.Control.CircleMenu
{
    [TemplatePart(Name = PartCenterBtn)]
    [TemplatePart(Name = PartContainer)]
    [TemplatePart(Name = PartPanelPresenter)]
    [TemplateVisualState(GroupName = "CommonStates", Name = VisualStateInitial)]
    [TemplateVisualState(GroupName = "CommonStates", Name = VisualStateExpanded)]
    [TemplateVisualState(GroupName = "CommonStates", Name = VisualStateCollapsed)]
    public class CircleMenuControl : ItemsControl
    {
        private const string PartCenterBtn = "PART_CenterBtn";
        private const string PartContainer = "PART_Container";
        private const string PartPanelPresenter = "PART_PanelPresenter";
        public const string VisualStateInitial = "Initial";
        public const string VisualStateExpanded = "Expanded";
        public const string VisualStateCollapsed = "Collapsed";

        static CircleMenuControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CircleMenuControl), new FrameworkPropertyMetadata(typeof(CircleMenuControl)));
        }

        #region dependency property

        public static readonly DependencyProperty AngleProperty = DependencyProperty.Register(
            "Angle", typeof(double), typeof(CircleMenuControl), new PropertyMetadata(360d));

        public double Angle
        {
            get { return (double)GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        #endregion

        private Border _centerBtn;
        private Grid _container;
        private CircleMenuPanel _circleMenuPanel;
        private CircleMenuItemsPresenter _circleMenuItemsPresenter;

        public override void OnApplyTemplate()
        {
            if (_centerBtn != null)
            {
                _centerBtn.MouseLeftButtonUp -= centerBtn_Click;
            }

            base.OnApplyTemplate();

            _centerBtn = GetTemplateChild(PartCenterBtn) as Border;
            _container = GetTemplateChild(PartContainer) as Grid;
            _circleMenuItemsPresenter = GetTemplateChild(PartPanelPresenter) as CircleMenuItemsPresenter;

            if (_centerBtn != null)
            {
                _centerBtn.MouseLeftButtonUp += centerBtn_Click;
            }
        }

        private void centerBtn_Click(object sender, RoutedEventArgs e)
        {
            //第一个参数是<VisualStateManager>所在元素的父元素，本控件中为Grid的父级，即控件本身
            switch (_circleMenuItemsPresenter.Status)
            {
                case CircleMenuStatus.Expanded:
                    VisualStateManager.GoToState(this, VisualStateCollapsed, false);
                    break;
                case CircleMenuStatus.Initial:
                case CircleMenuStatus.Collapsed:
                    VisualStateManager.GoToState(this, VisualStateExpanded, false);
                    break;
            }

            //如果只是在控件内部更改Panel状态可以直接设置ItemPresenter的Status
            //使用VisualStateManager是为了可以在外部通过更改状态更新面板
        }

        #region route event

        //route event

        //inner menu click
        public static readonly RoutedEvent SubMenuClickEvent =
            ButtonBase.ClickEvent.AddOwner(typeof (CircleMenuControl));

        public event RoutedEventHandler SubMenuClick
        {
            add { AddHandler(ButtonBase.ClickEvent, value, false); }
            remove { RemoveHandler(ButtonBase.ClickEvent, value); }
        }

        #endregion

    }

    public enum CircleMenuStatus : byte
    {
        Initial,
        Expanded,
        Collapsed,
    }
}
