using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Markup.Localizer;

namespace WpfApplication1
{
    public partial class ArbitraryPolygonCut
    {
        private const int Precision = 100000;
        private static CrossInOut _firstInterDi;

        // 传入的多边形的点需要封闭，即首尾点要相同
        public static List<List<VertexBase>> Cut(List<VertexBase> listS, List<VertexBase> listC)
        {
            //如果是浮点数，使用这个处理保证相等比较正确
            PrepareVertex(listS);
            PrepareVertex(listC);

            int cutStartIdx = 0;//实际切割由这个idx起始即可

            // 阶段1： 这个循环中的切割用于判断方向
            // 如果这步中没有发现交点，则进入不相交多边形的处理
            for (; cutStartIdx < listS.Count; cutStartIdx++)
            {
                //用实体多边形的一条边去切切割多边形
                var s1 = listS[cutStartIdx % listS.Count];
                var s2 = listS[(cutStartIdx + 1) % listS.Count];

                Tuple<CrossInOut, int, bool> ret;
                if (s1.X == s2.X)
                    ret = CutByLineVerticalForCrossDi(s1, s2, listC, false);
                else
                    ret = CutByLineForCrossDi(s1, s2, listC, false);
                //如果没有交点，继续下一条边。
                if (!ret.Item3) continue;

                var interDirect1 = ret.Item1;//交点对于切割多边形的进出性
                var cutLineIdx = ret.Item2;//交点所在的切割多边形的边的起点的索引，用于在使用切割多边形边去切实体多边形时，确定那条边

                WindowLog.Default.Log("得到S多边形边{0}{1}切割C多边形{2}{3}产生的交点，对于S进出性为{4}", s1.Name, s2.Name,
                    listC[cutLineIdx % listC.Count].Name,
                    listC[(cutLineIdx + 1) % listC.Count].Name,
                    interDirect1 == CrossInOut.In ? "进" : "出");

                //用切割多边形的一条边(cutLineIdx->cutLineIdx+1)去切实体多边形
                var ret2 = CutByLineForCrossDi(listC[cutLineIdx % listC.Count],
                                               listC[(cutLineIdx + 1) % listC.Count],
                                               listS, true, cutStartIdx);
                var interDirect2 = ret2.Item1;

                WindowLog.Default.Log("得到C多边形边{0}{1}切割S多边形{2}{3}产生的交点，对于C进出性为{4}",
                  listC[cutLineIdx % listC.Count].Name,
                  listC[(cutLineIdx + 1) % listC.Count].Name,
                  s1.Name, s2.Name,
                  interDirect2 == CrossInOut.In ? "进" : "出");

                if (interDirect1 == interDirect2) //进出性相同表示多边形不同向，反转其中一个多边形
                {
                    WindowLog.Default.Log("交点进出性相同，把C多边形反向");
                    //文档中是把S进行了反向，但这里不行，如果反向S，则记录的第一个交点（主要是记录这个交点的进出性）就不再是第一个交点了
                    //所以实际实现中需要将C反向，而且反向后第一条边，仍然需要是确定第一点那条线段
                    var listCReverse = new List<VertexBase>();
                    var reverseStartIdx = cutLineIdx + 1;
                    for (int i = 0; i < listC.Count; i++)
                    {
                        listCReverse.Add(listC[(reverseStartIdx - i + listC.Count) % listC.Count]);
                    }

                    listC = listCReverse;

                    WindowLog.Default.ReversePolygon();
                    WindowLog.Default.Log("反向后多边形点序列为：{0}", string.Join("->", listC.Select(r => r.Name)));
                }

                WindowLog.Default.Log("使用S多边形边{0}{1}与C多边形{2}{3}交点为第一个点，对于S进出性为{4}", s1.Name, s2.Name,
                        listC[cutLineIdx % listC.Count].Name,
                        listC[(cutLineIdx + 1) % listC.Count].Name,
                        interDirect1 == CrossInOut.In ? "进" : "出");

                _firstInterDi = ret.Item1;
                break;
            }

            if (cutStartIdx == listS.Count)//没有交点
            {
                WindowLog.Default.Log("没有交点，进入无交点情况处理");

                var ret = ProcessNoCross(listS, listC);
                return ret == null ? new List<List<VertexBase>>() : new List<List<VertexBase>> { ret };
            }

            // 阶段2： 链接多边形，即设置Next
            LinkNode(listS.Cast<Vertex>().ToList());
            LinkNode(listC.Cast<Vertex>().ToList());
            var listI = new List<Intersection>();
            var linkC = new LinkedList<VertexBase>(listC);

            //循环中用S中每条边切割C(准确说是C的链表，每次切割后交点插入C的列表再进行下次切割)，把交点插入S和C形成多边形链表
            for (; cutStartIdx < listS.Count; cutStartIdx++)
            {
                var s1 = listS[cutStartIdx % listS.Count] as Vertex;
                var s2 = listS[(cutStartIdx + 1) % listS.Count] as Vertex;

                WindowLog.Default.Log("---------------使用S多边形边{0}{1}切割C多边形---------------", s1.Name, s2.Name);

                var inters = CutByLine(s1, s2, linkC);
                //var inters = ret;

                if (inters.Count == 0) continue;

                listI.AddRange(inters);
                //把交点排序，准备插入S的边中
                if (s1.X < s2.X)
                    inters.Sort((p1, p2) => p1.X.CompareTo(p2.X));
                else
                    inters.Sort((p1, p2) => -(p1.X.CompareTo(p2.X)));

                //将交点插入S的边中
                s1.Next = inters[0];
                for (int j = 0; j < inters.Count - 1; j++)
                {
                    inters[j].NextS = inters[j + 1];
                }
                inters[inters.Count - 1].NextS = s2;

                #region log
                var sLinkSb = new StringBuilder();
                var v = listS[0];
                while (v != null)
                {
                    sLinkSb.Append(v.Name);
                    VertexBase next = null;
                    if (v is Intersection)
                    {
                        var curr = v as Intersection;
                        next = curr.NextS;
                    }
                    else if (v is Vertex)
                    {
                        var curr = v as Vertex;
                        next = curr.Next;
                    }

                    if (next.Equals(listS[0]))
                    {
                        break;
                    }
                    v = next;
                    sLinkSb.Append("->");
                }
                WindowLog.Default.Log("------S链表：" + sLinkSb);
                #endregion
            }

            // 设置交点的进出性
            // 不能简单的把listI中的交点按偶奇的顺序标记进出性，因为listI中的顺序可能和S链表中交点出现的顺序不一样
            // 需要安装S链表中交点的顺序标记
            var secondDi = _firstInterDi == CrossInOut.In ? CrossInOut.Out : CrossInOut.In;
            var nextS = (listS[0] as Vertex).Next;
            int order = 1;
            while (!nextS.Equals(listS[0]))
            {
                if (nextS is Intersection)
                {
                    (nextS as Intersection).CrossDi = order %2 ==1 ? _firstInterDi:secondDi;
                    ++order;
                    nextS = (nextS as Intersection).NextS;
                }
                else
                {
                    nextS = (nextS as Vertex).Next;
                }
            }

            #region log

            foreach (var inters in listI)
            {
                WindowLog.Default.Log("交点{0}，NextS:{1}，NextC:{2}，S对于C进出性:{3}",
                    inters.Name, inters.NextS.Name, inters.NextC.Name, inters.CrossDi == CrossInOut.In ? "进" : "出");
                WindowLog.Default.AddLabel(inters.Name, inters.ToPoint());
            }

            #endregion


            //阶段3：按规则连接交点得到结果
            var result = Compose(listI);

            return result;
        }

