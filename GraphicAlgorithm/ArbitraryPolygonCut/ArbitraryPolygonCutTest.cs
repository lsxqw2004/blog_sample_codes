using System.Collections.Generic;
using Xunit;

namespace WpfApplication1
{
    public partial class ArbitraryPolygonCut
    {
        [Fact]
        public void TestLineCross()
        {
            var x = LineCrossH(-0.5, new Vertex(4, 3), new Vertex(10, -4.5));
            Assert.Equal(x, 6.8);

            var y = LineCrossV(6.8, new Vertex(4, 3), new Vertex(10, -4.5));
            Assert.Equal(y, -0.5);
        }

        [Fact]
        public void TestCutByLine_Common1()
        {
            var lstC = new List<VertexBase>
                           {
                               new Vertex(4, 8),
                               new Vertex(12, 9),
                               new Vertex(14, 1),
                               new Vertex(2, 3),
                               new Vertex(4, 8)
                           };

            //S1S5切割多边形
            var polyC = new LinkedList<VertexBase>(lstC);
            var inters = CutByLine(new Vertex(2, 12), new Vertex(10, 1), polyC);
            Assert.True(inters.Count == 2 && polyC.Count == 7);

            //S2S1切割多边形
            polyC = new LinkedList<VertexBase>(lstC);
            inters = CutByLine(new Vertex(6, -2), new Vertex(2, 12), polyC);
            Assert.True(inters.Count == 2 && polyC.Count == 7);

            // S5S4切割多边形
            // 测试虚交点
            polyC = new LinkedList<VertexBase>(lstC);
            inters = CutByLine(new Vertex(10, 1), new Vertex(12, 5), polyC);
            Assert.True(inters.Count == 1 && polyC.Count == 6);
        }

        [Fact]
        public void TestCutByLine_Serial()
        {
            // 测试连续切割后，polyC链表是否正确
            //S1S5切割多边形
            var polyC = new LinkedList<VertexBase>(new List<VertexBase>()
                                                       {
                                                           new Vertex(4, 8),
                                                           new Vertex(12, 9),
                                                           new Vertex(14, 1),
                                                           new Vertex(2, 3),
                                                           new Vertex(4, 8)
                                                       });
            var inters = CutByLine(new Vertex(2, 12), new Vertex(10, 1), polyC);

            Assert.True(inters.Count == 2 && polyC.Count == 7);

            //S5S4切割多边形
            var inters2 = CutByLine(new Vertex(10, 1), new Vertex(12, 5), polyC);
            Assert.True(inters2.Count == 1 && polyC.Count == 8);

            //S2S1切割多边形
            var inters3 = CutByLine(new Vertex(6, -2), new Vertex(2, 12), polyC);
            Assert.True(inters3.Count == 2 && polyC.Count == 10);
        }

        [Fact]
        public void TestCutByLineForCrossDi_Common1()
        {
            //S1S5切割多边形
            //普通情况
            var polyC = new List<VertexBase>
                            {
                                new Vertex(4, 8),
                                new Vertex(12, 9),
                                new Vertex(14, 1),
                                new Vertex(2, 3),
                                new Vertex(4, 8)
                            };

            var inters = CutByLineForCrossDi(new Vertex(2, 12), new Vertex(10, 1), polyC, false);
            Assert.True(inters.Item1 == CrossInOut.In && inters.Item2 == 0 && inters.Item3);

            inters = CutByLineForCrossDi(new Vertex(10, 1), new Vertex(2, 12), polyC, false);
            Assert.True(inters.Item1 == CrossInOut.In && inters.Item2 == 2 && inters.Item3);

            var polyS = new List<VertexBase>
                            {
                                new Vertex(2, 12),
                                new Vertex(10, 1),
                                new Vertex(12, 5),
                                new Vertex(13, 0),
                                new Vertex(6, -2),
                                new Vertex(2, 12)
                            };

            var inters2 = CutByLineForCrossDi(new Vertex(14, 1), new Vertex(2, 3), polyS, true, 0);
            Assert.True(inters2.Item1 == CrossInOut.In && inters2.Item2 == 0 && inters2.Item3);
        }

