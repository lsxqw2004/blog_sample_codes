using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Wpf.Control.CircleMenu
{
    public class CircleMenuPanel : Panel
    {
        public static readonly DependencyProperty AnimationDurationProperty = DependencyProperty.Register(
            "AnimationDuration", typeof(Duration), typeof(CircleMenuPanel), new PropertyMetadata(default(Duration)));

        public Duration AnimationDuration
        {
            get { return (Duration)GetValue(AnimationDurationProperty); }
            set { SetValue(AnimationDurationProperty, value); }
        }

        public static readonly DependencyProperty AnimationDurationStepProperty = DependencyProperty.Register(
            "AnimationDurationStep", typeof(double), typeof(CircleMenuPanel), new PropertyMetadata(0.3d));

        public double AnimationDurationStep
        {
            get { return (double)GetValue(AnimationDurationStepProperty); }
            set { SetValue(AnimationDurationStepProperty, value); }
        }


        public static readonly DependencyProperty RadiusProperty = DependencyProperty.Register(
            "Radius", typeof(Double), typeof(CircleMenuPanel), new PropertyMetadata(50d));

        public double Radius
        {
            get { return (Double)GetValue(RadiusProperty); }
            set { SetValue(RadiusProperty, value); }
        }

        public static readonly DependencyProperty AngleProperty = DependencyProperty.Register(
            "Angle", typeof(double), typeof(CircleMenuPanel), new PropertyMetadata(360d));

        public double Angle
        {
            get { return (double)GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        public static readonly DependencyProperty PanelStatusProperty = DependencyProperty.Register(
            "PanelStatus", typeof(CircleMenuStatus), typeof(CircleMenuPanel), new PropertyMetadata(CircleMenuStatus.Initial, ReRender));

        public CircleMenuStatus PanelStatus
        {
            get { return (CircleMenuStatus)GetValue(PanelStatusProperty); }
            set { SetValue(PanelStatusProperty, value); }
        }

        private static void ReRender(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var circelPanel = (CircleMenuPanel)d;
            circelPanel.InvalidateArrange();
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

        //http://www.cnblogs.com/mantgh/p/4161142.html
        protected override Size ArrangeOverride(Size finalSize)
        {
            var cutNum = (int)Angle == 360 ? this.Children.Count : (this.Children.Count - 1);
            var degreesOffset = Angle / cutNum;
            var i = 0;
            foreach (ContentPresenter element in Children)
            {
                var elementRadius = element.DesiredSize.Width / 2.0;
                var elementCenterX = elementRadius;
                var elementCenterY = elementRadius;

                var panelCenterX = Radius - elementRadius;
                var panelCenterY = Radius - elementRadius;

                var degreesAngle = degreesOffset * i;
                var radianAngle = (Math.PI * degreesAngle) / 180.0;
                var x = this.Radius * Math.Sin(radianAngle);
                var y = -this.Radius * Math.Cos(radianAngle);
                var destX = x + finalSize.Width / 2 - elementCenterX;
                var destY = y + finalSize.Height / 2 - elementCenterY;

                switch (PanelStatus)
                {
                    case CircleMenuStatus.Initial:
                        ArrangeInitialElement(element, panelCenterX, panelCenterY);
                        break;
                    case CircleMenuStatus.Collapsed:
                        ArrangeCollapseElement(i, element, panelCenterX, panelCenterY, elementCenterX, elementCenterY, destX, destY);
                        break;
                    case CircleMenuStatus.Expanded:
                        ArrangeExpandElement(i, element, panelCenterX, panelCenterY, elementCenterX, elementCenterY, destX, destY);
                        break;
                }

                ++i;
            }
            return finalSize;
        }

        private void ArrangeExpandElement(int idx, ContentPresenter element,
            double panelCenterX, double panelCenterY,
            double elementCenterX, double elementCenterY,
            double destX, double destY)
        {
            element.Arrange(new Rect(panelCenterX, panelCenterY, element.DesiredSize.Width, element.DesiredSize.Height));

            var transGroup = element.RenderTransform as TransformGroup;
            Transform translateTransform, rotateTransform;
            if (transGroup == null)
            {
                element.RenderTransform = transGroup = new TransformGroup();
                translateTransform = new TranslateTransform();
                rotateTransform = new RotateTransform() { CenterX = elementCenterX, CenterY = elementCenterY };

                transGroup.Children.Add(translateTransform);
                transGroup.Children.Add(rotateTransform);
            }
            else
            {
                translateTransform = transGroup.Children[0] as TranslateTransform;
                rotateTransform = transGroup.Children[1] as RotateTransform;
            }
            element.RenderTransformOrigin = new Point(0.5, 0.5);

            //if (i != 0) continue;

            var aniDuration = AnimationDuration + TimeSpan.FromSeconds(AnimationDurationStep * idx);
            translateTransform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, destX - panelCenterX, aniDuration));
            translateTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, destY - panelCenterY, aniDuration));

            rotateTransform.BeginAnimation(RotateTransform.CenterXProperty, new DoubleAnimation(0, destX - panelCenterX, aniDuration));
            rotateTransform.BeginAnimation(RotateTransform.CenterYProperty, new DoubleAnimation(0, destY - panelCenterY, aniDuration));
            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 720, aniDuration));

            element.BeginAnimation(OpacityProperty, new DoubleAnimation(0.2, 1, aniDuration));
        }

        private void ArrangeInitialElement(ContentPresenter element, double panelCenterX, double panelCenterY)
        {
            element.Arrange(new Rect(panelCenterX, panelCenterY, element.DesiredSize.Width, element.DesiredSize.Height));
        }

        private void ArrangeCollapseElement(int idx, ContentPresenter element,
                        double panelCenterX, double panelCenterY,
                        double elementCenterX, double elementCenterY,
                        double destX, double destY)
        {
            element.Arrange(new Rect(destX, destY, element.DesiredSize.Width, element.DesiredSize.Height));

            var transGroup = element.RenderTransform as TransformGroup;
            Transform translateTransform, rotateTransform;
            if (transGroup == null)
            {
                element.RenderTransform = transGroup = new TransformGroup();
                translateTransform = new TranslateTransform();
                rotateTransform = new RotateTransform() { CenterX = elementCenterX, CenterY = elementCenterY };

                transGroup.Children.Add(translateTransform);
                transGroup.Children.Add(rotateTransform);
            }
            else
            {
                translateTransform = transGroup.Children[0] as TranslateTransform;
                rotateTransform = transGroup.Children[1] as RotateTransform;
            }
            element.RenderTransformOrigin = new Point(0.5, 0.5);

            //if (i != 0) continue;

            var aniDuration = AnimationDuration + TimeSpan.FromSeconds(AnimationDurationStep * idx);
            translateTransform.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, panelCenterX - destX, aniDuration));
            translateTransform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, panelCenterY - destY, aniDuration));

            rotateTransform.BeginAnimation(RotateTransform.CenterXProperty, new DoubleAnimation(0, panelCenterX - destX, aniDuration));
            rotateTransform.BeginAnimation(RotateTransform.CenterYProperty, new DoubleAnimation(0, panelCenterY - destY, aniDuration));
            rotateTransform.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, -720, aniDuration));

            element.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0.2, aniDuration));
        }
    }

}