        private static void PrepareVertex(List<VertexBase> list)
        {
            list.ForEach(r =>
            {
                r.X = r.X * Precision / Precision;
                r.Y = r.Y * Precision / Precision;
            });
        }

        /// <summary>
        /// 将多边形的结点通过Next属性构成链表
        /// </summary>
        /// <param name="list">实体多边形结点</param>
        private static void LinkNode(List<Vertex> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var v1 = list[i % list.Count];
                var v2 = list[(i + 1) % list.Count];
                v1.Next = v2;
            }
        }

        /// <summary>
        /// 用于获得交点进出性的切割函数
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="list"></param>
        /// <param name="line2Idx"> </param>
        /// <returns></returns>
        private static Tuple<CrossInOut, int, bool> CutByLineForCrossDi(VertexBase v1, VertexBase v2, List<VertexBase> list, bool withIdx, int line2Idx = 0)
        {
            bool hasIntersection = false;
            bool smallToBig = true;

            var crossXsmaller = new List<IntersWithIndex>();
            var crossXbigger = new List<IntersWithIndex>();

            var slope = (v2.Y - v1.Y) / (v1.X - v2.X);
            var minX = v1.X;
            var maxX = v2.X;
            if (v1.X > v2.X)
            {
                minX = v2.X;
                maxX = v1.X;
                smallToBig = false;
            }

            //错切后的切割多边形
            var shearedPolyC = list.Select(r => new Vertex(r.X, r.X * slope + r.Y) { Name = r.Name }).ToList();
            //把实体多边形的第一条边（即切线）错切后得到一条水平直线
            var y = v1.X * slope + v1.Y;

            for (var toCutLineStart = 0; toCutLineStart < list.Count; toCutLineStart++)
            {
                var c1 = shearedPolyC[toCutLineStart % list.Count];//注意，不要漏了文档中C4->C1这种结尾的线段
                var c2 = shearedPolyC[(toCutLineStart + 1) % list.Count];

                // 重合
                if (c1.Y == c2.Y && c1.Y == y) continue;
                //不相交
                if (c1.Y > y && c2.Y > y) continue;
                if (c1.Y < y && c2.Y < y) continue;

                var x = LineCrossH(y, c1, c2);
                var npy = y - x * slope;

                if (!hasIntersection)
                    if ((x > minX && x < maxX) ||
                        (c2.Y == y && x == v2.X) ||
                        (x == minX && c1.Y != y && c2.Y != y) ||
                        (x == maxX && c1.Y != y && c2.Y != y)
                    )
                        hasIntersection = true;

                var inters = new IntersWithIndex(x, npy, toCutLineStart);
                if (smallToBig)
                {
                    if (x < minX) crossXsmaller.Add(inters);
                    if ((x > minX && x < maxX) ||
                        (x == minX && c2.Y == y) ||
                        (x == minX && c1.Y != y && c2.Y != y) ||
                        (x == maxX && c2.Y == y) ||
                        (x == maxX && c1.Y != y && c2.Y != y) ||
                        (x > maxX) //这个必不可少，不然影响进出性判断
                        )
                        crossXbigger.Add(inters);
                }
                else
                {
                    if (x > maxX) crossXsmaller.Add(inters);
                    if ((x > minX && x < maxX) ||
                        (x == maxX && c2.Y == y) ||
                        (x == maxX && c1.Y != y && c2.Y != y) ||
                        (c2.Y == y && x == minX) ||
                        (x == minX && c1.Y != y && c2.Y != y) ||
                        (x < minX) //这个必不可少，不然影响进出性判断
                        )
                        crossXbigger.Add(inters);
                }
            }

            if (!hasIntersection) return Tuple.Create(CrossInOut.In, 0, false);

            if (smallToBig)
                crossXbigger.Sort((p1, p2) => p1.X.CompareTo(p2.X));
            else
                crossXbigger.Sort((p1, p2) => -(p1.X.CompareTo(p2.X)));

            var count = crossXbigger.Count;
            CrossInOut crossRet;
            int cuttedLineIdx;

            if (withIdx)//切割多边形的一条边去切实体多边形的一条边
            {
                int countSkip = 0;
                for (; ; countSkip++)
                {
                    if (crossXbigger[countSkip].Idx == line2Idx)//line2Idx确定了实体多边形的这条边
                        break;
                }
                count += countSkip;
                crossRet = count % 2 == 0 ? CrossInOut.In : CrossInOut.Out;
                cuttedLineIdx = crossXbigger[countSkip].Idx;
            }
            else//实体多边形的一条边去切切割多边形
            {
                crossRet = count % 2 == 0 ? CrossInOut.In : CrossInOut.Out;
                cuttedLineIdx = crossXbigger[0].Idx;
            }

            return Tuple.Create(crossRet, cuttedLineIdx, true);
        }

