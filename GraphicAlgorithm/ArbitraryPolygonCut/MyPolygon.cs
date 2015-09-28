using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfApplication1
{
    [Serializable]
    public class MyPolygon
    {
        public MyPolygon(RenderType type = RenderType.Polyline)
        {
            _type = type;
        }

        public Color StrokeColor { get; set; }
        public Color FillColor { get; set; }
        public double StrokeThickess { get; set; }

        private readonly RenderType _type;
        public bool Filled { get; set; } = false;
        public bool ShowArrow { get; set; }
        public bool ArrowReverse { get; set; }

        public List<VertexBase> PointList = new List<VertexBase>();
        private Shape _innerShape;

        private bool showed = false;

        public void Render(Canvas canvas)
        {
            if (_type == RenderType.Polyline)
            {
                if (_innerShape == null)
                {
                    _innerShape = new ArrowLine()
                    {
                        Stroke = new SolidColorBrush(StrokeColor),
                        StrokeThickness = StrokeThickess,
                        Fill = Filled ? new SolidColorBrush(FillColor) : new SolidColorBrush(Colors.Transparent),
                };
                }
                var polyline = _innerShape as ArrowLine;
                polyline.IsDirectionReverse = ArrowReverse;
                polyline.ShowArrow = ShowArrow;
                polyline.Text = PointList.Select(v => v.Name).ToList();
                polyline.Points.Clear();
                PointList.ForEach(v=>polyline.Points.Add(v.ToPoint()));
                polyline.Points.Add(PointList[0].ToPoint());
            }
            else if (_type == RenderType.Polygon)
            {
                if (_innerShape == null)
                {
                    _innerShape = new Polygon()
                    {
                        Stroke = new SolidColorBrush(StrokeColor),
                        StrokeThickness = StrokeThickess,
                        Fill = Filled ? new SolidColorBrush(FillColor) : new SolidColorBrush(Colors.Transparent)
                    };
                    var polygon = _innerShape as Polygon;
                    polygon.Points.Clear();
                    PointList.ForEach(pv => polygon.Points.Add(new Point(pv.X, pv.Y)));
                    polygon.Points.Add(PointList[0].ToPoint());
                }
            }

            if (!showed)
            {
                canvas.Children.Add(_innerShape);
                showed = true;
            }
        }

        public void Reset()
        {
            PointList.Clear();
            showed = false;
            ArrowReverse = false;
        }
    }

    public enum RenderType
    {
        Polyline,
        Polygon
    }
}

