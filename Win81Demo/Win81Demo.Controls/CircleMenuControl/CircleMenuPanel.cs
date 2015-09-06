using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Win81Demo.Controls.CircleMenuControl
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
            "PanelStatus",
            typeof(CircleMenuStatus),
            typeof(CircleMenuPanel),
            new PropertyMetadata((int)CircleMenuStatus.Initial, ReRender));

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

            return s;
        }

        //http://www.cnblogs.com/mantgh/p/4161142.html
        protected override Size ArrangeOverride(Size finalSize)
        {

            var cutNum = (int)Angle == 360 ? this.Children.Count : (this.Children.Count - 1);
            var degreesOffset = Angle / cutNum;
            var i = 0;
            foreach (UIElement element in Children)
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

        private void ArrangeExpandElement(int idx, UIElement element,
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
            var storySpin = new Storyboard();
            var translateXAnimation = new DoubleAnimation() { From = 0, To = destX - panelCenterX, Duration = aniDuration };
            var translateYAnimation = new DoubleAnimation() { From = 0, To = destY - panelCenterY, Duration = aniDuration };
            var transparentAnimation = new DoubleAnimation() { From = 0, To = 1, Duration = aniDuration };
            var rotateXAnimation = new DoubleAnimation() { From = 0, To = destX - panelCenterX, Duration = aniDuration };
            var rotateYAnimation = new DoubleAnimation() { From = 0, To = destY - panelCenterY, Duration = aniDuration };
            var rotateAngleAnimation = new DoubleAnimation() { From = 0, To = 720, Duration = aniDuration };

            storySpin.Children.Add(translateXAnimation);
            storySpin.Children.Add(translateYAnimation);
            storySpin.Children.Add(transparentAnimation);
            storySpin.Children.Add(rotateXAnimation);
            storySpin.Children.Add(rotateYAnimation);
            storySpin.Children.Add(rotateAngleAnimation);
            Storyboard.SetTargetProperty(translateXAnimation, "(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.X)");
            Storyboard.SetTargetProperty(translateYAnimation, "(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.Y)");
            Storyboard.SetTargetProperty(transparentAnimation, "UIElement.Opacity");
            Storyboard.SetTargetProperty(rotateXAnimation, "(UIElement.RenderTransform).(TransformGroup.Children)[1].(RotateTransform.CenterX)");
            Storyboard.SetTargetProperty(rotateYAnimation, "(UIElement.RenderTransform).(TransformGroup.Children)[1].(RotateTransform.CenterY)");
            Storyboard.SetTargetProperty(rotateAngleAnimation, "(UIElement.RenderTransform).(TransformGroup.Children)[1].(RotateTransform.Angle)");
            Storyboard.SetTarget(translateXAnimation, element);
            Storyboard.SetTarget(translateYAnimation, element);
            Storyboard.SetTarget(transparentAnimation, element);
            Storyboard.SetTarget(rotateXAnimation, element);
            Storyboard.SetTarget(rotateYAnimation, element);
            Storyboard.SetTarget(rotateAngleAnimation, element);

            storySpin.Begin();
        }

        private void ArrangeInitialElement(UIElement element, double panelCenterX, double panelCenterY)
        {
            element.Arrange(new Rect(panelCenterX, panelCenterY, element.DesiredSize.Width, element.DesiredSize.Height));
        }

        private void ArrangeCollapseElement(int idx, UIElement element,
                        double panelCenterX, double panelCenterY,
                        double elementCenterX, double elementCenterY,
                        double destX, double destY)
        {
            element.Arrange(new Rect(destX, destY, element.DesiredSize.Width, element.DesiredSize.Height));

            var aniDuration = AnimationDuration + TimeSpan.FromSeconds(AnimationDurationStep * idx);
            var storySpin = new Storyboard();
            var translateXAnimation = new DoubleAnimation() { From = 0, To = panelCenterX - destX, Duration = aniDuration };
            var translateYAnimation = new DoubleAnimation() { From = 0, To = panelCenterY - destY, Duration = aniDuration };
            var transparentAnimation = new DoubleAnimation() { From = 1, To = 0, Duration = aniDuration };
            var rotateXAnimation = new DoubleAnimation() { From = 0, To = panelCenterX - destX, Duration = aniDuration };
            var rotateYAnimation = new DoubleAnimation() { From = 0, To = panelCenterY - destY, Duration = aniDuration };
            var rotateAngleAnimation = new DoubleAnimation() { From = 0, To = -720, Duration = aniDuration };


            storySpin.Children.Add(translateXAnimation);
            storySpin.Children.Add(translateYAnimation);
            storySpin.Children.Add(transparentAnimation);
            storySpin.Children.Add(rotateXAnimation);
            storySpin.Children.Add(rotateYAnimation);
            storySpin.Children.Add(rotateAngleAnimation);
            Storyboard.SetTargetProperty(translateXAnimation, "(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.X)");
            Storyboard.SetTargetProperty(translateYAnimation, "(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.Y)");
            Storyboard.SetTargetProperty(transparentAnimation, "UIElement.Opacity");
            Storyboard.SetTargetProperty(rotateXAnimation, "(UIElement.RenderTransform).(TransformGroup.Children)[1].(RotateTransform.CenterX)");
            Storyboard.SetTargetProperty(rotateYAnimation, "(UIElement.RenderTransform).(TransformGroup.Children)[1].(RotateTransform.CenterY)");
            Storyboard.SetTargetProperty(rotateAngleAnimation, "(UIElement.RenderTransform).(TransformGroup.Children)[1].(RotateTransform.Angle)");

            Storyboard.SetTarget(translateXAnimation, element);
            Storyboard.SetTarget(translateYAnimation, element);
            Storyboard.SetTarget(transparentAnimation, element);
            Storyboard.SetTarget(rotateXAnimation, element);
            Storyboard.SetTarget(rotateYAnimation, element);
            Storyboard.SetTarget(rotateAngleAnimation, element);

            storySpin.Begin();
        }
    }
}