        private static Tuple<CrossInOut, int, bool> CutByLineVerticalForCrossDi(VertexBase v1, VertexBase v2, List<VertexBase> list, bool withIdx, int line2Idx = 0)
        {
            bool hasIntersection = false;
            bool smallToBig = true;

            var crossXsmaller = new List<IntersWithIndex>();
            var crossXbigger = new List<IntersWithIndex>();

            var minY = v1.Y;
            var maxY = v2.Y;
            if (v1.Y > v2.Y)
            {
                minY = v2.Y;
                maxY = v1.Y;
                smallToBig = false;
            }
            var x = v1.X;

            for (var toCutLineStart = 0; toCutLineStart < list.Count; toCutLineStart++)
            {
                var c1 = list[toCutLineStart % list.Count];
                var c2 = list[(toCutLineStart + 1) % list.Count];

                // 重合
                if (c1.X == c2.X && c1.X == x) continue;
                //不相交
                if (c1.X > x && c2.X > x) continue;
                if (c1.X < x && c2.X < x) continue;

                var y = LineCrossV(x, c1, c2);

                if (!hasIntersection)
                    if ((y > minY && y < maxY) ||
                        (c2.X == x && y == v2.Y) ||
                        (y == minY && c1.X != x && c2.X != x) ||
                        (y == maxY && c1.X != x && c2.X != x)
                    )
                        hasIntersection = true;

                var inters = new IntersWithIndex(x, y, toCutLineStart);
                if (smallToBig)
                {
                    if (y < minY) crossXsmaller.Add(inters);
                    if ((y > minY && y < maxY) ||
                        (y == minY && c2.X == x) ||
                        (y == minY && c1.X != x && c2.X != x) ||
                        (y == maxY && c2.X == x) ||
                        (y == maxY && c1.X != x && c2.X != x) ||
                        (y > maxY)
                        )
                        crossXbigger.Add(inters);
                }
                else
                {
                    if (y > maxY) crossXsmaller.Add(inters);
                    if ((y > minY && y < maxY) ||
                        (y == maxY && c2.X == x) ||
                        (y == maxY && c1.X != x && c2.X != x) ||
                        (y == minY && c2.X == x) ||
                        (y == minY && c1.X != x && c2.X != x) ||
                        (y < minY)
                        )
                        crossXbigger.Add(inters);
                }
            }

            if (!hasIntersection) return Tuple.Create(CrossInOut.In, 0, false);

            if (smallToBig)
                crossXbigger.Sort((p1, p2) => p1.X.CompareTo(p2.X));
            else
                crossXbigger.Sort((p1, p2) => -(p1.X.CompareTo(p2.X)));

            var count = crossXbigger.Count;
            CrossInOut crossRet;
            int cuttedLineIdx;

            if (withIdx)//切割多边形的一条边去切实体多边形的一条边
            {
                int countSkip = 0;
                for (; ; countSkip++)
                {
                    if (crossXbigger[countSkip].Idx == line2Idx)//line2Idx确定了实体多边形的这条边
                        break;
                }
                count += countSkip;
                crossRet = count % 2 == 0 ? CrossInOut.In : CrossInOut.Out;
                cuttedLineIdx = crossXbigger[countSkip].Idx;
            }
            else//实体多边形的一条边去切切割多边形
            {
                crossRet = count % 2 == 0 ? CrossInOut.In : CrossInOut.Out;
                cuttedLineIdx = crossXbigger[0].Idx;
            }

            return Tuple.Create(crossRet, cuttedLineIdx, true);
        }

