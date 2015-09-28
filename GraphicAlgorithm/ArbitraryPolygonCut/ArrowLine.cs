using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfApplication1
{

    public class ArrowLine : Shape
    {
        private ObservableCollection<Point> _points = new ObservableCollection<Point>();

        /// <summary> 
        /// Points
        /// </summary> 
        public ObservableCollection<Point> Points
        {
            get { return _points; }
            set
            {
                _points = value;
                CompositeGeometry();
            }
        }

        public ArrowLine()
        {
            Points.CollectionChanged += (sender, args) =>
            {
                CompositeGeometry();
            };
        }

        #region Dependency Properties 

        /// <summary> 
        /// ShowArrow 
        /// </summary> 
        public static readonly DependencyProperty ShowArrowProperty = DependencyProperty.Register(
            "ShowArrow",
            typeof(bool),
            typeof(ArrowLine),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender)
            );

        /// <summary>
        /// ShowArrow 
        /// </summary>
        public bool ShowArrow
        {
            get { return (bool)GetValue(ShowArrowProperty); }
            set { SetValue(ShowArrowProperty, value); }
        }

        public static readonly DependencyProperty ArrowEdgeAngleProperty = DependencyProperty.Register(
              "ArrowEdgeAngle", typeof(double), typeof(ArrowLine), new PropertyMetadata(60d));

        public double ArrowEdgeAngle
        {
            get { return (double)GetValue(ArrowEdgeAngleProperty); }
            set { SetValue(ArrowEdgeAngleProperty, value); }
        }

        public static readonly DependencyProperty ArrowEdgeLengthProperty = DependencyProperty.Register(
            "ArrowEdgeLength", typeof(double), typeof(ArrowLine), new PropertyMetadata(10d));

        public double ArrowEdgeLength
        {
            get { return (double)GetValue(ArrowEdgeLengthProperty); }
            set { SetValue(ArrowEdgeLengthProperty, value); }
        }

        public static readonly DependencyProperty IsDirectionReverseProperty = DependencyProperty.Register(
            "IsDirectionReverse", typeof(bool), typeof(ArrowLine), new PropertyMetadata(false));

        public bool IsDirectionReverse
        {
            get { return (bool)GetValue(IsDirectionReverseProperty); }
            set { SetValue(IsDirectionReverseProperty, value); }
        }

        private List<string> _text = new List<string>();

        /// <summary>
        /// 文本
        /// </summary>
        public List<string> Text
        {
            get { return _text; }
            set { _text = value; }
        }


        /// <summary>
        /// 文本朝上的依赖属性
        /// </summary>
        public static readonly DependencyProperty IsTextUpProperty = DependencyProperty.Register(
            "IsTextUp", typeof(bool), typeof(ArrowLine),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// 文本是否朝上
        /// </summary>
        public bool IsTextUp
        {
            get { return (bool)GetValue(IsTextUpProperty); }
            set { SetValue(IsTextUpProperty, value); }
        }

        /// <summary>
        /// 是否显示文本的依赖属性
        /// </summary>
        public static readonly DependencyProperty ShowTextProperty = DependencyProperty.Register(
            "ShowText", typeof(bool), typeof(ArrowLine), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// 是否显示文本
        /// </summary>
        public bool ShowText
        {
            get { return (bool)GetValue(ShowTextProperty); }
            set { SetValue(ShowTextProperty, value); }
        }

        #endregion

        private Geometry _polylineGeometry;

        protected override Geometry DefiningGeometry
        {
            get { return _polylineGeometry; }
        }

        private void CompositeGeometry()
        {
            var pointCollection = Points;
            PathFigure pathFigure = new PathFigure();

            if (pointCollection == null || pointCollection.Count == 0)
            {
                _polylineGeometry = Geometry.Empty;
                return;
            }

            if (pointCollection.Count > 0)
            {
                pathFigure.StartPoint = pointCollection[0];

                if (pointCollection.Count > 1)
                {
                    Point[] array = new Point[pointCollection.Count - 1];

                    for (int i = 1; i < pointCollection.Count; i++)
                    {
                        array[i - 1] = pointCollection[i];
                    }

                    pathFigure.Segments.Add(new PolyLineSegment(array, true));
                }
            }

            PathGeometry polylineGeometry = new PathGeometry();

            if (ShowArrow)
            {
                Action<Point, Point> addArrowAction = (p1, p2) =>
                {

                    MyVector topVec, bottomVec;
                    CalcArrowEdgeVec(p1, p2, out topVec, out bottomVec);
                    var pathFigureA = new PathFigure();
                    pathFigureA.StartPoint = p2.AddVec(topVec);
                    var arrowArray = new Point[] { p2, p2.AddVec(bottomVec) };
                    pathFigureA.Segments.Add(new PolyLineSegment(arrowArray, true));
                    polylineGeometry.Figures.Add(pathFigureA);

                };

                if (!IsDirectionReverse)
                    for (int i = 0; i < pointCollection.Count - 1; i++)
                    {
                        var p1 = pointCollection[i];
                        var p2 = pointCollection[i + 1];
                        addArrowAction(p1, p2);
                    }
                else
                    for (int i = pointCollection.Count - 1; i > 0; i--)
                    {
                        var p1 = pointCollection[i];
                        var p2 = pointCollection[i - 1];
                        addArrowAction(p1, p2);
                    }
            }



            polylineGeometry.Figures.Add(pathFigure);

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

            InvalidateVisual();
        }


        private void CalcArrowEdgeVec(Point p1, Point p2, out MyVector topEdgeVec, out MyVector bottomEdgeVec)
        {
            //sin(-θ)=-sinθ
            //cos(-θ)=cosθ
            var lineVec = new MyVector(p1.X - p2.X, p1.Y - p2.Y);
            lineVec.Normalize();
            lineVec.MultplyLen((float)ArrowEdgeLength);
            var rad = ArrowEdgeAngle / 2 * (Math.PI / 180);
            var sinθ = Math.Sin(rad);
            var cosθ = Math.Cos(rad);
            var topRotateMatrix = new MyMaxtrix(cosθ, sinθ, -sinθ, cosθ, 0, 0);
            var bottomRotateMatrix = new MyMaxtrix(cosθ, -sinθ, sinθ, cosθ, 0, 0);
            topEdgeVec = lineVec.MutliplyMatrix(topRotateMatrix);
            bottomEdgeVec = lineVec.MutliplyMatrix(bottomRotateMatrix);
        }

        /// <summary>
        /// 重载渲染事件 显示文本
        /// </summary>
        /// <param name="drawingContext">绘图上下文</param>
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (ShowText && Text.Count > 0)
            {
                for (int i = 0; i < Points.Count-1; i++)
                {
                    var startPoint = Points[i];
                    var endPoint = Points[i + 1];
                    var label = Text[i];

                    var startPoint1 = startPoint;
                    if (!string.IsNullOrEmpty(label))
                    {
                        var vec = endPoint - startPoint;
                        var angle = GetAngle(startPoint, endPoint);

                        //使用旋转变换,使其与线平等
                        var transform = new RotateTransform(angle) { CenterX = startPoint.X, CenterY = startPoint.Y };
                        drawingContext.PushTransform(transform);

                        var defaultTypeface = new Typeface(SystemFonts.StatusFontFamily, SystemFonts.StatusFontStyle,
                            SystemFonts.StatusFontWeight, new FontStretch());
                        var formattedText = new FormattedText(label, CultureInfo.CurrentCulture,
                            FlowDirection.LeftToRight,
                            defaultTypeface, SystemFonts.StatusFontSize, Brushes.Black)
                        {
                            //文本最大宽度为线的宽度
                            MaxTextWidth = vec.Length,
                            //设置文本对齐方式
                            TextAlignment = TextAlignment.Left
                        };

                        var offsetY = StrokeThickness;
                        if (IsTextUp)
                        {
                            //计算文本的行数
                            double textLineCount = formattedText.Width / formattedText.MaxTextWidth;
                            if (textLineCount < 1)
                            {
                                //怎么也得有一行
                                textLineCount = 1;
                            }
                            //计算朝上的偏移
                            offsetY = -formattedText.Height * textLineCount - StrokeThickness;
                        }
                        startPoint1 = startPoint1 + new Vector(0, offsetY);
                        drawingContext.DrawText(formattedText, startPoint1);
                        drawingContext.Pop();
                    }
                }
            }
        }

        private double GetAngle(Point start, Point end)
        {
            var vec = end - start;
            //X轴
            var xAxis = new Vector(1, 0);
            return Vector.AngleBetween(xAxis, vec);
        }
    }

    #region Math

    public class MyMaxtrix
    {
        public MyMaxtrix(double m11, double m12, double m21, double m22, double m31, double m32)
        {
            M11 = (float)m11;
            M12 = (float)m12;
            M21 = (float)m21;
            M22 = (float)m22;
            M31 = (float)m31;
            M32 = (float)m32;
        }

        public float M11;
        public float M12;
        public float M21;
        public float M22;
        public float M31;
        public float M32;
    }

    public class MyVector
    {
        public MyVector(double x, double y)
        {
            X = (float)x;
            Y = (float)y;
        }

        public float X;
        public float Y;

        public void Normalize()
        {
            var len = (float)Math.Sqrt(X * X + Y * Y);
            if (len > 0)
            {
                X /= len;
                Y /= len;
            }
            else
            {
                X = Y = 0;
            }
        }

        public void MultplyLen(float len)
        {
            X *= len;
            Y *= len;
        }

        public static MyVector GetVectorFromPoint(Point p1, Point p2)
        {
            return new MyVector(p1.X - p2.X, p1.Y - p2.Y);
        }

        public MyVector MutliplyMatrix(MyMaxtrix m)
        {
            var newX = this.X * m.M11 + this.Y * m.M21 + 0 * m.M31;
            var newY = this.X * m.M12 + this.Y * m.M22 + 0 * m.M32;
            return new MyVector(newX, newY);
        }
    }

    public static class PointExt
    {
        public static Point AddVec(this Point point, MyVector vec)
        {
            return new Point(point.X + vec.X, point.Y + vec.Y);
        }

        public static Point SubVec(this Point point, MyVector vec)
        {
            return new Point(point.X - vec.X, point.Y - vec.Y);
        }
    }

    #endregion
}
