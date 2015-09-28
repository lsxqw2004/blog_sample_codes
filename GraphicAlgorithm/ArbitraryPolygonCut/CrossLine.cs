using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfApplication1
{
    public class CrossLine : Shape
    {
        public static readonly DependencyProperty CenterProperty = DependencyProperty.Register(
            "Center", typeof(Point), typeof(CrossLine),
            new FrameworkPropertyMetadata(default(Point), FrameworkPropertyMetadataOptions.AffectsRender));

        public Point Center
        {
            get { return (Point)GetValue(CenterProperty); }
            set { SetValue(CenterProperty, value); }
        }

        public static readonly DependencyProperty VisibleProperty = DependencyProperty.Register(
            "Visible", typeof(bool), typeof(CrossLine),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public bool Visible
        {
            get { return (bool)GetValue(VisibleProperty); }
            set { SetValue(VisibleProperty, value); }
        }

        public static readonly DependencyProperty CrossRadiusProperty = DependencyProperty.Register(
            "CrossRadius", typeof(double), typeof(CrossLine), new PropertyMetadata(3d));

        public double CrossRadius
        {
            get { return (double)GetValue(CrossRadiusProperty); }
            set { SetValue(CrossRadiusProperty, value); }
        }

        private Geometry _polylineGeometry;

        protected override Geometry DefiningGeometry
        {
            get
            {
                CompositeGeometry();
                return _polylineGeometry;
            }
        }

        private void CompositeGeometry()
        {
            if (!Visible)
            {
                _polylineGeometry = Geometry.Empty;
                return;
            }

            PathGeometry polylineGeometry = new PathGeometry();


            var topPoint = new Point(Center.X, Center.Y - CrossRadius);
            var bottomPoint = new Point(Center.X, Center.Y + CrossRadius);
            PathFigure pathFigureV = new PathFigure();
            pathFigureV.StartPoint = topPoint;
            pathFigureV.Segments.Add(new LineSegment(bottomPoint, true));
            var leftPoint = new Point(Center.X - CrossRadius, Center.Y);
            var rightPoint = new Point(Center.X + CrossRadius, Center.Y);
            var pathFigureH = new PathFigure();
            pathFigureH.StartPoint = leftPoint;
            pathFigureH.Segments.Add(new LineSegment(rightPoint, true));
            polylineGeometry.Figures.Add(pathFigureV);
            polylineGeometry.Figures.Add(pathFigureH);

            // Set FillRule 
            polylineGeometry.FillRule = FillRule.EvenOdd;

            if (polylineGeometry.Bounds == Rect.Empty)
            {
                _polylineGeometry = Geometry.Empty;
            }
            else
            {
                _polylineGeometry = polylineGeometry;
            }
        }
    }
}