        /// <summary>
        /// 用实体多边形S的边切割切割多边形C，并插入交点到C中
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <param name="linkC"></param>
        /// <returns></returns>
        private static List<Intersection> CutByLine(Vertex s1, Vertex s2, LinkedList<VertexBase> linkC)
        {
            if (s1.X == s2.X)
                return CutByLineVertical(s1, s2, linkC);

            var crossXs = new List<Intersection>();

            double slope = 0, y = s1.Y;

            slope = (s2.Y - s1.Y) / (s1.X - s2.X);//s2.Y == s1.Y不单独判断，反正这里可以正常处理s2.Y == s1.Y即slope==0
            var shearedPolyC = linkC.Select(r => new Vertex(r.X, r.X * slope + r.Y) as VertexBase).ToList();//为了保存错切计算的坐标
            y = s1.X * slope + s1.Y;

            var minX = s1.X > s2.X ? s2.X : s1.X;
            var maxX = s1.X > s2.X ? s1.X : s2.X;

            //LinkedListNode<VertexBase> vertex;
            int i = -1;
            var backToHead = false;
            for (var vertex = linkC.First;
                //这样回到头部的线也可以正常被切到
                vertex != null &&
                ((vertex.Value is Vertex && vertex.Next != null) ||
                ((vertex.Value is Intersection) && ((Intersection)vertex.Value).NextC != null) ||
                    (vertex.Value is Vertex && vertex.Next == null && !backToHead) ||
                    (vertex.Value is Intersection && ((Intersection)vertex.Value).NextC == null && !backToHead));
                    
                 vertex = vertex.Next)
            {
                ++i;
                var c1 = shearedPolyC[i % shearedPolyC.Count];
                var c2 = shearedPolyC[(i + 1) % shearedPolyC.Count];

                // 重合
                if (c1.Y == c2.Y && c1.Y == y) continue;
                //不相交
                if (c1.Y > y && c2.Y > y) continue;
                if (c1.Y < y && c2.Y < y) continue;

                var x = LineCrossH(y, c1, c2);
                var npy = y - x * slope;
                var inters = new Intersection(x, npy);

                VertexBase next = null;
                if ((x > minX && x < maxX) ||
                    (c2.Y == y && x == s2.X) ||
                    (x == minX && c1.Y != y && c2.Y != y) ||
                    (x == maxX && c1.Y != y && c2.Y != y))
                {
                    inters.Name = "I" + Counter.Default.Val++;
                    if (vertex.Next == null)
                    {
                        backToHead = true;
                        next = linkC.First.Value;
                    }
                    else
                    {
                        next = vertex.Next.Value;
                    }

                    WindowLog.Default.Log("切割C的边{0}{1}得到交点{2}", vertex.Value.Name,
                        next.Name, inters.Name);

                    inters.NextC = next;
                    if (vertex.Value is Vertex)
                        ((Vertex)vertex.Value).Next = inters;
                    else if (vertex.Value is Intersection)
                        ((Intersection)vertex.Value).NextC = inters;
                    linkC.AddAfter(vertex, inters);
                    vertex = vertex.Next;
                    crossXs.Add(inters);
                }

                if (backToHead)
                    break;

                #region log
                var cLinkSb = new StringBuilder();
                var v = linkC.First.Value;
                while (v != null)
                {
                    cLinkSb.Append(v.Name);
                    if (v is Intersection)
                    {
                        var curr = v as Intersection;
                        next = curr.NextC;
                    }
                    else if (v is Vertex)
                    {
                        var curr = v as Vertex;
                        next = curr.Next;
                    }

                    if (next.Equals(linkC.First.Value))
                    {
                        break;
                    }
                    v = next;
                    cLinkSb.Append("->");
                }
                WindowLog.Default.Log("C链表：" + cLinkSb);
                #endregion
            }

            return crossXs;
        }