        [Fact]
        public void TestCutByLineForCrossDi_Common2()
        {
            //S4S3切割多边形
            //测试第一个交点进出性为出的情况
            var polyC = new List<VertexBase>
                            {
                                new Vertex(4, 8),
                                new Vertex(12, 9),
                                new Vertex(14, 1),
                                new Vertex(2, 3),
                                new Vertex(4, 8)
                            };
            var inters = CutByLineForCrossDi(new Vertex(12, 5), new Vertex(13, 0), polyC, false);

            Assert.True(inters.Item1 == CrossInOut.Out && inters.Item2 == 2 && inters.Item3);
        }

        [Fact]
        public void TestCutByLineForCrossDi_Common3()
        {
            //S4S3切割多边形
            //测试无交点
            var polyC = new List<VertexBase>
                            {
                                new Vertex(4, 8),
                                new Vertex(12, 9),
                                new Vertex(14, 1),
                                new Vertex(2, 3),
                                new Vertex(4, 8)
                            };
            var inters = CutByLineForCrossDi(new Vertex(13, 0), new Vertex(6, -2), polyC, false);

            Assert.False(inters.Item3);
        }

        [Fact]
        public void TestCutByLineForCrossDi_Common4()
        {
            //S4S3切割多边形
            //测试切线的第一点x坐标大于第二点x坐标
            var polyC = new List<VertexBase>
                            {
                                new Vertex(4, 8),
                                new Vertex(12, 9),
                                new Vertex(14, 1),
                                new Vertex(2, 3),
                                new Vertex(4, 8)
                            };
            var inters = CutByLineForCrossDi(new Vertex(6, -2), new Vertex(2, 12), polyC, false);

            Assert.True(inters.Item1 == CrossInOut.In && inters.Item2 == 2 && inters.Item3);
        }

        [Fact]
        public void TestCut_Commom()
        {
            var polyS = new List<VertexBase>
                            {
                                new Vertex(2, 12),
                                new Vertex(10, 1),
                                new Vertex(12, 5),
                                new Vertex(13, 0),
                                new Vertex(6, -2),
                                new Vertex(2, 12)
                            };

            var polyC = new List<VertexBase>
                            {
                                new Vertex(4, 8),
                                new Vertex(12, 9),
                                new Vertex(14, 1),
                                new Vertex(2, 3),
                                new Vertex(4, 8)
                            };

            // 多边形同向情况
            var result = Cut(polyS, polyC);
            Assert.True(result.Count == 2
                        && result[0].Count == 5 + 1
                        && result[1].Count == 3 + 1);

            // 多边形不同向
            polyS.Reverse();
            result = Cut(polyS, polyC);
            Assert.True(result.Count == 2
                        && result[0].Count == 5 + 1
                        && result[1].Count == 3 + 1);
        }

        [Fact]
        public void TestCutByLine_Special1_1()
        {
            //S1S5切割多边形
            var lstC = new List<VertexBase>()
                           {
                               new Vertex(4, 7),
                               new Vertex(8, 9),
                               new Vertex(9, 4),
                               new Vertex(2, 3),
                               new Vertex(4, 7)
                           };

            // 水平切(Sample 2)
            var polyC1 = new LinkedList<VertexBase>(lstC);
            var inters = CutByLine(new Vertex(6, 2), new Vertex(6, 8), polyC1);
            Assert.True(inters.Count == 2 && polyC1.Count == 7);

            // 垂直切(Sample 2)
            polyC1 = new LinkedList<VertexBase>(lstC);
            inters = CutByLine(new Vertex(6, 8), new Vertex(12, 8), polyC1);
            Assert.True(inters.Count == 2 && polyC1.Count == 7);

            // 连续切(Sample 3)
            var polyC2 = new LinkedList<VertexBase>(lstC);
            var inters2 = CutByLine(new Vertex(5, 2), new Vertex(6, 8), polyC2);
            Assert.True(inters2.Count == 2 && polyC2.Count == 7);

            var inters3 = CutByLine(new Vertex(6, 8), new Vertex(12, 7), polyC2);
            Assert.True(inters3.Count == 2 && polyC2.Count == 9);

        }

