using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Wpf.Control.CircleMenu
{
    public class CircleMenuItemsPresenter:ItemsPresenter
    {
        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
            "Status", typeof (CircleMenuStatus), typeof (CircleMenuItemsPresenter), new PropertyMetadata(default(CircleMenuStatus)));

        public CircleMenuStatus Status
        {
            get { return (CircleMenuStatus) GetValue(StatusProperty); }
            set { SetValue(StatusProperty, value); }
        }

        public static readonly DependencyProperty AngleProperty = DependencyProperty.Register(
            "Angle", typeof(Double), typeof(CircleMenuItemsPresenter), new PropertyMetadata(360d));

        public double Angle
        {
            get { return (Double)GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }
    }
}