        private static List<Intersection> CutByLineVertical(Vertex s1, Vertex s2, LinkedList<VertexBase> linkC)
        {
            var crossXs = new List<Intersection>();
            var x = s1.X;

            List<VertexBase> shearedPolyC = linkC.ToList();

            var minY = s1.Y > s2.Y ? s2.Y : s1.Y;
            var maxY = s1.Y > s2.Y ? s1.Y : s2.Y;

            int i = -1;
            bool backToHead = false;
            for (var vertex = linkC.First;
                //这样回到头部的线也可以正常被切到
                vertex != null &&
                ((vertex.Value is Vertex && vertex.Next != null) ||
                ((vertex.Value is Intersection) && ((Intersection)vertex.Value).NextC != null) ||
                    (vertex.Value is Vertex && vertex.Next == null && !backToHead) ||
                    (vertex.Value is Intersection && ((Intersection)vertex.Value).NextC == null && !backToHead));
                vertex = vertex.Next)
            {
                i++;
                var c1 = shearedPolyC[i % shearedPolyC.Count];
                var c2 = shearedPolyC[(i + 1)%shearedPolyC.Count];

                // 重合
                if (c1.X == c2.X && c1.X == x) continue;
                //不相交
                if (c1.X > x && c2.X > x) continue;
                if (c1.X < x && c2.X < x) continue;

                var y = LineCrossV(x, c1, c2);

                var inters = new Intersection(x, y);

                VertexBase next = null;
                if ((y > minY && y < maxY) ||
                    (c2.X == x && y == minY) ||
                    (c2.X == x && y == maxY) ||
                    (y == minY && c1.X != x && c2.X != x) ||
                    (y == maxY && c1.X != x && c2.X != x))
                {
                    inters.Name = "I" + Counter.Default.Val++;
                    if (vertex.Next == null)
                    {
                        backToHead = true;
                        next = linkC.First.Value;
                    }
                    else
                    {
                        next = vertex.Next.Value;
                    }

                    WindowLog.Default.Log("切割C的边{0}{1}得到交点{2}", vertex.Value.Name,
                        next.Name, inters.Name);

                    inters.NextC = next;
                    if (vertex.Value is Vertex)
                        (vertex.Value as Vertex).Next = inters;
                    else if (vertex.Value is Intersection)
                        (vertex.Value as Intersection).NextC = inters;
                    linkC.AddAfter(vertex, inters);
                    vertex = vertex.Next;
                    crossXs.Add(inters);
                }

                if (backToHead)
                    break;

                #region log
                var cLinkSb = new StringBuilder();
                var v = linkC.First.Value;
                while (v != null)
                {
                    cLinkSb.Append(v.Name);
                    if (v is Intersection)
                    {
                        var curr = v as Intersection;
                        next = curr.NextC;
                    }
                    else if (v is Vertex)
                    {
                        var curr = v as Vertex;
                        next = curr.Next;
                    }

                    if (next.Equals(linkC.First.Value))
                    {
                        break;
                    }
                    v = next;
                    cLinkSb.Append("->");
                }
                WindowLog.Default.Log("C链表：" + cLinkSb);
                #endregion
            }

            return crossXs;
        }