        [Fact]
        public void TestCutByLine_Special2_1()
        {
            //S1S5切割多边形
            var lstC = new List<VertexBase>()
                           {
                               new Vertex(4, 9),
                               new Vertex(8, 9),
                               new Vertex(9, 2),
                               new Vertex(6, 2),
                               new Vertex(4, 9)
                           };

            // 水平切(Sample 2)
            var polyC1 = new LinkedList<VertexBase>(lstC);
            var inters = CutByLine(new Vertex(11, 2), new Vertex(3, 2), polyC1);
            Assert.True(inters.Count == 2 && polyC1.Count == 7);
        }

        //交点相交
        [Fact]
        public void TestCutByLine_Special4_1()
        {
            //S1S5切割多边形
            var lstC = new List<Vertex>()
                           {
                               new Vertex(4, 8),
                               new Vertex(7, 4),
                               new Vertex(1, 1),
                               new Vertex(4, 8)
                           };

            //(Sample 2)
            // 交点处连续切
            var polyC1 = new LinkedList<VertexBase>(lstC);
            var inters = CutByLine(new Vertex(5, 2), new Vertex(4, 8), polyC1);
            Assert.True(inters.Count == 2 && polyC1.Count == 6);

            inters = CutByLine(new Vertex(4, 8), new Vertex(9, 9), polyC1);
            Assert.True(inters.Count == 0 && polyC1.Count == 6);

        }


        [Fact]
        public void TestCutByLineForCrossDi_Special4()
        {
            //S4S3切割多边形
            //交点重合图形，判断进出性
            //判断点为普通点
            var polyC = new List<VertexBase>
                            {
                               new Vertex(4, 8),
                               new Vertex(7, 4),
                               new Vertex(1, 1),
                               new Vertex(4, 8)
                            };

            var inters = CutByLineForCrossDi(new Vertex(5, 2), new Vertex(4, 8), polyC, false);
            Assert.True(inters.Item1 == CrossInOut.In && inters.Item2 == 1 && inters.Item3);

            var polyS = new List<VertexBase>
                            {
                               new Vertex(4, 8),
                               new Vertex(9, 9),
                               new Vertex(10, 3),
                               new Vertex(5, 2),
                               new Vertex(4, 8)
                            };

            var inters2 = CutByLineForCrossDi(new Vertex(7, 4), new Vertex(1, 1), polyS, true, 3);
            Assert.True(inters2.Item1 == CrossInOut.Out && inters2.Item2 == 3 && inters2.Item3);

        }

        [Fact]
        public void TestCut_Special4()
        {
            var polyS = new List<VertexBase>
                            {
                               new Vertex(4, 8),
                               new Vertex(9, 9),
                               new Vertex(10, 3),
                               new Vertex(5, 2),
                               new Vertex(4, 8)
                            };

            var polyC = new List<VertexBase>
                            {
                               new Vertex(4, 8),
                               new Vertex(7, 4),
                               new Vertex(1, 1),
                               new Vertex(4, 8)
                            };

            // 多边形同向情况
            var result = Cut(polyS, polyC);
            Assert.True(result.Count == 1
                        && result[0].Count == 4 + 1);

            // 多边形不同向
            polyS.Reverse();
            result = Cut(polyS, polyC);
            Assert.True(result.Count == 1
                        && result[0].Count == 4 + 1);
        }


    }
}