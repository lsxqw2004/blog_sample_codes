using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace ConsoleApplication1
{
    public class SocialNetwork
    {
        List<string> _people = new List<string>()
        {
            "Charlie","Augustus","Veruca","Violet","Mike","Joe","Willy","Miranda"
        };

        List<string[]> _links = new List<string[]>()
        {
            new[] {"Augustus", "Willy"},
            new[] {"Mike", "Joe"},
            new[] {"Miranda", "Mike"},
            new[] {"Violet", "Augustus"},
            new[] {"Miranda", "Willy"},
            new[] {"Charlie", "Mike"},
            new[] {"Veruca", "Joe"},
            new[] {"Miranda", "Augustus"},
            new[] {"Willy", "Augustus"},
            new[] {"Joe", "Charlie"},
            new[] {"Veruca", "Augustus"},
            new[] {"Miranda", "Joe"}
        };

        class Point
        {
            public Point(int x, int y)
            {
                X = x;
                Y = y;
            }
            public int X { get; set; }
            public int Y { get; set; }
        }

        public float CrossCost(List<int> v)
        {
            // 将数字序列转换成一个key为person，value为(x,y)的字典
            var loc = _people.Select((p, i) => Tuple.Create(p, i))
                .ToDictionary(t => t.Item1, t => new Point(v[t.Item2 * 2], v[t.Item2 * 2 + 1]));
            var total = 0f;

            // 遍历每一对连线
            for (int i = 0; i < _links.Count; i++)
            {
                for (int j = i + 1; j < _links.Count; j++)
                {
                    //判断是否共点
                    var concurrent = false;
                    var radian = 0f;
                    if (_links[i][0] == _links[j][0])
                    {
                        radian = CalcRadian(loc[_links[i][0]], loc[_links[i][1]], loc[_links[j][1]]);
                        concurrent = true;
                    }
                    if (_links[i][1] == _links[j][0])
                    {
                        if(_links[i][0] == _links[j][1]) continue; //排除共线不同向的线段
                        radian = CalcRadian(loc[_links[i][1]], loc[_links[i][0]], loc[_links[j][1]]);
                        concurrent = true;
                    }
                    if (_links[i][0] == _links[j][1])
                    {
                        radian = CalcRadian(loc[_links[i][0]], loc[_links[i][1]], loc[_links[j][0]]);
                        concurrent = true;
                    }
                    if (_links[i][1] == _links[j][1])
                    {
                        radian = CalcRadian(loc[_links[i][1]], loc[_links[i][0]], loc[_links[j][0]]);
                        concurrent = true;
                    }
                    if (concurrent)
                    {
                        //如果角度小于Math.PI/4(22.5度)我们认为是不好看的
                        if(radian*4<Math.PI)
                            total += 1 - radian / (float)(Math.PI/4); //角度越小惩罚越大
                        continue;
                    }

                    //不共点时，判断是否相交
                    // 获取坐标位置
                    var p1 = loc[_links[i][0]];
                    var p2 = loc[_links[i][1]];
                    var p3 = loc[_links[j][0]];
                    var p4 = loc[_links[j][1]];

                    // 强制声明为float
                    float den = (p4.Y - p3.Y) * (p2.X - p1.X) - (p4.X - p3.X) * (p2.Y - p1.Y);
                    // 如果两线平行，则den==0
                    if (den == 0) continue;
                    // 否则，ua与ub就是两条交叉线的分数值
                    var ua = ((p4.X - p3.X) * (p1.Y - p3.Y) -
                              (p4.Y - p3.Y) * (p1.X - p3.X)) / den;
                    var ub = ((p2.X - p1.X) * (p1.Y - p3.Y) -
                              (p2.Y - p1.Y) * (p1.X - p3.X)) / den;
                    // 如果两条线的分数值介于0和1之间，则两线彼此相交
                    if (ua > 0 && ua < 1 && ub > 0 && ub < 1)
                        total += 1;
                }
            }

            for (var i = 0; i < _people.Count; i++)
            {
                for (var j = i + 1; j < _people.Count; j++)
                {
                    //获得两点位置
                    var p1 = loc[_people[i]];
                    var p2 = loc[_people[j]];
                    //计算两节点间的距离
                    var dist = (float)Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
                    //对间距小于50个像素的节点进行惩罚
                    if (dist < 50)
                        total += 1 - dist / 50;
                }
            }

            return total;
        }

        // 计算夹角角度(返回弧度)
        private float CalcRadian(Point o, Point s, Point e)
        {
            float cosfi = 0, fi = 0, norm = 0;
            float dsx = s.X - o.X;
            float dsy = s.Y - o.Y;
            float dex = e.X - o.X;
            float dey = e.Y - o.Y;

            cosfi = dsx * dex + dsy * dey;
            norm = (dsx * dsx + dsy * dsy) * (dex * dex + dey * dey);
            cosfi /= (float)Math.Sqrt(norm);

            if (cosfi >= 1.0) return 0;
            if (cosfi <= -1.0) return (float)Math.PI;
            fi = (float)Math.Acos(cosfi);

            if (fi < Math.PI)
            {
                return fi;
            }
            return (float)Math.PI * 2 - fi;
        }

        //题解范围
        public List<Tuple<int, int>> Domain =>
            Enumerable.Repeat(0, _people.Count * 2)
                .Select((v, i) => Tuple.Create(10, 370))
                .ToList();

        public void DrawNetwork(List<int> sol)
        {
            // 将数字序列转换成一个key为person，value为(x,y)的字典
            var pos = _people.Select((p, i) => Tuple.Create(p, i))
                .ToDictionary(t => t.Item1, t => new Point(sol[t.Item2 * 2], sol[t.Item2 * 2 + 1]));

            Bitmap b = new Bitmap(400, 400, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(b))
            {
                g.Clear(Color.White);
                var pen = new Pen(Color.Black, 1);
                // 绘制连线
                foreach (var l in _links)
                {
                    var start = pos[l[0]];
                    var end = pos[l[1]];
                    g.DrawLine(pen, start.X, start.Y, end.X, end.Y);
                }
                // 绘制字符串
                var font = new Font("Times New Roman", 12);
                var brush = new SolidBrush(Color.Black);
                foreach (var pkv in pos)
                {
                    g.DrawString(pkv.Key, font, brush, pkv.Value.X, pkv.Value.Y);
                }

                b.Save($"result{DateTime.Now.Second}{DateTime.Now.Millisecond}.png", ImageFormat.Png);
            }
        }
    }
}