        //用垂直线切割另一条线段
        private static double LineCrossH(double y, VertexBase c1, VertexBase c2)
        {
            return c1.X + (c2.X - c1.X) * (y - c1.Y) / (c2.Y - c1.Y);
        }

        //用水平线切割另一条线段
        private static double LineCrossV(double x, VertexBase c1, VertexBase c2)
        {
            return c1.Y + (c2.Y - c1.Y) * (x - c1.X) / (c2.X - c1.X);
        }

        private static List<List<VertexBase>> Compose(List<Intersection> listI)
        {
            WindowLog.Default.Log(Environment.NewLine + "开始组合交点以获得结果");
            var logSb = new StringBuilder();

            var result = new List<List<VertexBase>>();

            foreach (var inters in listI)
            {
                if (!inters.Used && inters.CrossDi == CrossInOut.In)
                {
                    var oneResult = new List<VertexBase>();
                    oneResult.Add(new Vertex(inters.X, inters.Y));
                    inters.Used = true;

                    logSb.Append(inters.Name);

                    var loopvar = inters.NextS;
                    while (loopvar != null)
                    {
                        logSb.Append("->" +loopvar.Name);

                        oneResult.Add(new Vertex(loopvar.X, loopvar.Y));
                        VertexBase next = null;
                        if (loopvar is Intersection)
                        {
                            var curr = loopvar as Intersection;
                            curr.Used = true;
                            next = curr.CrossDi == CrossInOut.In ? curr.NextS : curr.NextC;
                        }
                        else if (loopvar is Vertex)
                        {
                            var curr = loopvar as Vertex;
                            next = curr.Next;
                        }

                        if (next.Equals(inters))
                        {
                            logSb.Append(Environment.NewLine);

                            oneResult.Add(new Vertex(next.X, next.Y));
                            break;
                        }
                        loopvar = next;
                    }
                    result.Add(oneResult);
                }
            }

            WindowLog.Default.Log(logSb.ToString());

            return result;
        }

        private static List<VertexBase> ProcessNoCross(List<VertexBase> listS, List<VertexBase> listC)
        {
            bool sInC = IsVertexInPolygon(listS[0], listC);
            if (sInC) return listS;
            bool cInS = IsVertexInPolygon(listC[0], listS);
            if (cInS) return listC;
            return null;
        }

        //判断点是否在一个多边形中
        //方法：由点做一条射线，如果和多边形边交叉次数是偶数（包括0不交叉）则不再多边形内，反之
        private static bool IsVertexInPolygon(VertexBase v, List<VertexBase> list)
        {
            int judgeIndex = 0;
            for (int i = 0; i < list.Count - 1; i++)
            {
                int j = i + 1;

                var minY = list[i].Y;
                var maxY = list[j].Y;
                if (minY > maxY)
                {
                    minY = list[j].Y;
                    maxY = list[i].Y;
                }
                if (v.Y >= maxY || v.Y <= minY) continue;

                double x = (list[i].X - list[j].X) / (list[i].Y - list[j].Y) * (v.Y - list[i].Y) + list[i].X;//求射线与边交点的x坐标
                if (Math.Abs(v.X - x) < double.Epsilon)
                    return true;
                if (v.X > x)//对于向右做射线，这样就说明射线与边一定会交叉
                    judgeIndex++;
            }

            if (judgeIndex % 2 != 0)
                return true;

            return false;
        }
    }


}
