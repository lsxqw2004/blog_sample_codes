using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace WpfApplication1
{
    [Serializable]
    public abstract class VertexBase
    {
        public double X { get; set; }
        public double Y { get; set; }

        public string Name { get; set; }

        public void SetXY(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Point ToPoint()
        {
            return new Point(X,Y);
        }
    }

    public class Vertex : VertexBase
    {
        [DebuggerNonUserCode]
        public Vertex(double x, double y)
        {
            X = x;
            Y = y;
        }

        [JsonIgnore]
        public VertexBase Next { get; set; }
    }

    public class Intersection : VertexBase
    {
        [DebuggerNonUserCode]
        public Intersection(double x, double y)
        {
            X = x;
            Y = y;
        }

        public CrossInOut CrossDi { get; set; }
        public bool Used { get; set; }
        public VertexBase NextS { get; set; }
        public VertexBase NextC { get; set; }
    }

    public class IntersWithIndex : Intersection
    {
        [DebuggerNonUserCode]
        public IntersWithIndex(double x, double y, int idx)
            : base(x, y)
        {
            Idx = idx;
        }

        public int Idx { get; set; }
    }

    public enum CrossInOut
    {
        In, Out
    }
}
