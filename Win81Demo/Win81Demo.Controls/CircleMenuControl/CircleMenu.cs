using System;
using System.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Win81Demo.Controls.CircleMenuControl
{
    [TemplatePart(Name = PartCenterBtn)]
    [TemplatePart(Name = PartContainer)]
    [TemplatePart(Name = PartPanel)]
    [TemplateVisualState(GroupName = "CommonStates", Name = VisualStateInitial)]
    [TemplateVisualState(GroupName = "CommonStates", Name = VisualStateExpanded)]
    [TemplateVisualState(GroupName = "CommonStates", Name = VisualStateCollapsed)]
    [StyleTypedProperty(Property = "SubMenuStyle", StyleTargetType = typeof(Button))]
    public class CircleMenu : Control
    {
        private const string PartCenterBtn = "PART_CenterBtn";
        private const string PartContainer = "PART_Container";
        private const string PartPanel = "PART_Panel";
        private const string VisualStateInitial = "Initial";
        private const string VisualStateExpanded = "Expanded";
        private const string VisualStateCollapsed = "Collapsed";

        public CircleMenu()
        {
            DefaultStyleKey = typeof(CircleMenu);
        }

        #region data

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource",
            typeof(IEnumerable),
            typeof(CircleMenu), new PropertyMetadata(null));

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty CenterMenuProperty = DependencyProperty.Register(
            "CenterMenu", typeof(string), typeof(CircleMenu), new PropertyMetadata(default(string)));

        public string CenterMenu
        {
            get { return (string)GetValue(CenterMenuProperty); }
            set { SetValue(CenterMenuProperty, value); }
        }

        #endregion

        #region style & size

        public static readonly DependencyProperty SubMenuStyleProperty = DependencyProperty.Register(
            "SubMenuStyle", typeof(Style), typeof(CircleMenu), null);

        public Style SubMenuStyle
        {
            get { return (Style)GetValue(SubMenuStyleProperty); }
            set { SetValue(SubMenuStyleProperty, value); }
        }

        public static readonly DependencyProperty CenterSizeProperty = DependencyProperty.Register(
            "CenterSize", typeof (double), typeof (CircleMenu), new PropertyMetadata(default(double)));

        public double CenterSize
        {
            get { return (double) GetValue(CenterSizeProperty); }
            set { SetValue(CenterSizeProperty, value); }
        }

        public static readonly DependencyProperty CenterRadiusProperty = DependencyProperty.Register(
            "CenterRadius", typeof(double), typeof(CircleMenu), new PropertyMetadata(default(double)));

        public double CenterRadius
        {
            get { return (double)GetValue(CenterRadiusProperty); }
            set { SetValue(CenterRadiusProperty, value); }
        }

        public static readonly DependencyProperty ShadowSizeProperty = DependencyProperty.Register(
            "ShadowSize", typeof(double), typeof(CircleMenu), new PropertyMetadata(100d));

        public double ShadowSize
        {
            get { return (double)GetValue(ShadowSizeProperty); }
            set { SetValue(ShadowSizeProperty, value); }
        }

        public static readonly DependencyProperty ShadowRadiusProperty = DependencyProperty.Register(
            "ShadowRadius", typeof(double), typeof(CircleMenu), new PropertyMetadata(100d));

        public double ShadowRadius
        {
            get { return (double)GetValue(ShadowRadiusProperty); }
            set { SetValue(ShadowRadiusProperty, value); }
        }

        #endregion

        #region dependencyproperty (event handler)

        public static readonly DependencyProperty SubClickCommandProperty = DependencyProperty.Register(
            "SubClickCommand", typeof(Action<int>), typeof(CircleMenu), new PropertyMetadata(null));

        public Action<int> SubClickCommand
        {
            get { return (Action<int>)GetValue(SubClickCommandProperty); }
            set { SetValue(SubClickCommandProperty, value); }
        }

        public static readonly DependencyProperty CircleDurationProperty = DependencyProperty.Register(
            "CircleDuration", typeof(Duration), typeof(CircleMenu), new PropertyMetadata(new Duration(TimeSpan.FromSeconds(0.8))));

        public Duration CircleDuration
        {
            get { return (Duration)GetValue(CircleDurationProperty); }
            set { SetValue(CircleDurationProperty, value); }
        }

        public static readonly DependencyProperty CircleDurationStepProperty = DependencyProperty.Register(
            "CircleDurationStep", typeof (double), typeof (CircleMenu), new PropertyMetadata(0.2d));

        public double CircleDurationStep
        {
            get { return (double) GetValue(CircleDurationStepProperty); }
            set { SetValue(CircleDurationStepProperty, value); }
        }

        #endregion

        #region Control & Template

        private Border CenterBtn { get; set; }
        private Grid Container { get; set; }
        private CircleMenuPanel CircleMenuPanel { get; set; }
        //private ItemsPresenter _circleMenuItemsPresenter;

        protected override void OnApplyTemplate()
        {
            if (CenterBtn != null)
            {
                CenterBtn.Tapped -= centerBtn_Click;
            }

            CenterBtn = GetTemplateChild(PartCenterBtn) as Border;
            Container = GetTemplateChild(PartContainer) as Grid;
            CircleMenuPanel = GetTemplateChild(PartPanel) as CircleMenuPanel;

            SetSubMenu();

            if (CenterBtn != null)
            {
                CenterBtn.Tapped += centerBtn_Click;
            }

            base.OnApplyTemplate();
        }

        private void SetSubMenu()
        {
            CircleMenuPanel.Children.Clear();

            foreach (var item in ItemsSource)
            {
                var menuItem = item as CircleMenuItem;
                if (menuItem != null)
                {
                    var btn = new Button();
                    btn.Opacity = 0;
                    var bindTag = new Binding
                    {
                        Path = new PropertyPath("Id"),
                        Source = menuItem,
                        Mode = BindingMode.OneWay
                    };
                    btn.SetBinding(TagProperty, bindTag);//用Tag存储Id

                    var textBlock = new TextBlock();
                    var bindTitle = new Binding
                    {
                        Path = new PropertyPath("Title"),
                        Source = menuItem,
                        Mode = BindingMode.OneWay
                    };
                    textBlock.SetBinding(TextBlock.TextProperty,bindTitle);

                    btn.Content = textBlock;
                    var binding = new Binding()
                    {
                        Path = new PropertyPath("SubMenuStyle"),
                        RelativeSource = new RelativeSource() { Mode = RelativeSourceMode.TemplatedParent },
                        Source = this
                    };
                    btn.SetBinding(StyleProperty, binding);
                    btn.Click += (s, e) =>
                    {
                        VisualStateManager.GoToState(this, VisualStateCollapsed, false);
                        if (SubClickCommand != null)
                        {
                            var sbtn = s as Button;
                            if (sbtn != null)
                                SubClickCommand(Convert.ToInt32(sbtn.Tag));
                        }
                        SetSubMenu();
                        VisualStateManager.GoToState(this, VisualStateExpanded, false);
                    };

                    CircleMenuPanel.Children.Add(btn);
                }
            }
        }

        private void centerBtn_Click(object sender, RoutedEventArgs e)
        {
            //第一个参数是<VisualStateManager>所在元素的父元素，本控件中为Grid的父级，即控件本身
            switch (CircleMenuPanel.PanelStatus)
            {
                case CircleMenuStatus.Expanded:
                    VisualStateManager.GoToState(this, VisualStateCollapsed, false);
                    break;
                case CircleMenuStatus.Initial:
                case CircleMenuStatus.Collapsed:
                    VisualStateManager.GoToState(this, VisualStateExpanded, false);
                    //CircleMenuPanel.PanelStatus = CircleMenuStatus.Expanded;
                    break;
            }

            //如果只是在控件内部更改Panel状态可以直接设置ItemPresenter的Status
            //使用VisualStateManager是为了可以在外部通过更改状态更新面板
        }

        #endregion
    }

    public enum CircleMenuStatus : int//todo:不知道为啥不能使用byte，XAML绑定会报错
    {
        Initial = 0,
        Expanded = 1,
        Collapsed = 2,
    }
}
